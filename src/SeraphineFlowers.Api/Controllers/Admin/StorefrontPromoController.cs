using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Api.Storefront;
using SeraphineFlowers.Domain.Entities;
using SeraphineFlowers.Infrastructure.Persistence;

namespace SeraphineFlowers.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/storefront")]
public sealed class StorefrontPromoController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("promo")]
    public async Task<ActionResult<PromoDto?>> GetPromo(CancellationToken cancellationToken)
    {
        var promo = await dbContext.PromoCampaigns.AsNoTracking().OrderByDescending(item => item.UpdatedAt).FirstOrDefaultAsync(cancellationToken);
        return Ok(promo is null ? null : StorefrontSupport.ToPromoDto(promo));
    }

    [HttpPut("promo")]
    public async Task<ActionResult<PromoDto>> SavePromo(AdminPromoRequest request, CancellationToken cancellationToken)
    {
        var promo = await dbContext.PromoCampaigns.OrderByDescending(item => item.UpdatedAt).FirstOrDefaultAsync(cancellationToken);
        if (promo is null)
        {
            promo = new PromoCampaign();
            dbContext.PromoCampaigns.Add(promo);
        }

        promo.Active = request.Active;
        promo.ActiveFrom = request.ActiveFrom;
        promo.ActiveTo = request.ActiveTo;
        promo.AdName = request.AdName.Trim();
        promo.AdDescription = request.AdDescription?.Trim() ?? string.Empty;
        promo.RedirectType = request.RedirectType.Trim();
        promo.RedirectValue = string.IsNullOrWhiteSpace(request.RedirectValue) ? null : request.RedirectValue.Trim();
        promo.CtaText = string.IsNullOrWhiteSpace(request.CtaText) ? "Order Now" : request.CtaText.Trim();
        promo.ShowOncePerDay = request.ShowOncePerDay;
        promo.ImagesJson = StorefrontSupport.SerializeStringList(request.Images);
        promo.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(StorefrontSupport.ToPromoDto(promo));
    }
}
