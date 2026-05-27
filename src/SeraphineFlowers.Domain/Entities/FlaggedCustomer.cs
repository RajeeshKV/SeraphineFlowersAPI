namespace SeraphineFlowers.Domain.Entities;

public sealed class FlaggedCustomer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Phone { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset FlaggedAt { get; set; } = DateTimeOffset.UtcNow;
}
