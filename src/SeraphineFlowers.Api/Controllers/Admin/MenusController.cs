using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SeraphineFlowers.Application.Abstractions;
using SeraphineFlowers.Application.Menus;

namespace SeraphineFlowers.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/menus")]
public sealed class MenusController(
    IQueryHandler<GetAllMenusQuery, IReadOnlyList<AdminMenu>> getMenusHandler,
    ICommandHandler<CreateMenuCommand, MenuDto> createHandler,
    ICommandHandler<UpdateMenuCommand, MenuDto> updateHandler,
    ICommandHandler<DeleteMenuCommand, bool> deleteHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MenuDto>>> Get(CancellationToken cancellationToken)
    {
        var menus = await getMenusHandler.HandleAsync(new GetAllMenusQuery(), cancellationToken);
        return Ok(menus);
    }

    [HttpPost]
    public async Task<ActionResult<MenuDto>> Create(CreateMenuRequest request, CancellationToken cancellationToken)
    {
        var menu = await createHandler.HandleAsync(new CreateMenuCommand(request.Name, request.Slug, request.Order, request.IsActive), cancellationToken);
        return CreatedAtAction(nameof(Create), new { id = menu.Id }, menu);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MenuDto>> Update(Guid id, UpdateMenuRequest request, CancellationToken cancellationToken)
    {
        var menu = await updateHandler.HandleAsync(new UpdateMenuCommand(id, request.Name, request.Slug, request.Order, request.IsActive), cancellationToken);
        return Ok(menu);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await deleteHandler.HandleAsync(new DeleteMenuCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record CreateMenuRequest(string Name, string? Slug, int Order = 0, bool IsActive = true);
public sealed record UpdateMenuRequest(string Name, string? Slug, int Order = 0, bool IsActive = true);
