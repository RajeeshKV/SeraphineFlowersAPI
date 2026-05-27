using SeraphineFlowers.Domain.Entities;

namespace SeraphineFlowers.Application.Advertisements;

public sealed record AdvertisementDto(
    Guid Id,
    string? Text,
    string? Url,
    bool IsActive,
    DateTimeOffset ActiveFrom,
    int Order,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    AdvertisementMediaDto Media);

public sealed record AdvertisementMediaDto(
    Guid Id,
    string MediaUrl,
    string PublicId,
    MediaType MediaType);

public static class AdvertisementMapping
{
    public static AdvertisementDto ToDto(this Advertisement advertisement)
    {
        return new AdvertisementDto(
            advertisement.Id,
            advertisement.Text,
            advertisement.Url,
            advertisement.IsActive,
            advertisement.ActiveFrom,
            advertisement.Order,
            advertisement.CreatedAt,
            advertisement.UpdatedAt,
            advertisement.Media.ToDto());
    }

    public static AdvertisementMediaDto ToDto(this AdvertisementMedia media)
    {
        return new AdvertisementMediaDto(
            media.Id,
            media.MediaUrl,
            media.PublicId,
            media.MediaType);
    }
}
