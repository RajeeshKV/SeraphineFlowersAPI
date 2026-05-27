using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Application.Abstractions;
using SeraphineFlowers.Application.Menus;
using SeraphineFlowers.Domain.Entities;

namespace SeraphineFlowers.Application.Dishes;

public sealed record CreateDishCommand(
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
    IFormFileCollection Files) : ICommand<DishDto>;

public sealed class CreateDishCommandHandler(IAppDbContext dbContext, IMediaStorageProxy mediaStorageProxy) : ICommandHandler<CreateDishCommand, DishDto>
{
    public async Task<DishDto> HandleAsync(CreateDishCommand command, CancellationToken cancellationToken = default)
    {
        var menuExists = await dbContext.Menus.AnyAsync(menu => menu.Id == command.MenuId, cancellationToken);
        if (!menuExists)
        {
            throw new KeyNotFoundException("Menu was not found.");
        }

        var slug = string.IsNullOrWhiteSpace(command.Slug) ? SlugGenerator.From(command.Name) : SlugGenerator.From(command.Slug);
        var slugExists = await dbContext.Dishes.AnyAsync(dish => dish.MenuId == command.MenuId && dish.Slug == slug, cancellationToken);
        if (slugExists)
        {
            throw new InvalidOperationException($"Dish slug '{slug}' already exists for this menu.");
        }

        var order = await DishOrderService.InsertAtAsync(dbContext, command.MenuId, command.Order, cancellationToken);

        var uploads = await mediaStorageProxy.UploadAsync(command.Files, cancellationToken);
        var dish = new Dish
        {
            MenuId = command.MenuId,
            Name = command.Name.Trim(),
            Slug = slug,
            Description = command.Description,
            Price = command.Price,
            FoodType = command.FoodType,
            Ingredients = command.Ingredients.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToList(),
            Metadata = command.Metadata,
            IsActive = command.IsActive,
            Order = order,
            Media = uploads.Select(upload => new DishMedia
            {
                Url = upload.Url,
                PublicId = upload.PublicId,
                MediaType = upload.MediaType
            }).ToList()
        };

        dbContext.Dishes.Add(dish);
        await dbContext.SaveChangesAsync(cancellationToken);

        return dish.ToDto();
    }
}
