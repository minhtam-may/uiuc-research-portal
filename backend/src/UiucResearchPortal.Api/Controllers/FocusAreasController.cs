using Microsoft.AspNetCore.Mvc;
using UiucResearchPortal.Application.Abstractions;

namespace UiucResearchPortal.Api.Controllers;

[ApiController]
[Route("api/focus-areas")]
public class FocusAreasController : ControllerBase
{
    private readonly IPortalQueryService _portalQueryService;

    public FocusAreasController(IPortalQueryService portalQueryService)
    {
        _portalQueryService = portalQueryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTopFocusAreas([FromQuery] int limit = 12, CancellationToken ct = default)
    {
        var focusAreas = await _portalQueryService.GetTopFocusAreasAsync(limit, ct);
        return Ok(focusAreas);
    }
}
