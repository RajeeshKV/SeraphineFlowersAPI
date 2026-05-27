namespace MonsoonMasala.Domain.Entities;

public sealed class Dish
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MenuId { get; set; }
    public Menu? Menu { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public FoodType FoodType { get; set; } = FoodType.Veg;
    public List<string> Ingredients { get; set; } = [];
    public string? Metadata { get; set; }
    public bool IsActive { get; set; } = true;
    public int Order { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public List<DishMedia> Media { get; set; } = [];
}
