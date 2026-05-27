using Microsoft.EntityFrameworkCore;
using MonsoonMasala.Application.Abstractions;

namespace MonsoonMasala.Application.Menus;

public sealed record UpdateMenuCommand(Guid Id, string Name, string? Slug, int Order, bool IsActive) : ICommand<MenuDto>;

public sealed class UpdateMenuCommandHandler(IAppDbContext dbContext) : ICommandHandler<UpdateMenuCommand, MenuDto>
{
    public async Task<MenuDto> HandleAsync(UpdateMenuCommand command, CancellationToken cancellationToken = default)
    {
        var menu = await dbContext.Menus
            .Include(menu => menu.Dishes)
            .ThenInclude(dish => dish.Media)
            .FirstOrDefaultAsync(menu => menu.Id == command.Id, cancellationToken);

        if (menu is null)
        {
            throw new KeyNotFoundException("Menu was not found.");
        }

        var slug = string.IsNullOrWhiteSpace(command.Slug) ? SlugGenerator.From(command.Name) : SlugGenerator.From(command.Slug);
        var slugExists = await dbContext.Menus.AnyAsync(existing => existing.Id != command.Id && existing.Slug == slug, cancellationToken);
        if (slugExists)
        {
            throw new InvalidOperationException($"Menu slug '{slug}' already exists.");
        }

        menu.Name = command.Name.Trim();
        menu.Slug = slug;
        menu.Order = command.Order;
        menu.IsActive = command.IsActive;
        menu.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new MenuDto(
            menu.Id,
            menu.Name,
            menu.Order,
            menu.Dishes
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
                    dish.Media.Select(media => new DishMediaDto(media.Id, media.Url, media.PublicId, media.MediaType)).ToList()))
                .ToList());
    }
}
