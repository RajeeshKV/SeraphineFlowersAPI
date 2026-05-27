using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Api.Storefront;
using SeraphineFlowers.Infrastructure.Persistence;

namespace SeraphineFlowers.Api.Controllers.Public;

[ApiController]
[Route("api/public")]
public sealed class StorefrontPromoController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("promo")]
    public async Task<ActionResult<PromoDto?>> GetPromo(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var promo = await dbContext.PromoCampaigns.AsNoTracking()
            .OrderByDescending(item => item.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (promo is null || !promo.Active)
        {
            return Ok(null);
        }

        if (promo.ActiveFrom is not null && promo.ActiveFrom > now)
        {
            return Ok(null);
        }

        if (promo.ActiveTo is not null && promo.ActiveTo < now)
        {
            return Ok(null);
        }

        return Ok(StorefrontSupport.ToPromoDto(promo));
    }
}
