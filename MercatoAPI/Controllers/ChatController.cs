using System.Security.Claims;
using MercatoApplication.Common;
using MercatoApplication.DTOs.Chat;
using MercatoApplication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MercatoAPI.Controllers;

[ApiController]
[Route("api/v1/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    // POST /api/v1/chat/conversations - Starts or gets an existing conversation room
    [HttpPost("conversations")]
    public async Task<IActionResult> StartOrGetConversation([FromBody] CreateConversationRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _chatService.StartOrGetConversationAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // GET /api/v1/chat/conversations - Retrieves user's conversation inbox with unread badges
    [HttpGet("conversations")]
    public async Task<IActionResult> GetMyConversations()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _chatService.GetMyConversationsAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // GET /api/v1/chat/conversations/{id}/messages - Retrieves paginated message history
    [HttpGet("conversations/{id:guid}/messages")]
    public async Task<IActionResult> GetMessages([FromRoute] Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _chatService.GetMessagesAsync(userId, id, page, pageSize);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // POST /api/v1/chat/conversations/{id}/messages - Sends a new message (text / attachment)
    [HttpPost("conversations/{id:guid}/messages")]
    public async Task<IActionResult> SendMessage([FromRoute] Guid id, [FromBody] SendMessageRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _chatService.SendMessageAsync(userId, id, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // PUT /api/v1/chat/conversations/{id}/read - Marks unread messages as read (turns ticks blue)
    [HttpPut("conversations/{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead([FromRoute] Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _chatService.MarkAsReadAsync(userId, id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // PUT /api/v1/chat/messages/{id} - Edits a previously sent message
    [HttpPut("messages/{id:guid}")]
    public async Task<IActionResult> EditMessage([FromRoute] Guid id, [FromBody] EditMessageRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _chatService.EditMessageAsync(userId, id, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // DELETE /api/v1/chat/messages/{id}/me - Deletes message for current user only
    [HttpDelete("messages/{id:guid}/me")]
    public async Task<IActionResult> DeleteForMe([FromRoute] Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _chatService.DeleteForMeAsync(userId, id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // DELETE /api/v1/chat/messages/{id}/everyone - Deletes message for all room participants
    [HttpDelete("messages/{id:guid}/everyone")]
    public async Task<IActionResult> DeleteForEveryone([FromRoute] Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized user."));

        var result = await _chatService.DeleteForEveryoneAsync(userId, id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(idClaim, out var userId) ? userId : Guid.Empty;
    }
}
