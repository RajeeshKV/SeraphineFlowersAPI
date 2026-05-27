using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Api.Storefront;
using SeraphineFlowers.Infrastructure.Persistence;

namespace SeraphineFlowers.Api.Controllers.Public;

[ApiController]
[Route("api/public")]
public sealed class StorefrontFlaggedController(AppDbContext dbContext) : ControllerBase
{
    [HttpPost("flagged/check")]
    public async Task<ActionResult<FlagCheckResponse>> CheckFlagged(FlagCheckRequest request, CancellationToken cancellationToken)
    {
        var normalizedPhone = StorefrontSupport.NormalizePhone(request.Phone);
        var flagged = await dbContext.FlaggedCustomers.AsNoTracking().AnyAsync(item => item.Phone == normalizedPhone, cancellationToken);
        return Ok(new FlagCheckResponse(flagged));
    }
}
