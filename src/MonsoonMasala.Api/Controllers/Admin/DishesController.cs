using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonsoonMasala.Application.Abstractions;
using MonsoonMasala.Application.Common;
using MonsoonMasala.Application.Dishes;
using MonsoonMasala.Application.Menus;
using MonsoonMasala.Domain.Entities;

namespace MonsoonMasala.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/dishes")]
public sealed class DishesController(
    IQueryHandler<GetDishesByMenuQuery, PaginatedResult<DishDto>> getDishesHandler,
    ICommandHandler<CreateDishCommand, DishDto> createHandler,
    ICommandHandler<UpdateDishCommand, DishDto> updateHandler,
    ICommandHandler<DeleteDishCommand, bool> deleteHandler,
    ICommandHandler<FixDishOrdersCommand, FixDishOrdersResult> fixOrdersHandler) : ControllerBase
{
    [HttpGet("menu/{menuId:guid}")]
    public async Task<ActionResult<PaginatedResult<DishDto>>> GetByMenu(
        Guid menuId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var dishes = await getDishesHandler.HandleAsync(
            new GetDishesByMenuQuery(menuId, page, pageSize),
            cancellationToken);
        return Ok(dishes);
    }

    [HttpPost]
    [RequestSizeLimit(200_000_000)]
    public async Task<ActionResult<DishDto>> Create([FromForm] CreateDishRequest request, CancellationToken cancellationToken)
    {
        var files = Request.Form.Files;
        var dish = await createHandler.HandleAsync(new CreateDishCommand(
            request.MenuId,
            request.Name,
            request.Slug,
            request.Description,
            request.Price,
            request.FoodType,
            request.Ingredients ?? [],
            request.Metadata,
            request.IsActive,
            request.Order,
            files), cancellationToken);

        return CreatedAtAction(nameof(Create), new { id = dish.Id }, dish);
    }

    [HttpPut("{id:guid}")]
    [RequestSizeLimit(200_000_000)]
    public async Task<ActionResult<DishDto>> Update(Guid id, [FromForm] UpdateDishRequest request, CancellationToken cancellationToken)
    {
        var files = Request.Form.Files;
        var dish = await updateHandler.HandleAsync(new UpdateDishCommand(
            id,
            request.MenuId,
            request.Name,
            request.Slug,
            request.Description,
            request.Price,
            request.FoodType,
            request.Ingredients ?? [],
            request.Metadata,
            request.IsActive,
            request.Order,
            request.ReplaceMedia,
            files), cancellationToken);

        return Ok(dish);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await deleteHandler.HandleAsync(new DeleteDishCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("fix-order")]
    public async Task<ActionResult<FixDishOrdersResult>> FixOrder([FromQuery] Guid? menuId, CancellationToken cancellationToken)
    {
        var result = await fixOrdersHandler.HandleAsync(new FixDishOrdersCommand(menuId), cancellationToken);
        return Ok(result);
    }
}

public sealed class CreateDishRequest
{
    public Guid MenuId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public FoodType FoodType { get; set; } = FoodType.Veg;
    public List<string>? Ingredients { get; set; }
    public string? Metadata { get; set; }
    public bool IsActive { get; set; } = true;
    public int Order { get; set; }
}

public sealed class UpdateDishRequest
{
    public Guid MenuId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public FoodType FoodType { get; set; } = FoodType.Veg;
    public List<string>? Ingredients { get; set; }
    public string? Metadata { get; set; }
    public bool IsActive { get; set; } = true;
    public int Order { get; set; }
    public bool ReplaceMedia { get; set; }
}
