namespace SeraphineFlowers.Domain.Entities;

public sealed class MediaAssetConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CollectionKey { get; set; } = string.Empty;
    public string FolderName { get; set; } = string.Empty;
    public string ImageName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
