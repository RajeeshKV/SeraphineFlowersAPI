using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SeraphineFlowers.Api.Controllers;

[ApiController]
[AllowAnonymous]
public sealed class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public ActionResult<HealthResponse> GetHealth()
    {
        return Ok(CreateResponse());
    }

    [HttpHead("/health")]
    public IActionResult HeadHealth()
    {
        return Ok();
    }

    [HttpGet("/")]
    public ActionResult<HealthResponse> GetRoot()
    {
        return Ok(CreateResponse());
    }

    [HttpHead("/")]
    public IActionResult HeadRoot()
    {
        return Ok();
    }

    private static HealthResponse CreateResponse()
    {
        return new HealthResponse("Healthy", "SeraphineFlowers.Api", DateTimeOffset.UtcNow);
    }
}

public sealed record HealthResponse(string Status, string Service, DateTimeOffset Timestamp);
