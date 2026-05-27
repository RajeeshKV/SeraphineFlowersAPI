using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonsoonMasala.Application.Abstractions;
using MonsoonMasala.Application.Advertisements;
using MonsoonMasala.Application.Common;

namespace MonsoonMasala.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/advertisements")]
public sealed class AdvertisementsController(
    IQueryHandler<GetAdvertisementsQuery, PaginatedResult<AdvertisementDto>> getAdvertisementsHandler,
    IQueryHandler<GetAdvertisementByIdQuery, AdvertisementDto?> getAdvertisementByIdHandler,
    ICommandHandler<CreateAdvertisementCommand, AdvertisementDto> createHandler,
    ICommandHandler<UpdateAdvertisementCommand, AdvertisementDto> updateHandler,
    ICommandHandler<DeleteAdvertisementCommand, bool> deleteHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<AdvertisementDto>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var advertisements = await getAdvertisementsHandler.HandleAsync(
            new GetAdvertisementsQuery(page, pageSize),
            cancellationToken);
        return Ok(advertisements);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdvertisementDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var advertisement = await getAdvertisementByIdHandler.HandleAsync(
            new GetAdvertisementByIdQuery(id),
            cancellationToken);

        if (advertisement is null)
        {
            return NotFound();
        }

        return Ok(advertisement);
    }

    [HttpPost]
    [RequestSizeLimit(200_000_000)]
    public async Task<ActionResult<AdvertisementDto>> Create([FromForm] CreateAdvertisementRequest request, CancellationToken cancellationToken)
    {
        var files = Request.Form.Files;
        var advertisement = await createHandler.HandleAsync(new CreateAdvertisementCommand(
            request.Text,
            request.Url,
            request.IsActive,
            request.ActiveFrom,
            request.Order,
            files), cancellationToken);

        return CreatedAtAction(nameof(Create), new { id = advertisement.Id }, advertisement);
    }

    [HttpPut("{id:guid}")]
    [RequestSizeLimit(200_000_000)]
    public async Task<ActionResult<AdvertisementDto>> Update(Guid id, [FromForm] UpdateAdvertisementRequest request, CancellationToken cancellationToken)
    {
        var files = Request.Form.Files;
        var advertisement = await updateHandler.HandleAsync(new UpdateAdvertisementCommand(
            id,
            request.Text,
            request.Url,
            request.IsActive,
            request.ActiveFrom,
            request.Order,
            request.ReplaceMedia,
            files), cancellationToken);

        return Ok(advertisement);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await deleteHandler.HandleAsync(new DeleteAdvertisementCommand(id), cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}

public sealed class CreateAdvertisementRequest
{
    public string? Text { get; set; }
    public string? Url { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? ActiveFrom { get; set; }
    public int Order { get; set; }
}

public sealed class UpdateAdvertisementRequest
{
    public string? Text { get; set; }
    public string? Url { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset ActiveFrom { get; set; } = DateTimeOffset.UtcNow;
    public int Order { get; set; }
    public bool ReplaceMedia { get; set; }
}
