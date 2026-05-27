using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Application.Abstractions;

namespace SeraphineFlowers.Application.Dishes;

public sealed record FixDishOrdersCommand(Guid? MenuId) : ICommand<FixDishOrdersResult>;

public sealed record FixDishOrdersResult(int MenusChecked, int DishesReordered);

public sealed class FixDishOrdersCommandHandler(IAppDbContext dbContext) : ICommandHandler<FixDishOrdersCommand, FixDishOrdersResult>
{
    public async Task<FixDishOrdersResult> HandleAsync(FixDishOrdersCommand command, CancellationToken cancellationToken = default)
    {
        var menuIdsQuery = dbContext.Dishes.Select(dish => dish.MenuId).Distinct();
        if (command.MenuId.HasValue)
        {
            menuIdsQuery = menuIdsQuery.Where(menuId => menuId == command.MenuId.Value);
        }

        var menuIds = await menuIdsQuery.ToListAsync(cancellationToken);
        var changed = 0;

        foreach (var menuId in menuIds)
        {
            changed += await DishOrderService.NormalizeMenuAsync(dbContext, menuId, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new FixDishOrdersResult(menuIds.Count, changed);
    }
}
