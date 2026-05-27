using Microsoft.EntityFrameworkCore;
using MonsoonMasala.Application.Abstractions;
using MonsoonMasala.Application.Common;

namespace MonsoonMasala.Application.Menus;

public sealed record GetPublicMenuDishesQuery(Guid MenuId, int Page = 1, int PageSize = 10) : IQuery<PaginatedResult<DishDto>>;

public sealed class GetPublicMenuDishesQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetPublicMenuDishesQuery, PaginatedResult<DishDto>>
{
    public async Task<PaginatedResult<DishDto>> HandleAsync(GetPublicMenuDishesQuery query, CancellationToken cancellationToken = default)
    {
        var menuExists = await dbContext.Menus
            .AnyAsync(menu => menu.Id == query.MenuId && menu.IsActive, cancellationToken);

        if (!menuExists)
        {
            throw new KeyNotFoundException("Menu was not found.");
        }

        var totalCount = await dbContext.Dishes
            .AsNoTracking()
            .CountAsync(dish => dish.MenuId == query.MenuId && dish.IsActive, cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
        var page = Math.Max(1, query.Page);

        var dishes = await dbContext.Dishes
            .AsNoTracking()
            .Where(dish => dish.MenuId == query.MenuId && dish.IsActive)
            .Include(dish => dish.Media)
            .OrderBy(dish => dish.Order)
            .ThenBy(dish => dish.Name)
            .Skip((page - 1) * query.PageSize)
            .Take(query.PageSize)
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
            .ToListAsync(cancellationToken);

        return new PaginatedResult<DishDto>(dishes, page, query.PageSize, totalCount, totalPages);
    }
}
