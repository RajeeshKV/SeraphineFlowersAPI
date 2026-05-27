using SeraphineFlowers.Application.Menus;
using SeraphineFlowers.Domain.Entities;

namespace SeraphineFlowers.Application.Dishes;

internal static class DishMapping
{
    public static DishDto ToDto(this Dish dish)
    {
        return new DishDto(
            dish.Id,
            dish.MenuId,
            dish.Name,
            dish.Slug,
            dish.Description,
            dish.Price,
            dish.FoodType,
            dish.Ingredients,
            dish.Metadata,
            dish.IsActive,
            dish.Order,
            dish.Media
                .OrderBy(media => media.CreatedAt)
                .Select(media => new DishMediaDto(media.Id, media.Url, media.PublicId, media.MediaType))
                .ToList());
    }
}
