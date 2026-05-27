namespace SeraphineFlowers.Domain.Entities;

public sealed class PromoCampaign
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool Active { get; set; }
    public DateTimeOffset? ActiveFrom { get; set; }
    public DateTimeOffset? ActiveTo { get; set; }
    public string AdName { get; set; } = string.Empty;
    public string AdDescription { get; set; } = string.Empty;
    public string RedirectType { get; set; } = "none";
    public string? RedirectValue { get; set; }
    public string CtaText { get; set; } = "Order Now";
    public bool ShowOncePerDay { get; set; } = true;
    public string ImagesJson { get; set; } = "[]";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
