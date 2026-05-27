using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SeraphineFlowers.Application.Abstractions;
using SeraphineFlowers.Api.Storefront;
using SeraphineFlowers.Infrastructure.Persistence;
using SeraphineFlowers.Infrastructure.Storefront;

namespace SeraphineFlowers.Api.Controllers.Public;

[ApiController]
[Route("api/public")]
public sealed class StorefrontController(
    AppDbContext dbContext,
    IOptions<StorefrontOptions> storefrontOptions,
    IFirebaseTokenVerifier firebaseTokenVerifier,
    CloudinaryStorefrontService cloudinaryStorefrontService) : ControllerBase
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

    [HttpPost("customers/register")]
    public async Task<ActionResult<CustomerRegistrationResponse>> RegisterCustomer(RegisterCustomerRequest request, CancellationToken cancellationToken)
    {
        var normalizedPhone = StorefrontSupport.NormalizePhone(request.Phone);
        if (!StorefrontSupport.IsValidIndianPhone(normalizedPhone) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "A valid 10-digit Indian mobile number and name are required." });
        }

        FirebaseCustomerIdentity? identity = null;
        if (_storefrontOptions.RequireCustomerOtp)
        {
            if (string.IsNullOrWhiteSpace(request.FirebaseIdToken))
            {
                return BadRequest(new { error = "Firebase ID token is required because customer OTP verification is enabled." });
            }

            identity = await firebaseTokenVerifier.VerifyAsync(request.FirebaseIdToken, cancellationToken);
            if (StorefrontSupport.NormalizePhone(identity.PhoneNumber) != normalizedPhone)
            {
                return Unauthorized(new { error = "Firebase verified phone number does not match the requested phone number." });
            }
        }

        var existing = await dbContext.Customers.FirstOrDefaultAsync(item => item.Phone == normalizedPhone, cancellationToken);
        if (existing is not null)
        {
            if (identity is not null)
            {
                existing.FirebaseUid = identity.Uid;
                existing.IsOtpVerified = true;
                existing.LastLoginAt = DateTimeOffset.UtcNow;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return Ok(new CustomerRegistrationResponse(true, false, StorefrontSupport.ToCustomerDto(existing, _storefrontOptions.Offer, true)));
        }

        var customer = new SeraphineFlowers.Domain.Entities.Customer
        {
            FirebaseUid = identity?.Uid,
            Phone = normalizedPhone,
            Name = request.Name.Trim(),
            IsOtpVerified = identity is not null,
            LastLoginAt = identity is not null ? DateTimeOffset.UtcNow : null,
            OfferCode = "welcome"
        };

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new CustomerRegistrationResponse(true, true, StorefrontSupport.ToCustomerDto(customer, _storefrontOptions.Offer, true)));
    }

    [HttpPost("customers/lookup")]
    public async Task<ActionResult<CustomerLookupResponse>> LookupCustomer(CustomerLookupRequest request, CancellationToken cancellationToken)
    {
        var normalizedPhone = StorefrontSupport.NormalizePhone(request.Phone);
        if (_storefrontOptions.RequireCustomerOtp)
        {
            if (string.IsNullOrWhiteSpace(request.FirebaseIdToken))
            {
                return BadRequest(new { error = "Firebase ID token is required because customer OTP verification is enabled." });
            }

            var identity = await firebaseTokenVerifier.VerifyAsync(request.FirebaseIdToken, cancellationToken);
            if (StorefrontSupport.NormalizePhone(identity.PhoneNumber) != normalizedPhone)
            {
                return Unauthorized(new { error = "Firebase verified phone number does not match the requested phone number." });
            }
        }

        var customer = await dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(item => item.Phone == normalizedPhone, cancellationToken);
        return Ok(new CustomerLookupResponse(customer is not null, customer is null ? null : StorefrontSupport.ToCustomerDto(customer, _storefrontOptions.Offer, true)));
    }

    [HttpGet("customers/{phone}")]
    public async Task<ActionResult<CustomerDto>> GetCustomer(string phone, CancellationToken cancellationToken)
    {
        var normalizedPhone = StorefrontSupport.NormalizePhone(phone);
        var customer = await dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(item => item.Phone == normalizedPhone, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        return Ok(StorefrontSupport.ToCustomerDto(customer, _storefrontOptions.Offer, true));
    }

    [HttpGet("reviews")]
    public async Task<ActionResult<IReadOnlyList<ReviewDto>>> GetReviews(CancellationToken cancellationToken)
    {
        var reviews = await dbContext.CustomerReviews.AsNoTracking()
            .Where(item => item.Status == "approved")
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => StorefrontSupport.ToReviewDto(item))
            .ToListAsync(cancellationToken);
        return Ok(reviews);
    }

    [HttpPost("reviews")]
    public async Task<ActionResult<ReviewDto>> SubmitReview(SubmitReviewRequest request, CancellationToken cancellationToken)
    {
        var normalizedPhone = StorefrontSupport.NormalizePhone(request.Phone);
        if (!StorefrontSupport.IsValidIndianPhone(normalizedPhone) ||
            string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Text) ||
            request.Rating < 1 ||
            request.Rating > 5)
        {
            return BadRequest(new { error = "Phone, name, rating, and review text are required." });
        }

        var existing = await dbContext.CustomerReviews.FirstOrDefaultAsync(item => item.Phone == normalizedPhone, cancellationToken);
        if (existing is not null)
        {
            return Conflict(new { error = "You have already submitted a review." });
        }

        var review = new SeraphineFlowers.Domain.Entities.CustomerReview
        {
            Phone = normalizedPhone,
            Name = request.Name.Trim(),
            Rating = request.Rating,
            Text = request.Text.Trim(),
            Status = "approved"
        };

        dbContext.CustomerReviews.Add(review);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(StorefrontSupport.ToReviewDto(review));
    }

    [HttpPost("flagged/check")]
    public async Task<ActionResult<FlagCheckResponse>> CheckFlagged(FlagCheckRequest request, CancellationToken cancellationToken)
    {
        var normalizedPhone = StorefrontSupport.NormalizePhone(request.Phone);
        var flagged = await dbContext.FlaggedCustomers.AsNoTracking().AnyAsync(item => item.Phone == normalizedPhone, cancellationToken);
        return Ok(new FlagCheckResponse(flagged));
    }

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

    [HttpGet("media/{collectionKey}")]
    public async Task<ActionResult<IReadOnlyList<MediaAssetConfigDto>>> GetMediaConfig(string collectionKey, CancellationToken cancellationToken)
    {
        var items = await dbContext.MediaAssetConfigs.AsNoTracking()
            .Where(item => item.CollectionKey == collectionKey)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.ImageName)
            .Select(item => new MediaAssetConfigDto(item.Id, item.CollectionKey, item.FolderName, item.ImageName, item.DisplayName, item.Description, item.Amount, item.SortOrder))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

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

    [HttpGet("cloudinary/{collectionKey}/images")]
    public async Task<ActionResult<CloudinaryResourcePage>> GetCloudinaryImages(string collectionKey, [FromQuery] int pageSize = 20, [FromQuery] string? nextCursor = null, CancellationToken cancellationToken = default)
    {
        var folder = ResolveCollectionFolder(collectionKey);
        var page = await cloudinaryStorefrontService.ListImagesAsync(folder, Math.Clamp(pageSize, 1, 100), nextCursor, cancellationToken);
        return Ok(page);
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

    private string GetClientIp()
    {
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
        {
            return forwarded.ToString().Split(',')[0].Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

public sealed record RegisterCustomerRequest(string Phone, string Name, string? FirebaseIdToken);
public sealed record CustomerRegistrationResponse(bool Success, bool IsNew, CustomerDto Customer);
public sealed record CustomerLookupRequest(string Phone, string? FirebaseIdToken);
public sealed record CustomerLookupResponse(bool Found, CustomerDto? Customer);
public sealed record SubmitReviewRequest(string Phone, string Name, int Rating, string Text);
public sealed record FlagCheckRequest(string Phone);
public sealed record FlagCheckResponse(bool Flagged);
public sealed record TrackVisitRequest(string? VisitorKey, bool LocationOnly, string? Referrer, string? Page, object? Utm, object? Location, object? Device);
public sealed record StorefrontConfigDto(bool RequireCustomerOtp, FirebaseConfigDto Firebase, int WelcomeDiscount, int LoyaltyDiscount, int LoyaltyEvery);
public sealed record PublicBootstrapDto(CustomerDto? Customer, PromoDto? Promo, IReadOnlyList<ReviewDto> Reviews, StorefrontConfigDto Config);
public sealed record MediaAssetConfigDto(Guid Id, string CollectionKey, string FolderName, string ImageName, string DisplayName, string Description, decimal? Amount, int SortOrder);
