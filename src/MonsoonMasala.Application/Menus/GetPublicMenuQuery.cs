using Microsoft.EntityFrameworkCore;
using MonsoonMasala.Application.Abstractions;

namespace MonsoonMasala.Application.Menus;

public sealed record GetPublicMenuQuery : IQuery<IReadOnlyList<MenuDto>>;

public sealed class GetPublicMenuQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetPublicMenuQuery, IReadOnlyList<MenuDto>>
{
    public async Task<IReadOnlyList<MenuDto>> HandleAsync(GetPublicMenuQuery query, CancellationToken cancellationToken = default)
    {
        return await dbContext.Menus
            .AsNoTracking()
            .Where(menu => menu.IsActive)
            .OrderBy(menu => menu.Order)
            .ThenBy(menu => menu.Name)
            .Select(menu => new MenuDto(
                menu.Id,
                menu.Name,
                menu.Order,
                menu.Dishes
                    .Where(dish => dish.IsActive)
                    .OrderBy(dish => dish.Order)
                    .ThenBy(dish => dish.Name)
                    .Select(dish => new DishDto(
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
                            .ToList()))
                    .ToList()))
            .ToListAsync(cancellationToken);
    }
}
