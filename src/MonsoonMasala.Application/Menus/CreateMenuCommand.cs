using Microsoft.EntityFrameworkCore;
using MonsoonMasala.Application.Abstractions;
using MonsoonMasala.Domain.Entities;

namespace MonsoonMasala.Application.Menus;

public sealed record CreateMenuCommand(string Name, string? Slug, int Order, bool IsActive) : ICommand<MenuDto>;

public sealed class CreateMenuCommandHandler(IAppDbContext dbContext) : ICommandHandler<CreateMenuCommand, MenuDto>
{
    public async Task<MenuDto> HandleAsync(CreateMenuCommand command, CancellationToken cancellationToken = default)
    {
        var slug = string.IsNullOrWhiteSpace(command.Slug) ? SlugGenerator.From(command.Name) : SlugGenerator.From(command.Slug);
        var exists = await dbContext.Menus.AnyAsync(menu => menu.Slug == slug, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Menu slug '{slug}' already exists.");
        }

        var menu = new Menu
        {
            Name = command.Name.Trim(),
            Slug = slug,
            Order = command.Order,
            IsActive = command.IsActive
        };

        dbContext.Menus.Add(menu);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new MenuDto(menu.Id, menu.Name, menu.Order, []);
    }
}
