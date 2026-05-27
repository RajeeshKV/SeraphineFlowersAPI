using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Application.Abstractions;
using SeraphineFlowers.Application.Menus;
using SeraphineFlowers.Domain.Entities;

namespace SeraphineFlowers.Application.Dishes;

public sealed record UpdateDishCommand(
    Guid Id,
    Guid MenuId,
    string Name,
    string? Slug,
    string? Description,
    decimal? Price,
    FoodType FoodType,
    IReadOnlyList<string> Ingredients,
    string? Metadata,
    bool IsActive,
    int Order,
    bool ReplaceMedia,
    IFormFileCollection Files) : ICommand<DishDto>;

public sealed class UpdateDishCommandHandler(IAppDbContext dbContext, IMediaStorageProxy mediaStorageProxy) : ICommandHandler<UpdateDishCommand, DishDto>
{
    public async Task<DishDto> HandleAsync(UpdateDishCommand command, CancellationToken cancellationToken = default)
    {
        var menuExists = await dbContext.Menus.AnyAsync(menu => menu.Id == command.MenuId, cancellationToken);
        if (!menuExists)
        {
            throw new KeyNotFoundException("Menu was not found.");
        }

        var dish = await dbContext.Dishes
            .Include(dish => dish.Media)
            .FirstOrDefaultAsync(dish => dish.Id == command.Id, cancellationToken);

        if (dish is null)
        {
            throw new KeyNotFoundException("Dish was not found.");
        }

        var slug = string.IsNullOrWhiteSpace(command.Slug) ? SlugGenerator.From(command.Name) : SlugGenerator.From(command.Slug);
        var slugExists = await dbContext.Dishes.AnyAsync(existing =>
            existing.Id != command.Id &&
            existing.MenuId == command.MenuId &&
            existing.Slug == slug,
            cancellationToken);

        if (slugExists)
        {
            throw new InvalidOperationException($"Dish slug '{slug}' already exists for this menu.");
        }

        await DishOrderService.MoveAsync(dbContext, dish, command.MenuId, command.Order, cancellationToken);

        dish.Name = command.Name.Trim();
        dish.Slug = slug;
        dish.Description = command.Description;
        dish.Price = command.Price;
        dish.FoodType = command.FoodType;
        dish.Ingredients = command.Ingredients.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToList();
        dish.Metadata = command.Metadata;
        dish.IsActive = command.IsActive;
        dish.UpdatedAt = DateTimeOffset.UtcNow;

        if (command.ReplaceMedia)
        {
            dish.Media.Clear();
        }

        var uploads = await mediaStorageProxy.UploadAsync(command.Files, cancellationToken);
        foreach (var upload in uploads)
        {
            dbContext.DishMedia.Add(new DishMedia
            {
                DishId = dish.Id,
                Url = upload.Url,
                PublicId = upload.PublicId,
                MediaType = upload.MediaType
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return dish.ToDto();
    }
}
