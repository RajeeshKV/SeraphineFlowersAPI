using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SeraphineFlowers.Api.Storefront;
using SeraphineFlowers.Infrastructure.Persistence;
using SeraphineFlowers.Infrastructure.Storefront;

namespace SeraphineFlowers.Api.Controllers.Public;

[ApiController]
[Route("api/public")]
public sealed class StorefrontBootstrapController(
    AppDbContext dbContext,
    IOptions<StorefrontOptions> storefrontOptions) : ControllerBase
{
    private readonly StorefrontOptions _storefrontOptions = storefrontOptions.Value;

    [HttpGet("bootstrap")]
    public async Task<ActionResult<PublicBootstrapDto>> GetBootstrap([FromQuery] string? phone, CancellationToken cancellationToken)
    {
        var normalizedPhone = StorefrontSupport.NormalizePhone(phone);
        var customer = string.IsNullOrWhiteSpace(normalizedPhone)
            ? null
            : await dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(item => item.Phone == normalizedPhone, cancellationToken);

        var promo = await dbContext.PromoCampaigns.AsNoTracking().OrderByDescending(item => item.UpdatedAt).FirstOrDefaultAsync(cancellationToken);
        var reviews = await dbContext.CustomerReviews.AsNoTracking()
            .Where(item => item.Status == "approved")
            .OrderByDescending(item => item.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        return Ok(new PublicBootstrapDto(
            customer is null ? null : StorefrontSupport.ToCustomerDto(customer, _storefrontOptions.Offer, true),
            promo is null ? null : StorefrontSupport.ToPromoDto(promo),
            reviews.Select(StorefrontSupport.ToReviewDto).ToArray(),
            new StorefrontConfigDto(
                _storefrontOptions.RequireCustomerOtp,
                new FirebaseConfigDto(_storefrontOptions.Firebase.Enabled, _storefrontOptions.Firebase.ProjectId),
                _storefrontOptions.Offer.WelcomeDiscount,
                _storefrontOptions.Offer.LoyaltyDiscount,
                _storefrontOptions.Offer.LoyaltyEvery)));
    }
}
