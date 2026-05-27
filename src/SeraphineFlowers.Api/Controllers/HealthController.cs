using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Infrastructure.Persistence;

namespace SeraphineFlowers.Api.Controllers;

[ApiController]
[AllowAnonymous]
public sealed class HealthController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("/health")]
    public async Task<ActionResult<HealthResponse>> GetHealth(CancellationToken cancellationToken)
    {
        return Ok(await CreateResponseAsync(cancellationToken));
    }

    [HttpHead("/health")]
    public async Task<IActionResult> HeadHealth(CancellationToken cancellationToken)
    {
        await ProbeDatabaseAsync(cancellationToken);
        return Ok();
    }

    [HttpGet("/")]
    public async Task<ActionResult<HealthResponse>> GetRoot(CancellationToken cancellationToken)
    {
        return Ok(await CreateResponseAsync(cancellationToken));
    }

    [HttpHead("/")]
    public async Task<IActionResult> HeadRoot(CancellationToken cancellationToken)
    {
        await ProbeDatabaseAsync(cancellationToken);
        return Ok();
    }

    private async Task<HealthResponse> CreateResponseAsync(CancellationToken cancellationToken)
    {
        await ProbeDatabaseAsync(cancellationToken);
        return new HealthResponse("Healthy", "SeraphineFlowers.Api", "Healthy", DateTimeOffset.UtcNow);
    }

    private async Task ProbeDatabaseAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
    }
}

public sealed record HealthResponse(string Status, string Service, string Database, DateTimeOffset Timestamp);
