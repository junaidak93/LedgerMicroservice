using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ledger.API.DTOs;
using Ledger.API.Services;
using Ledger.API.Helpers;

namespace Ledger.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatsController : ControllerBase
{
    private readonly IStatsService _statsService;

    public StatsController(IStatsService statsService)
    {
        _statsService = statsService;
    }

    [HttpGet("global")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<GlobalStatsDto>> GetGlobalStats()
    {
        var result = await _statsService.GetGlobalStatsAsync();
        return Ok(result);
    }

    [HttpGet("users/{userId}")]
    public async Task<ActionResult<UserStatsDto>> GetUserStats(Guid userId)
    {
        var requestingUserId = ClaimsHelper.GetUserId(User);
        var isAdmin = ClaimsHelper.IsAdmin(User);

        if (!isAdmin && requestingUserId != userId)
        {
            return Forbid("You do not have permission to access this user's statistics");
        }

        var result = await _statsService.GetUserStatsAsync(userId);
        return Ok(result);
    }
}

