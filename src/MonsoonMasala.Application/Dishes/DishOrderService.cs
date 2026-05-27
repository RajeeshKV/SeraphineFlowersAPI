using Microsoft.EntityFrameworkCore;
using MonsoonMasala.Application.Abstractions;
using MonsoonMasala.Domain.Entities;

namespace MonsoonMasala.Application.Dishes;

internal static class DishOrderService
{
    public static async Task<int> InsertAtAsync(IAppDbContext dbContext, Guid menuId, int requestedOrder, CancellationToken cancellationToken)
    {
        await NormalizeMenuAsync(dbContext, menuId, cancellationToken);

        var dishes = await dbContext.Dishes
            .Where(dish => dish.MenuId == menuId)
            .OrderBy(dish => dish.Order)
            .ThenBy(dish => dish.CreatedAt)
            .ToListAsync(cancellationToken);

        var targetOrder = requestedOrder <= 0 ? dishes.Count + 1 : Math.Min(requestedOrder, dishes.Count + 1);

        foreach (var dish in dishes.Where(dish => dish.Order >= targetOrder))
        {
            dish.Order += 1;
        }

        return targetOrder;
    }

    public static async Task MoveAsync(IAppDbContext dbContext, Dish dish, Guid targetMenuId, int requestedOrder, CancellationToken cancellationToken)
    {
        if (requestedOrder <= 0)
        {
            throw new ArgumentException("Order must be greater than 0.");
        }

        var sourceMenuId = dish.MenuId;
        await NormalizeMenuAsync(dbContext, sourceMenuId, cancellationToken);

        if (sourceMenuId == targetMenuId)
        {
            await MoveWithinMenuAsync(dbContext, dish, requestedOrder, cancellationToken);
            return;
        }

        var sourceOrder = dish.Order;
        var sourceSiblings = await dbContext.Dishes
            .Where(sibling => sibling.MenuId == sourceMenuId && sibling.Id != dish.Id && sibling.Order > sourceOrder)
            .ToListAsync(cancellationToken);

        foreach (var sibling in sourceSiblings)
        {
            sibling.Order -= 1;
        }

        await NormalizeMenuAsync(dbContext, targetMenuId, cancellationToken);

        var targetSiblings = await dbContext.Dishes
            .Where(sibling => sibling.MenuId == targetMenuId)
            .OrderBy(sibling => sibling.Order)
            .ThenBy(sibling => sibling.CreatedAt)
            .ToListAsync(cancellationToken);

        var targetOrder = Math.Min(requestedOrder, targetSiblings.Count + 1);
        foreach (var sibling in targetSiblings.Where(sibling => sibling.Order >= targetOrder))
        {
            sibling.Order += 1;
        }

        dish.MenuId = targetMenuId;
        dish.Order = targetOrder;
    }

    public static async Task<int> NormalizeMenuAsync(IAppDbContext dbContext, Guid menuId, CancellationToken cancellationToken)
    {
        var dishes = await dbContext.Dishes
            .Where(dish => dish.MenuId == menuId)
            .OrderBy(dish => dish.Order <= 0 ? int.MaxValue : dish.Order)
            .ThenBy(dish => dish.Order)
            .ThenBy(dish => dish.CreatedAt)
            .ThenBy(dish => dish.Name)
            .ToListAsync(cancellationToken);

        var changed = 0;
        for (var index = 0; index < dishes.Count; index++)
        {
            var normalizedOrder = index + 1;
            if (dishes[index].Order != normalizedOrder)
            {
                dishes[index].Order = normalizedOrder;
                dishes[index].UpdatedAt = DateTimeOffset.UtcNow;
                changed++;
            }
        }

        return changed;
    }

    private static async Task MoveWithinMenuAsync(IAppDbContext dbContext, Dish dish, int requestedOrder, CancellationToken cancellationToken)
    {
        var siblings = await dbContext.Dishes
            .Where(sibling => sibling.MenuId == dish.MenuId)
            .OrderBy(sibling => sibling.Order)
            .ThenBy(sibling => sibling.CreatedAt)
            .ToListAsync(cancellationToken);

        var currentOrder = dish.Order;
        var targetOrder = Math.Min(requestedOrder, siblings.Count);

        if (targetOrder == currentOrder)
        {
            return;
        }

        if (targetOrder < currentOrder)
        {
            foreach (var sibling in siblings.Where(sibling => sibling.Id != dish.Id && sibling.Order >= targetOrder && sibling.Order < currentOrder))
            {
                sibling.Order += 1;
            }
        }
        else
        {
            foreach (var sibling in siblings.Where(sibling => sibling.Id != dish.Id && sibling.Order > currentOrder && sibling.Order <= targetOrder))
            {
                sibling.Order -= 1;
            }
        }

        dish.Order = targetOrder;
    }
}
