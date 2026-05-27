using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Api.Storefront;
using SeraphineFlowers.Infrastructure.Persistence;

namespace SeraphineFlowers.Api.Controllers.Public;

[ApiController]
[Route("api/public")]
public sealed class StorefrontAnalyticsController(AppDbContext dbContext) : ControllerBase
{
    [HttpPost("analytics/visits")]
    public async Task<IActionResult> TrackVisit(TrackVisitRequest request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var ipAddress = GetClientIp();
        var visitorKey = request.VisitorKey?.Trim();
        if (string.IsNullOrWhiteSpace(visitorKey))
        {
            visitorKey = ipAddress;
        }

        var visitor = await dbContext.VisitorProfiles.FirstOrDefaultAsync(item => item.VisitorKey == visitorKey, cancellationToken);
        var sessionEntry = new
        {
            timestamp = now,
            referrer = request.Referrer ?? "direct",
            page = request.Page ?? "/",
            utm = request.Utm
        };

        if (visitor is null)
        {
            visitor = new SeraphineFlowers.Domain.Entities.VisitorProfile
            {
                VisitorKey = visitorKey,
                IpAddress = ipAddress,
                FirstSeen = now,
                LastSeen = now,
                VisitCount = 1,
                FirstUtmJson = request.Utm is null ? null : JsonSerializer.Serialize(request.Utm),
                LocationJson = JsonSerializer.Serialize(request.Location ?? new { granted = false }),
                DeviceJson = JsonSerializer.Serialize(request.Device ?? new { }),
                SessionsJson = JsonSerializer.Serialize(new[] { sessionEntry })
            };
            dbContext.VisitorProfiles.Add(visitor);
        }
        else
        {
            visitor.LastSeen = now;
            visitor.VisitCount += request.LocationOnly ? 0 : 1;
            visitor.UpdatedAt = now;
            if (request.Utm is not null && string.IsNullOrWhiteSpace(visitor.FirstUtmJson))
            {
                visitor.FirstUtmJson = JsonSerializer.Serialize(request.Utm);
            }

            if (request.Location is not null)
            {
                visitor.LocationJson = JsonSerializer.Serialize(request.Location);
            }

            if (request.Device is not null)
            {
                visitor.DeviceJson = JsonSerializer.Serialize(request.Device);
            }

            if (!request.LocationOnly)
            {
                var sessions = JsonSerializer.Deserialize<List<object>>(visitor.SessionsJson) ?? [];
                sessions.Insert(0, sessionEntry);
                visitor.SessionsJson = JsonSerializer.Serialize(sessions.Take(10));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { success = true });
    }

    private string GetClientIp()
    {
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
        {
            return forwarded.ToString().Split(',')[0].Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
