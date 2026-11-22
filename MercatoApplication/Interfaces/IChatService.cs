using MercatoApplication.Common;
using MercatoApplication.DTOs.Chat;

namespace MercatoApplication.Interfaces;

public interface IChatService
{
    // Starts a new conversation room or retrieves an existing one (auto-detects relationship type)
    Task<ApiResponse<ConversationResponseDto>> StartOrGetConversationAsync(Guid currentUserId, CreateConversationRequestDto request);

    // Retrieves all active conversation rooms for the user (Inbox view with unread badges)
    Task<ApiResponse<List<ConversationResponseDto>>> GetMyConversationsAsync(Guid currentUserId);

    // Retrieves paginated chat history for a specific room (filters out 'Delete for Me' messages)
    Task<ApiResponse<PagedResult<ChatMessageResponseDto>>> GetMessagesAsync(Guid currentUserId, Guid conversationId, int page = 1, int pageSize = 50);

    // Sends a message: saves to SQL Server & broadcasts in real-time via SignalR
    Task<ApiResponse<ChatMessageResponseDto>> SendMessageAsync(Guid currentUserId, Guid conversationId, SendMessageRequestDto request);

    // Marks unread messages as Read (updates blue double ticks in real-time)
    Task<ApiResponse<bool>> MarkAsReadAsync(Guid currentUserId, Guid conversationId);

    // Edits a previously sent message
    Task<ApiResponse<ChatMessageResponseDto>> EditMessageAsync(Guid currentUserId, Guid messageId, EditMessageRequestDto request);

    // Hides message only for the current user ("Delete for Me")
    Task<ApiResponse<bool>> DeleteForMeAsync(Guid currentUserId, Guid messageId);

    // Deletes message for all participants in the room ("Delete for Everyone")
    Task<ApiResponse<bool>> DeleteForEveryoneAsync(Guid currentUserId, Guid messageId);
}                       