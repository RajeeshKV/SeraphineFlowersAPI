using SeraphineFlowers.Application.Common;
using SeraphineFlowers.Domain.Entities;
using SeraphineFlowers.Infrastructure.Storefront;

namespace SeraphineFlowers.Api.Storefront;

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
public sealed record AdminCustomerUpsertRequest(string Phone, string Name, int OrderCount, string? Notes, DateTimeOffset? RegisteredAt, DateTimeOffset? LastOrderAt);
public sealed record AddFlaggedCustomerRequest(string Phone, string? Reason);
public sealed record AdminPromoRequest(bool Active, DateTimeOffset? ActiveFrom, DateTimeOffset? ActiveTo, string AdName, string? AdDescription, string RedirectType, string? RedirectValue, string? CtaText, bool ShowOncePerDay, IReadOnlyList<string> Images);
public sealed record SaveMediaAssetConfigRequest(string ImageName, string? DisplayName, string? Description, decimal? Amount, string? FolderName, string? ImageUrl, int? SortOrder);
public sealed record BackfillPlacesRequest(IReadOnlyList<BackfillPlacePatch> Patches);
public sealed record BackfillPlacePatch(Guid VisitorId, string PlaceName);
public sealed record VisitorProfileDto(Guid Id, string VisitorKey, string IpAddress, DateTimeOffset FirstSeen, DateTimeOffset LastSeen, int VisitCount, string? FirstUtmJson, string LocationJson, string DeviceJson, string SessionsJson);
public sealed record AnalyticsSnapshotDto(int UniqueVisitors, int TotalVisits, IReadOnlyList<VisitorProfileDto> Visitors);
public sealed record MediaAssetConfigDto(Guid Id, string CollectionKey, string FolderName, string ImageName, string ImageUrl, string DisplayName, string Description, decimal? Amount, int SortOrder);
public sealed record BackfillMediaImageUrlsResult(int Updated);

public static class Pagination
{
    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        return (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));
    }

    public static PaginatedResult<T> Create<T>(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PaginatedResult<T>(items, page, pageSize, totalCount, totalPages);
    }
}

public static class StorefrontMediaSupport
{
    public static MediaAssetConfigDto ToDto(MediaAssetConfig item, CloudinaryStorefrontService cloudinaryStorefrontService)
    {
        var imageUrl = string.IsNullOrWhiteSpace(item.ImageUrl)
            ? cloudinaryStorefrontService.BuildImageUrl(item.FolderName, item.ImageName)
            : item.ImageUrl;

        return new MediaAssetConfigDto(
            item.Id,
            item.CollectionKey,
            item.FolderName,
            item.ImageName,
            imageUrl,
            item.DisplayName,
            item.Description,
            item.Amount,
            item.SortOrder);
    }

    public static string ResolveCollectionFolder(string collectionKey, StorefrontOptions storefrontOptions)
    {
        return collectionKey.ToLowerInvariant() switch
        {
            "gallery" => storefrontOptions.Cloudinary.GalleryFolder,
            "trending" => storefrontOptions.Cloudinary.TrendingFolder,
            "customercollection" => storefrontOptions.Cloudinary.CustomerCollectionFolder,
            "promoads" => storefrontOptions.Cloudinary.PromoAdsFolder,
            _ => collectionKey
        };
    }

    public static RawJsonSource ResolveImportSource(string collectionKey, StorefrontOptions storefrontOptions)
    {
        return collectionKey.ToLowerInvariant() switch
        {
            "gallery" => storefrontOptions.Imports.GalleryConfig,
            "trending" => storefrontOptions.Imports.TrendingConfig,
            "customercollection" => storefrontOptions.Imports.CustomerCollectionConfig,
            _ => new RawJsonSource(collectionKey, "seraphine-config.json")
        };
    }
}
