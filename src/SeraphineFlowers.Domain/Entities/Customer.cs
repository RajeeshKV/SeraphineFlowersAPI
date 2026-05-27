namespace SeraphineFlowers.Domain.Entities;

public sealed class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Phone { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.UtcNow;
    public int OrderCount { get; set; }
    public string OfferCode { get; set; } = "welcome";
    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset? LastOrderAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
