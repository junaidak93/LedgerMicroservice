using Microsoft.AspNetCore.Mvc;

namespace Ledger.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    [HttpGet("readiness")]
    public IActionResult GetReadiness()
    {
        // In a real implementation, check database connectivity, external services, etc.
        return Ok(new { status = "ready", timestamp = DateTime.UtcNow });
    }
}

