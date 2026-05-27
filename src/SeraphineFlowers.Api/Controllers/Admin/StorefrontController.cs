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
public sealed class StorefrontController(
    AppDbContext dbContext,
    IOptions<StorefrontOptions> storefrontOptions,
    CloudinaryStorefrontService cloudinaryStorefrontService) : ControllerBase
{
    private readonly StorefrontOptions _storefrontOptions = storefrontOptions.Value;

    [HttpGet("customers")]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> GetCustomers(CancellationToken cancellationToken)
    {
        var customers = await dbContext.Customers.AsNoTracking()
            .OrderByDescending(item => item.RegisteredAt)
            .ToListAsync(cancellationToken);
        return Ok(customers.Select(item => StorefrontSupport.ToCustomerDto(item, _storefrontOptions.Offer, true)).ToArray());
    }

    [HttpPost("customers")]
    public async Task<ActionResult<CustomerDto>> UpsertCustomer(AdminCustomerUpsertRequest request, CancellationToken cancellationToken)
    {
        var normalizedPhone = StorefrontSupport.NormalizePhone(request.Phone);
        if (!StorefrontSupport.IsValidIndianPhone(normalizedPhone))
        {
            return BadRequest(new { error = "A valid 10-digit Indian mobile number is required." });
        }

        var customer = await dbContext.Customers.FirstOrDefaultAsync(item => item.Phone == normalizedPhone, cancellationToken);
        if (customer is null)
        {
            customer = new Customer
            {
                Phone = normalizedPhone,
                RegisteredAt = request.RegisteredAt ?? DateTimeOffset.UtcNow
            };
            dbContext.Customers.Add(customer);
        }

        customer.Name = request.Name.Trim();
        customer.OrderCount = Math.Max(0, request.OrderCount);
        customer.Notes = request.Notes?.Trim() ?? string.Empty;
        customer.LastOrderAt = request.LastOrderAt;
        customer.OfferCode = StorefrontSupport.ComputeOfferCode(customer.OrderCount, _storefrontOptions.Offer);
        customer.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(StorefrontSupport.ToCustomerDto(customer, _storefrontOptions.Offer, true));
    }

    [HttpPost("customers/{id:guid}/orders/increment")]
    public async Task<ActionResult<CustomerDto>> IncrementOrder(Guid id, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        customer.OrderCount += 1;
        customer.LastOrderAt = DateTimeOffset.UtcNow;
        customer.OfferCode = StorefrontSupport.ComputeOfferCode(customer.OrderCount, _storefrontOptions.Offer);
        customer.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(StorefrontSupport.ToCustomerDto(customer, _storefrontOptions.Offer, true));
    }

    [HttpDelete("customers/{id:guid}")]
    public async Task<IActionResult> DeleteCustomer(Guid id, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        dbContext.Customers.Remove(customer);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("reviews")]
    public async Task<ActionResult<IReadOnlyList<ReviewDto>>> GetReviews(CancellationToken cancellationToken)
    {
        var reviews = await dbContext.CustomerReviews.AsNoTracking().OrderByDescending(item => item.CreatedAt).ToListAsync(cancellationToken);
        return Ok(reviews.Select(StorefrontSupport.ToReviewDto).ToArray());
    }

    [HttpPost("reviews/{id:guid}/approve")]
    public async Task<IActionResult> ApproveReview(Guid id, CancellationToken cancellationToken)
    {
        return await UpdateReviewStatus(id, "approved", cancellationToken);
    }

    [HttpPost("reviews/{id:guid}/reject")]
    public async Task<IActionResult> RejectReview(Guid id, CancellationToken cancellationToken)
    {
        return await UpdateReviewStatus(id, "rejected", cancellationToken);
    }

    [HttpDelete("reviews/{id:guid}")]
    public async Task<IActionResult> DeleteReview(Guid id, CancellationToken cancellationToken)
    {
        var review = await dbContext.CustomerReviews.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (review is null)
        {
            return NotFound();
        }

        dbContext.CustomerReviews.Remove(review);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("flagged")]
    public async Task<ActionResult<IReadOnlyList<FlaggedCustomer>>> GetFlagged(CancellationToken cancellationToken)
    {
        var flagged = await dbContext.FlaggedCustomers.AsNoTracking().OrderByDescending(item => item.FlaggedAt).ToListAsync(cancellationToken);
        return Ok(flagged);
    }

    [HttpPost("flagged")]
    public async Task<ActionResult<FlaggedCustomer>> AddFlagged(AddFlaggedCustomerRequest request, CancellationToken cancellationToken)
    {
        var normalizedPhone = StorefrontSupport.NormalizePhone(request.Phone);
        if (!StorefrontSupport.IsValidIndianPhone(normalizedPhone))
        {
            return BadRequest(new { error = "A valid 10-digit Indian mobile number is required." });
        }

        var flagged = await dbContext.FlaggedCustomers.FirstOrDefaultAsync(item => item.Phone == normalizedPhone, cancellationToken);
        if (flagged is null)
        {
            flagged = new FlaggedCustomer
            {
                Phone = normalizedPhone,
                Reason = request.Reason?.Trim() ?? string.Empty
            };
            dbContext.FlaggedCustomers.Add(flagged);
        }
        else
        {
            flagged.Reason = request.Reason?.Trim() ?? flagged.Reason;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(flagged);
    }

    [HttpDelete("flagged/{phone}")]
    public async Task<IActionResult> DeleteFlagged(string phone, CancellationToken cancellationToken)
    {
        var normalizedPhone = StorefrontSupport.NormalizePhone(phone);
        var flagged = await dbContext.FlaggedCustomers.FirstOrDefaultAsync(item => item.Phone == normalizedPhone, cancellationToken);
        if (flagged is null)
        {
            return NotFound();
        }

        dbContext.FlaggedCustomers.Remove(flagged);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

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

    [HttpGet("media-configs/{collectionKey}")]
    public async Task<ActionResult<IReadOnlyList<MediaAssetConfigDto>>> GetMediaConfigs(string collectionKey, CancellationToken cancellationToken)
    {
        var items = await dbContext.MediaAssetConfigs.AsNoTracking()
            .Where(item => item.CollectionKey == collectionKey)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.ImageName)
            .Select(item => new MediaAssetConfigDto(item.Id, item.CollectionKey, item.FolderName, item.ImageName, item.DisplayName, item.Description, item.Amount, item.SortOrder))
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPut("media-configs/{collectionKey}")]
    public async Task<ActionResult<IReadOnlyList<MediaAssetConfigDto>>> SaveMediaConfigs(string collectionKey, IReadOnlyList<SaveMediaAssetConfigRequest> request, CancellationToken cancellationToken)
    {
        var existing = await dbContext.MediaAssetConfigs.Where(item => item.CollectionKey == collectionKey).ToListAsync(cancellationToken);
        dbContext.MediaAssetConfigs.RemoveRange(existing);

        var folderName = ResolveCollectionFolder(collectionKey);
        var items = request.Select((item, index) => new MediaAssetConfig
        {
            CollectionKey = collectionKey,
            FolderName = string.IsNullOrWhiteSpace(item.FolderName) ? folderName : item.FolderName,
            ImageName = item.ImageName.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(item.DisplayName) ? item.ImageName.Trim() : item.DisplayName.Trim(),
            Description = item.Description?.Trim() ?? string.Empty,
            Amount = item.Amount,
            SortOrder = item.SortOrder ?? index,
            UpdatedAt = DateTimeOffset.UtcNow
        }).ToArray();

        await dbContext.MediaAssetConfigs.AddRangeAsync(items, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(items.Select(item => new MediaAssetConfigDto(item.Id, item.CollectionKey, item.FolderName, item.ImageName, item.DisplayName, item.Description, item.Amount, item.SortOrder)).ToArray());
    }

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

    [HttpGet("cloudinary/{collectionKey}/images")]
    public async Task<ActionResult<CloudinaryResourcePage>> ListCloudinaryImages(string collectionKey, [FromQuery] int pageSize = 50, [FromQuery] string? nextCursor = null, CancellationToken cancellationToken = default)
    {
        var folder = ResolveCollectionFolder(collectionKey);
        var page = await cloudinaryStorefrontService.ListImagesAsync(folder, Math.Clamp(pageSize, 1, 100), nextCursor, cancellationToken);
        return Ok(page);
    }

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
            if (customer.Id == Guid.Empty)
            {
                customer.Id = Guid.NewGuid();
            }

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
        var source = ResolveImportSource(collectionKey);
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

            dbContext.MediaAssetConfigs.Add(new MediaAssetConfig
            {
                CollectionKey = collectionKey,
                FolderName = source.Folder,
                ImageName = imageName,
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

    private async Task<IActionResult> UpdateReviewStatus(Guid id, string status, CancellationToken cancellationToken)
    {
        var review = await dbContext.CustomerReviews.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (review is null)
        {
            return NotFound();
        }

        review.Status = status;
        review.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(StorefrontSupport.ToReviewDto(review));
    }

    private string ResolveCollectionFolder(string collectionKey)
    {
        return collectionKey.ToLowerInvariant() switch
        {
            "gallery" => _storefrontOptions.Cloudinary.GalleryFolder,
            "trending" => _storefrontOptions.Cloudinary.TrendingFolder,
            "customercollection" => _storefrontOptions.Cloudinary.CustomerCollectionFolder,
            "promoads" => _storefrontOptions.Cloudinary.PromoAdsFolder,
            _ => collectionKey
        };
    }

    private RawJsonSource ResolveImportSource(string collectionKey)
    {
        return collectionKey.ToLowerInvariant() switch
        {
            "gallery" => _storefrontOptions.Imports.GalleryConfig,
            "trending" => _storefrontOptions.Imports.TrendingConfig,
            "customercollection" => _storefrontOptions.Imports.CustomerCollectionConfig,
            _ => new RawJsonSource(collectionKey, "seraphine-config.json")
        };
    }
}

public sealed record AdminCustomerUpsertRequest(string Phone, string Name, int OrderCount, string? Notes, DateTimeOffset? RegisteredAt, DateTimeOffset? LastOrderAt);
public sealed record AddFlaggedCustomerRequest(string Phone, string? Reason);
public sealed record AdminPromoRequest(bool Active, DateTimeOffset? ActiveFrom, DateTimeOffset? ActiveTo, string AdName, string? AdDescription, string RedirectType, string? RedirectValue, string? CtaText, bool ShowOncePerDay, IReadOnlyList<string> Images);
public sealed record SaveMediaAssetConfigRequest(string ImageName, string? DisplayName, string? Description, decimal? Amount, string? FolderName, int? SortOrder);
public sealed record BackfillPlacesRequest(IReadOnlyList<BackfillPlacePatch> Patches);
public sealed record BackfillPlacePatch(Guid VisitorId, string PlaceName);
public sealed record VisitorProfileDto(Guid Id, string VisitorKey, string IpAddress, DateTimeOffset FirstSeen, DateTimeOffset LastSeen, int VisitCount, string? FirstUtmJson, string LocationJson, string DeviceJson, string SessionsJson);
public sealed record AnalyticsSnapshotDto(int UniqueVisitors, int TotalVisits, IReadOnlyList<VisitorProfileDto> Visitors);
public sealed record MediaAssetConfigDto(Guid Id, string CollectionKey, string FolderName, string ImageName, string DisplayName, string Description, decimal? Amount, int SortOrder);
