using Microsoft.AspNetCore.Mvc;
using UiucResearchPortal.Application.Abstractions;

namespace UiucResearchPortal.Api.Controllers;

[ApiController]
[Route("api/papers")]
public class PapersController : ControllerBase
{
    private readonly IPortalQueryService _portalQueryService;

    public PapersController(IPortalQueryService portalQueryService)
    {
        _portalQueryService = portalQueryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecentPapers([FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var papers = await _portalQueryService.GetRecentPapersAsync(limit, ct);
        return Ok(papers);
    }
}
