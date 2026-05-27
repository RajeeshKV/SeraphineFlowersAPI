namespace MonsoonMasala.Domain.Entities;

public sealed class Advertisement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Text { get; set; }
    public string? Url { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset ActiveFrom { get; set; } = DateTimeOffset.UtcNow;
    public int Order { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public AdvertisementMedia Media { get; set; } = null!;
}
