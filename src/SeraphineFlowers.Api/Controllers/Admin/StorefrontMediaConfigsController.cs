using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SeraphineFlowers.Api.Storefront;
using SeraphineFlowers.Application.Common;
using SeraphineFlowers.Domain.Entities;
using SeraphineFlowers.Infrastructure.Persistence;
using SeraphineFlowers.Infrastructure.Storefront;

namespace SeraphineFlowers.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/storefront")]
public sealed class StorefrontMediaConfigsController(
    AppDbContext dbContext,
    IOptions<StorefrontOptions> storefrontOptions,
    CloudinaryStorefrontService cloudinaryStorefrontService) : ControllerBase
{
    private readonly StorefrontOptions _storefrontOptions = storefrontOptions.Value;

    [HttpGet("media-configs/{collectionKey}")]
    public async Task<ActionResult<PaginatedResult<MediaAssetConfigDto>>> GetMediaConfigs(
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

    [HttpPut("media-configs/{collectionKey}")]
    public async Task<ActionResult<IReadOnlyList<MediaAssetConfigDto>>> SaveMediaConfigs(string collectionKey, IReadOnlyList<SaveMediaAssetConfigRequest> request, CancellationToken cancellationToken)
    {
        var existing = await dbContext.MediaAssetConfigs.Where(item => item.CollectionKey == collectionKey).ToListAsync(cancellationToken);
        dbContext.MediaAssetConfigs.RemoveRange(existing);

        var folderName = StorefrontMediaSupport.ResolveCollectionFolder(collectionKey, _storefrontOptions);
        var items = request.Select((item, index) =>
        {
            var resolvedFolderName = string.IsNullOrWhiteSpace(item.FolderName) ? folderName : item.FolderName.Trim();
            var imageName = item.ImageName.Trim();
            return new MediaAssetConfig
            {
                CollectionKey = collectionKey,
                FolderName = resolvedFolderName,
                ImageName = imageName,
                ImageUrl = string.IsNullOrWhiteSpace(item.ImageUrl) ? cloudinaryStorefrontService.BuildImageUrl(resolvedFolderName, imageName) : item.ImageUrl.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(item.DisplayName) ? imageName : item.DisplayName.Trim(),
                Description = item.Description?.Trim() ?? string.Empty,
                Amount = item.Amount,
                SortOrder = item.SortOrder ?? index,
                UpdatedAt = DateTimeOffset.UtcNow
            };
        }).ToArray();

        await dbContext.MediaAssetConfigs.AddRangeAsync(items, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(items.Select(item => StorefrontMediaSupport.ToDto(item, cloudinaryStorefrontService)).ToArray());
    }

    [HttpPost("media-configs/{collectionKey}/backfill-image-urls")]
    public async Task<ActionResult<BackfillMediaImageUrlsResult>> BackfillImageUrls(string collectionKey, CancellationToken cancellationToken)
    {
        var items = await dbContext.MediaAssetConfigs
            .Where(item => item.CollectionKey == collectionKey && string.IsNullOrWhiteSpace(item.ImageUrl))
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            item.ImageUrl = cloudinaryStorefrontService.BuildImageUrl(item.FolderName, item.ImageName);
            item.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new BackfillMediaImageUrlsResult(items.Count));
    }

    [HttpGet("cloudinary/{collectionKey}/images")]
    public async Task<ActionResult<CloudinaryResourcePage>> ListCloudinaryImages(string collectionKey, [FromQuery] int pageSize = 10, [FromQuery] string? nextCursor = null, CancellationToken cancellationToken = default)
    {
        var folder = StorefrontMediaSupport.ResolveCollectionFolder(collectionKey, _storefrontOptions);
        var page = await cloudinaryStorefrontService.ListImagesAsync(folder, Math.Clamp(pageSize, 1, 100), nextCursor, cancellationToken);
        return Ok(page);
    }
}
