using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Api.Storefront;
using SeraphineFlowers.Infrastructure.Persistence;

namespace SeraphineFlowers.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/storefront")]
public sealed class StorefrontAnalyticsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("analytics")]
    public async Task<ActionResult<AnalyticsSnapshotDto>> GetAnalytics(CancellationToken cancellationToken)
    {
        var visitors = await dbContext.VisitorProfiles.AsNoTracking().OrderByDescending(item => item.LastSeen).ToListAsync(cancellationToken);
        var result = new AnalyticsSnapshotDto(
            visitors.Count,
            visitors.Sum(item => item.VisitCount),
            visitors.Select(item => new VisitorProfileDto(item.Id, item.VisitorKey, item.IpAddress, item.FirstSeen, item.LastSeen, item.VisitCount, item.FirstUtmJson, item.LocationJson, item.DeviceJson, item.SessionsJson)).ToArray());
        return Ok(result);
    }

    [HttpPost("analytics/backfill-places")]
    public async Task<IActionResult> BackfillPlaces(BackfillPlacesRequest request, CancellationToken cancellationToken)
    {
        foreach (var patch in request.Patches)
        {
            var visitor = await dbContext.VisitorProfiles.FirstOrDefaultAsync(item => item.Id == patch.VisitorId, cancellationToken);
            if (visitor is null)
            {
                continue;
            }

            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(visitor.LocationJson) ? "{}" : visitor.LocationJson);
            var values = document.RootElement.EnumerateObject().ToDictionary(item => item.Name, item => item.Value.Clone());
            using var output = new MemoryStream();
            await using var writer = new Utf8JsonWriter(output);
            writer.WriteStartObject();
            foreach (var value in values)
            {
                value.Value.WriteTo(writer);
            }
            writer.WriteString("placeName", patch.PlaceName);
            writer.WriteEndObject();
            await writer.FlushAsync(cancellationToken);
            visitor.LocationJson = System.Text.Encoding.UTF8.GetString(output.ToArray());
            visitor.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { success = true });
    }
}
