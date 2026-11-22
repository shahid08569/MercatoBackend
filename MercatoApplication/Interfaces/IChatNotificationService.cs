using MercatoApplication.DTOs.Chat;

namespace MercatoApplication.Interfaces;

/// <summary>
/// Real-time notification abstraction for broadcasting chat events.
/// Decouples Application/Infrastructure from SignalR Hubs to prevent circular references.
/// </summary>
public interface IChatNotificationService
{
    // Broadcasts incoming message to active conversation room participants
    Task BroadcastMessageAsync(Guid conversationId, ChatMessageResponseDto message);

    // Notifies room participants that their sent messages were marked as read (turns double ticks blue)
    Task BroadcastMessagesReadAsync(Guid conversationId);

    // Broadcasts edited message text updates to the room
    Task BroadcastMessageEditedAsync(Guid conversationId, Guid messageId, string newContent);

    // Broadcasts message deletion event to all participants
    Task BroadcastMessageDeletedAsync(Guid conversationId, Guid messageId);
}
