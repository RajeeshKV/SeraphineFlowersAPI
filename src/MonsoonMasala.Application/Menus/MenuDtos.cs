using MonsoonMasala.Domain.Entities;

namespace MonsoonMasala.Application.Menus;

public sealed record DishMediaDto(Guid Id, string Url, string PublicId, MediaType MediaType);
public sealed record DishDto(Guid Id, Guid MenuId, string Name, string Slug, string? Description, decimal? Price, FoodType FoodType, IReadOnlyList<string> Ingredients, string? Metadata, bool IsActive, int Order, IReadOnlyList<DishMediaDto> Media);
public sealed record MenuDto(Guid Id, string Name, int Order, IReadOnlyList<DishDto> Dishes);
public sealed record AdminMenu(Guid Id, string Name, int Order);