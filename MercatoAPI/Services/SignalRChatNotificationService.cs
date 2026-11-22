using MercatoAPI.Hubs;
using MercatoApplication.DTOs.Chat;
using MercatoApplication.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace MercatoAPI.Services;

/// <summary>
/// Implements real-time SignalR notifications for the Chat Engine.
/// Bridges the IChatNotificationService abstraction to the concrete ChatHub.
/// </summary>
public class SignalRChatNotificationService : IChatNotificationService
{
    private readonly IHubContext<ChatHub> _hubContext;

    public SignalRChatNotificationService(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task BroadcastMessageAsync(Guid conversationId, ChatMessageResponseDto message)
    {
        await _hubContext.Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", message);
    }

    public async Task BroadcastMessagesReadAsync(Guid conversationId)
    {
        await _hubContext.Clients.Group(conversationId.ToString()).SendAsync("MessagesMarkedAsRead", conversationId.ToString());
    }

    public async Task BroadcastMessageEditedAsync(Guid conversationId, Guid messageId, string newContent)
    {
        await _hubContext.Clients.Group(conversationId.ToString()).SendAsync("MessageEdited", messageId.ToString(), newContent);
    }

    public async Task BroadcastMessageDeletedAsync(Guid conversationId, Guid messageId)
    {
        await _hubContext.Clients.Group(conversationId.ToString()).SendAsync("MessageDeleted", messageId.ToString());
    }
}
