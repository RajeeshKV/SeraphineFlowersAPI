namespace SeraphineFlowers.Domain.Entities;

public sealed class VisitorProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string VisitorKey { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTimeOffset FirstSeen { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeen { get; set; } = DateTimeOffset.UtcNow;
    public int VisitCount { get; set; } = 1;
    public string? FirstUtmJson { get; set; }
    public string LocationJson { get; set; } = "{}";
    public string DeviceJson { get; set; } = "{}";
    public string SessionsJson { get; set; } = "[]";
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
