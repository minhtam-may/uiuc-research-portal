using Microsoft.AspNetCore.Mvc;
using UiucResearchPortal.Application.Dtos;
using UiucResearchPortal.Application.Services;

namespace UiucResearchPortal.Api.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly ResearchChatService _chatService;

    public ChatController(ResearchChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] ChatRequestDto request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest("Question must not be empty.");
        }

        var response = await _chatService.AnswerAsync(request.Question, ct);
        return Ok(response);
    }
}
