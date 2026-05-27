namespace SeraphineFlowers.Domain.Entities;

public sealed class AdvertisementMedia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AdvertisementId { get; set; }
    public Advertisement? Advertisement { get; set; }
    public string MediaUrl { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public MediaType MediaType { get; set; } = MediaType.Image;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
