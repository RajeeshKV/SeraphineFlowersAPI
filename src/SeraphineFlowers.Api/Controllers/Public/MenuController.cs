using Microsoft.AspNetCore.Mvc;
using SeraphineFlowers.Application.Abstractions;
using SeraphineFlowers.Application.Common;
using SeraphineFlowers.Application.Menus;

namespace SeraphineFlowers.Api.Controllers.Public;

[ApiController]
[Route("api/public/[controller]")]
public sealed class MenuController(
    IQueryHandler<GetPublicMenuQuery, IReadOnlyList<MenuDto>> getMenuHandler,
    IQueryHandler<GetPublicMenusQuery, IReadOnlyList<MenuSummaryDto>> getMenusHandler,
    IQueryHandler<GetPublicMenuDishesQuery, PaginatedResult<DishDto>> getMenuDishesHandler) : ControllerBase
{
    [HttpGet("all")]
    public async Task<ActionResult<IReadOnlyList<MenuDto>>> GetAll(CancellationToken cancellationToken)
    {
        var menus = await getMenuHandler.HandleAsync(new GetPublicMenuQuery(), cancellationToken);
        return Ok(menus);
    }

    [HttpGet("menus")]
    public async Task<ActionResult<IReadOnlyList<MenuSummaryDto>>> GetMenus(CancellationToken cancellationToken)
    {
        var menus = await getMenusHandler.HandleAsync(new GetPublicMenusQuery(), cancellationToken);
        return Ok(menus);
    }

    [HttpGet("menus/{menuId:guid}/dishes")]
    public async Task<ActionResult<PaginatedResult<DishDto>>> GetMenuDishes(
        Guid menuId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var dishes = await getMenuDishesHandler.HandleAsync(
            new GetPublicMenuDishesQuery(menuId, page, pageSize),
            cancellationToken);
        return Ok(dishes);
    }
}
