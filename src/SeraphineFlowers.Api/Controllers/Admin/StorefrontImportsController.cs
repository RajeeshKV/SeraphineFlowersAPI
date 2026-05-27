using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SeraphineFlowers.Api.Storefront;
using SeraphineFlowers.Domain.Entities;
using SeraphineFlowers.Infrastructure.Persistence;
using SeraphineFlowers.Infrastructure.Storefront;

namespace SeraphineFlowers.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/storefront")]
public sealed class StorefrontImportsController(
    AppDbContext dbContext,
    IOptions<StorefrontOptions> storefrontOptions,
    CloudinaryStorefrontService cloudinaryStorefrontService) : ControllerBase
{
    private readonly StorefrontOptions _storefrontOptions = storefrontOptions.Value;

    [HttpPost("imports/customers")]
    public async Task<IActionResult> ImportCustomers(CancellationToken cancellationToken)
    {
        var document = await cloudinaryStorefrontService.FetchRawJsonAsync(_storefrontOptions.Imports.Customers.Folder, _storefrontOptions.Imports.Customers.PublicId, cancellationToken);
        if (document is null || !document.RootElement.TryGetProperty("customers", out var customers))
        {
            return Ok(new { imported = 0 });
        }

        var imported = 0;
        foreach (var item in customers.EnumerateArray())
        {
            var phone = StorefrontSupport.NormalizePhone(item.TryGetProperty("phone", out var phoneValue) ? phoneValue.GetString() : null);
            if (string.IsNullOrWhiteSpace(phone))
            {
                continue;
            }

            var customer = await dbContext.Customers.FirstOrDefaultAsync(entity => entity.Phone == phone, cancellationToken) ?? new Customer { Phone = phone };
            customer.Name = item.TryGetProperty("name", out var nameValue) ? nameValue.GetString() ?? string.Empty : string.Empty;
            customer.RegisteredAt = StorefrontSupport.ParseDate(item.TryGetProperty("registeredAt", out var registeredAt) ? registeredAt.GetString() : null) ?? customer.RegisteredAt;
            customer.OrderCount = item.TryGetProperty("orderCount", out var orderCount) && orderCount.TryGetInt32(out var parsedOrderCount) ? parsedOrderCount : 0;
            customer.OfferCode = StorefrontSupport.ComputeOfferCode(customer.OrderCount, _storefrontOptions.Offer);
            customer.Notes = item.TryGetProperty("notes", out var notesValue) ? notesValue.GetString() ?? string.Empty : string.Empty;
            customer.LastOrderAt = StorefrontSupport.ParseDate(item.TryGetProperty("lastOrderAt", out var lastOrderAt) ? lastOrderAt.GetString() : null);
            customer.UpdatedAt = DateTimeOffset.UtcNow;

            if (dbContext.Entry(customer).State == EntityState.Detached)
            {
                dbContext.Customers.Add(customer);
            }

            imported += 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { imported });
    }

    [HttpPost("imports/reviews")]
    public async Task<IActionResult> ImportReviews(CancellationToken cancellationToken)
    {
        var document = await cloudinaryStorefrontService.FetchRawJsonAsync(_storefrontOptions.Imports.Reviews.Folder, _storefrontOptions.Imports.Reviews.PublicId, cancellationToken);
        if (document is null || !document.RootElement.TryGetProperty("reviews", out var reviews))
        {
            return Ok(new { imported = 0 });
        }

        var imported = 0;
        foreach (var item in reviews.EnumerateArray())
        {
            var phone = StorefrontSupport.NormalizePhone(item.TryGetProperty("phone", out var phoneValue) ? phoneValue.GetString() : null);
            if (string.IsNullOrWhiteSpace(phone))
            {
                continue;
            }

            var review = await dbContext.CustomerReviews.FirstOrDefaultAsync(entity => entity.Phone == phone, cancellationToken) ?? new CustomerReview { Phone = phone };
            review.Name = item.TryGetProperty("name", out var nameValue) ? nameValue.GetString() ?? string.Empty : string.Empty;
            review.Rating = item.TryGetProperty("rating", out var ratingValue) && ratingValue.TryGetInt32(out var parsedRating) ? parsedRating : 5;
            review.Text = item.TryGetProperty("text", out var textValue) ? textValue.GetString() ?? string.Empty : string.Empty;
            review.Status = item.TryGetProperty("status", out var statusValue) ? statusValue.GetString() ?? "approved" : "approved";
            review.CreatedAt = StorefrontSupport.ParseDate(item.TryGetProperty("createdAt", out var createdAt) ? createdAt.GetString() : null) ?? review.CreatedAt;
            review.UpdatedAt = DateTimeOffset.UtcNow;

            if (dbContext.Entry(review).State == EntityState.Detached)
            {
                dbContext.CustomerReviews.Add(review);
            }

            imported += 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { imported });
    }

    [HttpPost("imports/flagged")]
    public async Task<IActionResult> ImportFlagged(CancellationToken cancellationToken)
    {
        var document = await cloudinaryStorefrontService.FetchRawJsonAsync(_storefrontOptions.Imports.Flagged.Folder, _storefrontOptions.Imports.Flagged.PublicId, cancellationToken);
        if (document is null || !document.RootElement.TryGetProperty("flagged", out var flagged))
        {
            return Ok(new { imported = 0 });
        }

        var imported = 0;
        foreach (var item in flagged.EnumerateArray())
        {
            var phone = StorefrontSupport.NormalizePhone(item.TryGetProperty("phone", out var phoneValue) ? phoneValue.GetString() : null);
            if (string.IsNullOrWhiteSpace(phone))
            {
                continue;
            }

            var entity = await dbContext.FlaggedCustomers.FirstOrDefaultAsync(record => record.Phone == phone, cancellationToken) ?? new FlaggedCustomer { Phone = phone };
            entity.Reason = item.TryGetProperty("reason", out var reasonValue) ? reasonValue.GetString() ?? string.Empty : string.Empty;
            entity.FlaggedAt = StorefrontSupport.ParseDate(item.TryGetProperty("flaggedAt", out var flaggedAt) ? flaggedAt.GetString() : null) ?? entity.FlaggedAt;
            if (dbContext.Entry(entity).State == EntityState.Detached)
            {
                dbContext.FlaggedCustomers.Add(entity);
            }

            imported += 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { imported });
    }

    [HttpPost("imports/promo")]
    public async Task<IActionResult> ImportPromo(CancellationToken cancellationToken)
    {
        var document = await cloudinaryStorefrontService.FetchRawJsonAsync(_storefrontOptions.Imports.Promo.Folder, _storefrontOptions.Imports.Promo.PublicId, cancellationToken);
        if (document is null)
        {
            return Ok(new { imported = 0 });
        }

        var root = document.RootElement;
        var promo = await dbContext.PromoCampaigns.OrderByDescending(item => item.UpdatedAt).FirstOrDefaultAsync(cancellationToken) ?? new PromoCampaign();
        if (dbContext.Entry(promo).State == EntityState.Detached)
        {
            dbContext.PromoCampaigns.Add(promo);
        }

        promo.Active = root.TryGetProperty("active", out var active) && active.GetBoolean();
        promo.ActiveFrom = StorefrontSupport.ParseDate(root.TryGetProperty("activeFrom", out var activeFrom) ? activeFrom.GetString() : null);
        promo.ActiveTo = StorefrontSupport.ParseDate(root.TryGetProperty("activeTo", out var activeTo) ? activeTo.GetString() : null);
        promo.AdName = root.TryGetProperty("adName", out var adName) ? adName.GetString() ?? string.Empty : string.Empty;
        promo.AdDescription = root.TryGetProperty("adDescription", out var description) ? description.GetString() ?? string.Empty : string.Empty;
        promo.RedirectType = root.TryGetProperty("redirectType", out var redirectType) ? redirectType.GetString() ?? "none" : "none";
        promo.RedirectValue = root.TryGetProperty("redirectValue", out var redirectValue) ? redirectValue.GetString() : null;
        promo.CtaText = root.TryGetProperty("ctaText", out var ctaText) ? ctaText.GetString() ?? "Order Now" : "Order Now";
        promo.ShowOncePerDay = !root.TryGetProperty("showOncePerDay", out var showOncePerDay) || showOncePerDay.GetBoolean();
        promo.ImagesJson = root.TryGetProperty("images", out var images) ? images.GetRawText() : "[]";
        promo.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { imported = 1 });
    }

    [HttpPost("imports/analytics")]
    public async Task<IActionResult> ImportAnalytics(CancellationToken cancellationToken)
    {
        var document = await cloudinaryStorefrontService.FetchRawJsonAsync(_storefrontOptions.Imports.Analytics.Folder, _storefrontOptions.Imports.Analytics.PublicId, cancellationToken);
        if (document is null || !document.RootElement.TryGetProperty("visitors", out var visitors))
        {
            return Ok(new { imported = 0 });
        }

        var imported = 0;
        foreach (var item in visitors.EnumerateArray())
        {
            var visitorKey = item.TryGetProperty("id", out var idValue) ? idValue.GetString() ?? string.Empty : string.Empty;
            if (string.IsNullOrWhiteSpace(visitorKey))
            {
                continue;
            }

            var entity = await dbContext.VisitorProfiles.FirstOrDefaultAsync(record => record.VisitorKey == visitorKey, cancellationToken) ?? new VisitorProfile { VisitorKey = visitorKey };
            entity.IpAddress = item.TryGetProperty("ip", out var ipValue) ? ipValue.GetString() ?? string.Empty : string.Empty;
            entity.FirstSeen = StorefrontSupport.ParseDate(item.TryGetProperty("firstSeen", out var firstSeen) ? firstSeen.GetString() : null) ?? entity.FirstSeen;
            entity.LastSeen = StorefrontSupport.ParseDate(item.TryGetProperty("lastSeen", out var lastSeen) ? lastSeen.GetString() : null) ?? entity.LastSeen;
            entity.VisitCount = item.TryGetProperty("visitCount", out var visitCount) && visitCount.TryGetInt32(out var parsedVisitCount) ? parsedVisitCount : entity.VisitCount;
            entity.FirstUtmJson = item.TryGetProperty("firstUtm", out var firstUtm) ? firstUtm.GetRawText() : entity.FirstUtmJson;
            entity.LocationJson = item.TryGetProperty("location", out var location) ? location.GetRawText() : entity.LocationJson;
            entity.DeviceJson = item.TryGetProperty("device", out var device) ? device.GetRawText() : entity.DeviceJson;
            entity.SessionsJson = item.TryGetProperty("sessions", out var sessions) ? sessions.GetRawText() : entity.SessionsJson;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            if (dbContext.Entry(entity).State == EntityState.Detached)
            {
                dbContext.VisitorProfiles.Add(entity);
            }

            imported += 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { imported });
    }

    [HttpPost("imports/media-configs/{collectionKey}")]
    public async Task<IActionResult> ImportMediaConfigs(string collectionKey, CancellationToken cancellationToken)
    {
        var source = StorefrontMediaSupport.ResolveImportSource(collectionKey, _storefrontOptions);
        var document = await cloudinaryStorefrontService.FetchRawJsonAsync(source.Folder, source.PublicId, cancellationToken);
        if (document is null || document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return Ok(new { imported = 0 });
        }

        var existing = await dbContext.MediaAssetConfigs.Where(item => item.CollectionKey == collectionKey).ToListAsync(cancellationToken);
        dbContext.MediaAssetConfigs.RemoveRange(existing);

        var imported = 0;
        foreach (var property in document.RootElement.EnumerateObject())
        {
            var imageName = property.Value.TryGetProperty("imageName", out var imageNameValue)
                ? imageNameValue.GetString() ?? property.Name
                : property.Name;
            var imageUrl = property.Value.TryGetProperty("imageUrl", out var imageUrlValue)
                ? imageUrlValue.GetString()
                : null;

            dbContext.MediaAssetConfigs.Add(new MediaAssetConfig
            {
                CollectionKey = collectionKey,
                FolderName = source.Folder,
                ImageName = imageName,
                ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? cloudinaryStorefrontService.BuildImageUrl(source.Folder, imageName) : imageUrl,
                DisplayName = property.Value.TryGetProperty("name", out var nameValue) ? nameValue.GetString() ?? imageName : imageName,
                Description = property.Value.TryGetProperty("description", out var descriptionValue) ? descriptionValue.GetString() ?? string.Empty : string.Empty,
                Amount = property.Value.TryGetProperty("amount", out var amountValue) && amountValue.TryGetDecimal(out var parsedAmount) ? parsedAmount : null,
                SortOrder = imported,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            imported += 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { imported });
    }
}
