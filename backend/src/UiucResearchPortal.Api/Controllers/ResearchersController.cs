using Microsoft.AspNetCore.Mvc;
using UiucResearchPortal.Application.Abstractions;

namespace UiucResearchPortal.Api.Controllers;

[ApiController]
[Route("api/researchers")]
public class ResearchersController : ControllerBase
{
    private readonly IPortalQueryService _portalQueryService;

    public ResearchersController(IPortalQueryService portalQueryService)
    {
        _portalQueryService = portalQueryService;
    }

    [HttpGet]
    public async Task<IActionResult> SearchResearchers(
        [FromQuery] string? search,
        [FromQuery] int limit = 24,
        CancellationToken ct = default)
    {
        var researchers = await _portalQueryService.SearchResearchersAsync(search, limit, ct);
        return Ok(researchers);
    }
}
