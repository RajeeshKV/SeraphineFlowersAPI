using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SeraphineFlowers.Api.Storefront;
using SeraphineFlowers.Application.Common;
using SeraphineFlowers.Infrastructure.Persistence;
using SeraphineFlowers.Infrastructure.Storefront;

namespace SeraphineFlowers.Api.Controllers.Public;

[ApiController]
[Route("api/public")]
public sealed class StorefrontMediaController(
    AppDbContext dbContext,
    IOptions<StorefrontOptions> storefrontOptions,
    CloudinaryStorefrontService cloudinaryStorefrontService) : ControllerBase
{
    private readonly StorefrontOptions _storefrontOptions = storefrontOptions.Value;

    [HttpGet("media/{collectionKey}")]
    public async Task<ActionResult<PaginatedResult<MediaAssetConfigDto>>> GetMediaConfig(
        string collectionKey,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = dbContext.MediaAssetConfigs.AsNoTracking().Where(item => item.CollectionKey == collectionKey);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.ImageName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(Pagination.Create(items.Select(item => StorefrontMediaSupport.ToDto(item, cloudinaryStorefrontService)).ToArray(), page, pageSize, totalCount));
    }

    [HttpGet("cloudinary/{collectionKey}/images")]
    public async Task<ActionResult<CloudinaryResourcePage>> GetCloudinaryImages(string collectionKey, [FromQuery] int pageSize = 10, [FromQuery] string? nextCursor = null, CancellationToken cancellationToken = default)
    {
        var folder = StorefrontMediaSupport.ResolveCollectionFolder(collectionKey, _storefrontOptions);
        var page = await cloudinaryStorefrontService.ListImagesAsync(folder, Math.Clamp(pageSize, 1, 100), nextCursor, cancellationToken);
        return Ok(page);
    }
}
