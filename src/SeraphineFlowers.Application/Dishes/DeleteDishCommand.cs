using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Application.Abstractions;

namespace SeraphineFlowers.Application.Dishes;

public sealed record DeleteDishCommand(Guid Id) : ICommand<bool>;

public sealed class DeleteDishCommandHandler(IAppDbContext dbContext) : ICommandHandler<DeleteDishCommand, bool>
{
    public async Task<bool> HandleAsync(DeleteDishCommand command, CancellationToken cancellationToken = default)
    {
        var dish = await dbContext.Dishes.FirstOrDefaultAsync(dish => dish.Id == command.Id, cancellationToken);
        if (dish is null)
        {
            throw new KeyNotFoundException("Dish was not found.");
        }

        var menuId = dish.MenuId;

        dbContext.Dishes.Remove(dish);
        await dbContext.SaveChangesAsync(cancellationToken);
        await DishOrderService.NormalizeMenuAsync(dbContext, menuId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
