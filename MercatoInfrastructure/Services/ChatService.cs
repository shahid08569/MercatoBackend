using MercatoApplication.Common;
using MercatoApplication.DTOs.Chat;
using MercatoApplication.Interfaces;
using MercatoDomain.Entities;
using MercatoDomain.Enums;
using MercatoInfrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MercatoInfrastructure.Services;

/// <summary>
/// Real-time chat messaging service supporting multi-role marketplace communication.
/// Handles dynamic role resolution, relationship boundary validation, and real-time broadcasts.
/// </summary>
public class ChatService : IChatService
{
    private readonly AppDbContext _db;
    private readonly IChatNotificationService _notificationService;

    public ChatService(AppDbContext db, IChatNotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    // Starts or retrieves an existing conversation room between two users
    public async Task<ApiResponse<ConversationResponseDto>> StartOrGetConversationAsync(
        Guid currentUserId, CreateConversationRequestDto request)
    {
        if (currentUserId == request.RecipientUserId)
            return ApiResponse<ConversationResponseDto>.FailureResponse("You cannot start a conversation with yourself.");

        var currentUser = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == currentUserId);

        if (currentUser == null)
            return ApiResponse<ConversationResponseDto>.FailureResponse("Sender user not found.");

        var recipient = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == request.RecipientUserId);

        if (recipient == null)
            return ApiResponse<ConversationResponseDto>.FailureResponse("Recipient user not found.");

        // Check if an existing direct room exists between these two users
        var existingRoom = await _db.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c =>
                c.Participants.Any(p => p.UserId == currentUserId) &&
                c.Participants.Any(p => p.UserId == request.RecipientUserId) &&
                c.OrderId == request.OrderId &&
                c.MerchantId == request.MerchantId);

        if (existingRoom != null)
        {
            return ApiResponse<ConversationResponseDto>.SuccessResponse(MapConversationDto(existingRoom, currentUserId));
        }

        // Dynamically resolve marketplace roles from database entities
        var senderRole = await ResolveUserRoleAsync(currentUser);
        var recipientRole = await ResolveUserRoleAsync(recipient);

        // Dynamically determine conversation channel type based on roles
        var conversationType = MatchConversationType(senderRole, recipientRole);
        if (conversationType == null)
        {
            return ApiResponse<ConversationResponseDto>.FailureResponse(
                $"Direct communication between {senderRole} and {recipientRole} is not supported on this platform.");
        }

        // Dynamic business boundary validation based on contextual references
        var validationResult = await ValidateBusinessBoundariesAsync(
            currentUserId, request, senderRole, recipientRole);

        if (!validationResult.Success)
            return ApiResponse<ConversationResponseDto>.FailureResponse(validationResult.Message);

        var newRoom = new Conversation
        {
            Id = Guid.NewGuid(),
            Type = conversationType.Value,
            Title = request.Title ?? $"Chat: {currentUser.Email} & {recipient.Email}",
            OrderId = request.OrderId,
            MerchantId = request.MerchantId,
            CreatedAt = DateTime.UtcNow,
            LastMessageAt = DateTime.UtcNow
        };

        newRoom.Participants.Add(new ConversationParticipant
        {
            Id = Guid.NewGuid(),
            ConversationId = newRoom.Id,
            UserId = currentUserId,
            JoinedAt = DateTime.UtcNow
        });

        newRoom.Participants.Add(new ConversationParticipant
        {
            Id = Guid.NewGuid(),
            ConversationId = newRoom.Id,
            UserId = request.RecipientUserId,
            JoinedAt = DateTime.UtcNow
        });

        _db.Conversations.Add(newRoom);
        await _db.SaveChangesAsync();

        return ApiResponse<ConversationResponseDto>.SuccessResponse(MapConversationDto(newRoom, currentUserId));
    }

    // Retrieves all inbox rooms for the current user
    public async Task<ApiResponse<List<ConversationResponseDto>>> GetMyConversationsAsync(Guid currentUserId)
    {
        var rooms = await _db.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Include(c => c.Messages)
            .Where(c => c.Participants.Any(p => p.UserId == currentUserId))
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToListAsync();

        var dtos = rooms.Select(r => MapConversationDto(r, currentUserId)).ToList();
        return ApiResponse<List<ConversationResponseDto>>.SuccessResponse(dtos);
    }

    // Retrieves message history for a specific room (filters out 'Delete for Me' messages)
    public async Task<ApiResponse<PagedResult<ChatMessageResponseDto>>> GetMessagesAsync(
        Guid currentUserId, Guid conversationId, int page = 1, int pageSize = 50)
    {
        var isParticipant = await _db.ConversationParticipants
            .AnyAsync(p => p.ConversationId == conversationId && p.UserId == currentUserId);

        if (!isParticipant)
            return ApiResponse<PagedResult<ChatMessageResponseDto>>.FailureResponse("Unauthorized to view this conversation.");

        var query = _db.ChatMessages
            .Include(m => m.SenderUser)
            .Include(m => m.UserDeletions)
            .Where(m => m.ConversationId == conversationId &&
                        !m.UserDeletions.Any(d => d.UserId == currentUserId))
            .OrderByDescending(m => m.SentAt);

        var totalCount = await query.CountAsync();
        var messages = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        // Chronological order (oldest to newest) for client UI display
        messages.Reverse();

        var dtos = messages.Select(m => MapMessageDto(m, currentUserId)).ToList();
        return ApiResponse<PagedResult<ChatMessageResponseDto>>.SuccessResponse(
            new PagedResult<ChatMessageResponseDto>(dtos, totalCount, page, pageSize));
    }

    // Saves message in SQL Server & broadcasts instantly via SignalR
    public async Task<ApiResponse<ChatMessageResponseDto>> SendMessageAsync(
        Guid currentUserId, Guid conversationId, SendMessageRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Content) && string.IsNullOrWhiteSpace(request.AttachmentUrl))
            return ApiResponse<ChatMessageResponseDto>.FailureResponse("Message content or attachment is required.");

        var isParticipant = await _db.ConversationParticipants
            .AnyAsync(p => p.ConversationId == conversationId && p.UserId == currentUserId);

        if (!isParticipant)
            return ApiResponse<ChatMessageResponseDto>.FailureResponse("You are not a participant in this conversation.");

        var sender = await _db.Users.FindAsync(currentUserId);

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderUserId = currentUserId,
            Content = request.Content.Trim(),
            DeliveryStatus = MessageDeliveryStatus.Sent,
            SentAt = DateTime.UtcNow,
            AttachmentUrl = request.AttachmentUrl,
            AttachmentFileName = request.AttachmentFileName,
            AttachmentType = request.AttachmentType,
            AttachmentSizeBytes = request.AttachmentSizeBytes
        };

        _db.ChatMessages.Add(message);

        // Update last message timestamp on room
        var conversation = await _db.Conversations.FindAsync(conversationId);
        if (conversation != null)
        {
            conversation.LastMessageAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        var messageDto = MapMessageDto(message, currentUserId, sender?.Email ?? "User");

        // Broadcast real-time message via SignalR abstraction
        await _notificationService.BroadcastMessageAsync(conversationId, messageDto);

        return ApiResponse<ChatMessageResponseDto>.SuccessResponse(messageDto);
    }

    // Marks unread messages as Read (updates blue double ticks in real-time)
    public async Task<ApiResponse<bool>> MarkAsReadAsync(Guid currentUserId, Guid conversationId)
    {
        var participant = await _db.ConversationParticipants
            .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == currentUserId);

        if (participant == null)
            return ApiResponse<bool>.FailureResponse("Not a participant.");

        var unreadMessages = await _db.ChatMessages
            .Where(m => m.ConversationId == conversationId &&
                        m.SenderUserId != currentUserId &&
                        m.DeliveryStatus != MessageDeliveryStatus.Read)
            .ToListAsync();

        if (unreadMessages.Any())
        {
            foreach (var msg in unreadMessages)
            {
                msg.DeliveryStatus = MessageDeliveryStatus.Read;
                msg.ReadAt = DateTime.UtcNow;
            }

            participant.LastReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Real-time broadcast: notify sender that their messages were read
            await _notificationService.BroadcastMessagesReadAsync(conversationId);
        }

        return ApiResponse<bool>.SuccessResponse(true);
    }

    // Edits a previously sent message
    public async Task<ApiResponse<ChatMessageResponseDto>> EditMessageAsync(
        Guid currentUserId, Guid messageId, EditMessageRequestDto request)
    {
        var message = await _db.ChatMessages
            .Include(m => m.SenderUser)
            .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderUserId == currentUserId);

        if (message == null)
            return ApiResponse<ChatMessageResponseDto>.FailureResponse("Message not found or you are not authorized to edit it.");

        if (message.IsDeletedForEveryone)
            return ApiResponse<ChatMessageResponseDto>.FailureResponse("Cannot edit a deleted message.");

        message.Content = request.NewContent.Trim();
        message.IsEdited = true;
        message.EditedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var dto = MapMessageDto(message, currentUserId);

        // Real-time broadcast: notify room of edited text
        await _notificationService.BroadcastMessageEditedAsync(message.ConversationId, message.Id, message.Content);

        return ApiResponse<ChatMessageResponseDto>.SuccessResponse(dto);
    }

    // Hides message only for current user ("Delete for Me")
    public async Task<ApiResponse<bool>> DeleteForMeAsync(Guid currentUserId, Guid messageId)
    {
        var exists = await _db.ChatMessageUserDeletions
            .AnyAsync(d => d.ChatMessageId == messageId && d.UserId == currentUserId);

        if (!exists)
        {
            _db.ChatMessageUserDeletions.Add(new ChatMessageUserDeletion
            {
                Id = Guid.NewGuid(),
                ChatMessageId = messageId,
                UserId = currentUserId,
                DeletedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        return ApiResponse<bool>.SuccessResponse(true);
    }

    // Deletes message for all participants in the room ("Delete for Everyone")
    public async Task<ApiResponse<bool>> DeleteForEveryoneAsync(Guid currentUserId, Guid messageId)
    {
        var message = await _db.ChatMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderUserId == currentUserId);

        if (message == null)
            return ApiResponse<bool>.FailureResponse("Message not found or unauthorized.");

        message.IsDeletedForEveryone = true;
        message.Content = "This message was deleted";
        message.AttachmentUrl = null;

        await _db.SaveChangesAsync();

        // Real-time broadcast: notify everyone in the room
        await _notificationService.BroadcastMessageDeletedAsync(message.ConversationId, message.Id);

        return ApiResponse<bool>.SuccessResponse(true);
    }

    #region Dynamic Role Detection & Boundary Validation

    private enum MarketplaceUserRole
    {
        SuperAdmin,
        PlatformPartner,
        Merchant,
        Affiliate,
        Customer
    }

    // Dynamically resolves a user's marketplace role by querying database entities
    private async Task<MarketplaceUserRole> ResolveUserRoleAsync(User user)
    {
        // 1. SuperAdmin / Admin Role check
        if (user.Role != null && (user.Role.Name.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                                  user.Role.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
        {
            return MarketplaceUserRole.SuperAdmin;
        }

        // 2. Platform Partner check (Entity + Role)
        var isPartner = await _db.Partners.AnyAsync(p => p.UserId == user.Id && p.IsActive);
        if (isPartner || (user.Role != null && (user.Role.Name.Equals("Partner", StringComparison.OrdinalIgnoreCase) ||
                                               user.Role.Name.Equals("PlatformPartner", StringComparison.OrdinalIgnoreCase))))
        {
            return MarketplaceUserRole.PlatformPartner;
        }

        // 3. Merchant check (Entity + Role)
        var isMerchant = await _db.Merchants.AnyAsync(m => m.OwnerUserId == user.Id && m.IsActive);
        if (isMerchant || (user.Role != null && user.Role.Name.Equals("Merchant", StringComparison.OrdinalIgnoreCase)))
        {
            return MarketplaceUserRole.Merchant;
        }

        // 4. Affiliate check (Entity + Role)
        var isAffiliate = await _db.Affiliates.AnyAsync(a => a.UserId == user.Id && a.IsActive);
        if (isAffiliate || (user.Role != null && user.Role.Name.Equals("Affiliate", StringComparison.OrdinalIgnoreCase)))
        {
            return MarketplaceUserRole.Affiliate;
        }

        // 5. Default role is Customer (Buyer)
        return MarketplaceUserRole.Customer;
    }

    // Dynamically matches pair of roles to the 8 supported marketplace conversation channels
    private static ConversationType? MatchConversationType(MarketplaceUserRole roleA, MarketplaceUserRole roleB)
    {
        if ((roleA == MarketplaceUserRole.SuperAdmin && roleB == MarketplaceUserRole.PlatformPartner) ||
            (roleB == MarketplaceUserRole.SuperAdmin && roleA == MarketplaceUserRole.PlatformPartner))
            return ConversationType.SuperAdminToPartner;

        if ((roleA == MarketplaceUserRole.SuperAdmin && roleB == MarketplaceUserRole.Merchant) ||
            (roleB == MarketplaceUserRole.SuperAdmin && roleA == MarketplaceUserRole.Merchant))
            return ConversationType.SuperAdminToMerchant;

        if ((roleA == MarketplaceUserRole.SuperAdmin && roleB == MarketplaceUserRole.Affiliate) ||
            (roleB == MarketplaceUserRole.SuperAdmin && roleA == MarketplaceUserRole.Affiliate))
            return ConversationType.SuperAdminToAffiliate;

        if ((roleA == MarketplaceUserRole.PlatformPartner && roleB == MarketplaceUserRole.Merchant) ||
            (roleB == MarketplaceUserRole.PlatformPartner && roleA == MarketplaceUserRole.Merchant))
            return ConversationType.PartnerToMerchant;

        if ((roleA == MarketplaceUserRole.PlatformPartner && roleB == MarketplaceUserRole.Customer) ||
            (roleB == MarketplaceUserRole.PlatformPartner && roleA == MarketplaceUserRole.Customer))
            return ConversationType.PartnerToCustomer;

        if ((roleA == MarketplaceUserRole.Merchant && roleB == MarketplaceUserRole.Affiliate) ||
            (roleB == MarketplaceUserRole.Merchant && roleA == MarketplaceUserRole.Affiliate))
            return ConversationType.MerchantToAffiliate;

        if ((roleA == MarketplaceUserRole.Merchant && roleB == MarketplaceUserRole.Customer) ||
            (roleB == MarketplaceUserRole.Merchant && roleA == MarketplaceUserRole.Customer))
            return ConversationType.MerchantToCustomer;

        if ((roleA == MarketplaceUserRole.Affiliate && roleB == MarketplaceUserRole.Customer) ||
            (roleB == MarketplaceUserRole.Affiliate && roleA == MarketplaceUserRole.Customer))
            return ConversationType.AffiliateToCustomer;

        return null;
    }

    // Validates business boundaries dynamically (Order ownership, Merchant active status)
    private async Task<(bool Success, string Message)> ValidateBusinessBoundariesAsync(
        Guid currentUserId, CreateConversationRequestDto request, MarketplaceUserRole roleA, MarketplaceUserRole roleB)
    {
        // Dynamic Order verification
        if (request.OrderId.HasValue)
        {
            var order = await _db.Orders
                .Include(o => o.SubOrders)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId.Value || o.ParentOrderId == request.OrderId.Value);

            if (order == null)
                return (false, "The specified order was not found.");

            // Ensure at least one participant is the buyer, unless SuperAdmin is inspecting
            var isBuyerInvolved = order.UserId == currentUserId || order.UserId == request.RecipientUserId;
            if (!isBuyerInvolved && roleA != MarketplaceUserRole.SuperAdmin && roleB != MarketplaceUserRole.SuperAdmin)
                return (false, "Neither participant is the customer who placed this order.");
        }

        // Dynamic Merchant store verification
        if (request.MerchantId.HasValue)
        {
            var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.Id == request.MerchantId.Value);
            if (merchant == null || !merchant.IsActive)
                return (false, "The referenced merchant store does not exist or is currently inactive.");

            // Ensure merchant owner is a participant, unless SuperAdmin or Partner is involved
            var isMerchantOwnerInvolved = merchant.OwnerUserId == currentUserId || merchant.OwnerUserId == request.RecipientUserId;
            if (!isMerchantOwnerInvolved &&
                roleA != MarketplaceUserRole.SuperAdmin && roleB != MarketplaceUserRole.SuperAdmin &&
                roleA != MarketplaceUserRole.PlatformPartner && roleB != MarketplaceUserRole.PlatformPartner)
            {
                return (false, "Neither participant is the registered owner of this merchant store.");
            }
        }

        return (true, string.Empty);
    }

    #endregion

    #region DTO Mappers

    private static ConversationResponseDto MapConversationDto(Conversation c, Guid currentUserId)
    {
        var otherParticipant = c.Participants.FirstOrDefault(p => p.UserId != currentUserId)?.User;
        var myParticipant = c.Participants.FirstOrDefault(p => p.UserId == currentUserId);

        var lastMessage = c.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault();

        var unreadCount = c.Messages.Count(m =>
            m.SenderUserId != currentUserId &&
            (myParticipant?.LastReadAt == null || m.SentAt > myParticipant.LastReadAt));

        return new ConversationResponseDto
        {
            ConversationId = c.Id,
            Title = c.Title ?? "Direct Chat",
            ConversationType = c.Type.ToString(),
            OrderId = c.OrderId,
            MerchantId = c.MerchantId,
            OtherParticipantUserId = otherParticipant?.Id ?? Guid.Empty,
            OtherParticipantName = otherParticipant?.Email ?? "User",
            UnreadCount = unreadCount,
            LastMessageContent = lastMessage != null
                ? (lastMessage.IsDeletedForEveryone ? "This message was deleted" : lastMessage.Content)
                : null,
            LastMessageAt = c.LastMessageAt ?? c.CreatedAt
        };
    }

    private static ChatMessageResponseDto MapMessageDto(ChatMessage m, Guid currentUserId, string? senderName = null) =>
        new()
        {
            MessageId = m.Id,
            ConversationId = m.ConversationId,
            SenderUserId = m.SenderUserId,
            SenderName = senderName ?? m.SenderUser?.Email ?? "User",
            Content = m.IsDeletedForEveryone ? "This message was deleted" : m.Content,
            DeliveryStatus = m.DeliveryStatus.ToString(),
            SentAt = m.SentAt,
            ReadAt = m.ReadAt,
            IsEdited = m.IsEdited,
            IsDeletedForEveryone = m.IsDeletedForEveryone,
            IsMine = m.SenderUserId == currentUserId,
            AttachmentUrl = m.AttachmentUrl,
            AttachmentFileName = m.AttachmentFileName,
            AttachmentType = m.AttachmentType,
            AttachmentSizeBytes = m.AttachmentSizeBytes
        };

    #endregion
}
