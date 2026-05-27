namespace MonsoonMasala.Domain.Entities;

public sealed class DishMedia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DishId { get; set; }
    public Dish? Dish { get; set; }
    public string Url { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public MediaType MediaType { get; set; } = MediaType.Image;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
