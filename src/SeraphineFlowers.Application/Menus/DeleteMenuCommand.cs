using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Application.Abstractions;

namespace SeraphineFlowers.Application.Menus;

public sealed record DeleteMenuCommand(Guid Id) : ICommand<bool>;

public sealed class DeleteMenuCommandHandler(IAppDbContext dbContext) : ICommandHandler<DeleteMenuCommand, bool>
{
    public async Task<bool> HandleAsync(DeleteMenuCommand command, CancellationToken cancellationToken = default)
    {
        var menu = await dbContext.Menus.FirstOrDefaultAsync(menu => menu.Id == command.Id, cancellationToken);
        if (menu is null)
        {
            throw new KeyNotFoundException("Menu was not found.");
        }

        dbContext.Menus.Remove(menu);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
