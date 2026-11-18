using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MercatoAPI.Hubs;

[Authorize]
public class ChatHub : Hub
{
    // Join a specific conversation room group
    public async Task JoinConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
    }

    // Leave a conversation room group
    public async Task LeaveConversation(string conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
    }

    // Live typing indicator (e.g. "Ali is typing...")
    public async Task SendTypingIndicator(string conversationId, string userName)
    {
        await Clients.OthersInGroup(conversationId).SendAsync("UserTyping", conversationId, userName);
    }

    // Live delivery & read status update (e.g., switches single tick to double/blue tick)
    public async Task UpdateMessageStatus(string conversationId, string messageId, string status)
    {
        await Clients.OthersInGroup(conversationId).SendAsync("MessageStatusUpdated", messageId, status);
    }

    // Broadcast when a message is edited in real-time
    public async Task NotifyMessageEdited(string conversationId, string messageId, string newContent)
    {
        await Clients.OthersInGroup(conversationId).SendAsync("MessageEdited", messageId, newContent);
    }

    // Broadcast when a message is deleted for everyone in real-time
    public async Task NotifyMessageDeleted(string conversationId, string messageId)
    {
        await Clients.OthersInGroup(conversationId).SendAsync("MessageDeleted", messageId);
    }

    // Track user online presence
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Clients.All.SendAsync("UserOnlineStatus", userId, true);
        }
        await base.OnConnectedAsync();
    }

    // Track user offline presence
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Clients.All.SendAsync("UserOnlineStatus", userId, false);
        }
        await base.OnDisconnectedAsync(exception);
    }
}