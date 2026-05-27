using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MonsoonMasala.Application.Abstractions;
using MonsoonMasala.Domain.Entities;

namespace MonsoonMasala.Application.Advertisements;

public sealed record UpdateAdvertisementCommand(
    Guid Id,
    string? Text,
    string? Url,
    bool IsActive,
    DateTimeOffset ActiveFrom,
    int Order,
    bool ReplaceMedia,
    IFormFileCollection Files) : ICommand<AdvertisementDto>;

public sealed class UpdateAdvertisementCommandHandler(IAppDbContext dbContext, IAdvertisementMediaStorageProxy mediaStorageProxy) : ICommandHandler<UpdateAdvertisementCommand, AdvertisementDto>
{
    public async Task<AdvertisementDto> HandleAsync(UpdateAdvertisementCommand command, CancellationToken cancellationToken = default)
    {
        var advertisement = await dbContext.Advertisements
            .Include(a => a.Media)
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken);

        if (advertisement is null)
        {
            throw new KeyNotFoundException("Advertisement was not found.");
        }

        var oldOrder = advertisement.Order;
        var newOrder = command.Order;

        // Handle order shifting if the order has changed
        if (newOrder != oldOrder && newOrder > 0)
        {
            if (newOrder < oldOrder)
            {
                // Moving up: increment orders between newOrder and oldOrder-1 by 1
                var adsToShift = await dbContext.Advertisements
                    .Where(a => a.Id != command.Id &&
                                a.Order >= newOrder &&
                                a.Order < oldOrder)
                    .ToListAsync(cancellationToken);

                foreach (var adToShift in adsToShift)
                {
                    adToShift.Order++;
                }
            }
            else
            {
                // Moving down: decrement orders between oldOrder+1 and newOrder by 1
                var adsToShift = await dbContext.Advertisements
                    .Where(a => a.Id != command.Id &&
                                a.Order > oldOrder &&
                                a.Order <= newOrder)
                    .ToListAsync(cancellationToken);

                foreach (var adToShift in adsToShift)
                {
                    adToShift.Order--;
                }
            }
        }

        advertisement.Text = command.Text?.Trim();
        advertisement.Url = command.Url;
        advertisement.IsActive = command.IsActive;
        advertisement.ActiveFrom = command.ActiveFrom;
        advertisement.Order = newOrder > 0 ? newOrder : oldOrder;
        advertisement.UpdatedAt = DateTimeOffset.UtcNow;

        var uploads = await mediaStorageProxy.UploadAsync(command.Files, cancellationToken);
        var upload = uploads.FirstOrDefault();

        if (upload is not null)
        {
            advertisement.Media = new AdvertisementMedia
            {
                AdvertisementId = advertisement.Id,
                MediaUrl = upload.Url,
                PublicId = upload.PublicId,
                MediaType = upload.MediaType
            };
        }
        else if (command.ReplaceMedia)
        {
            throw new InvalidOperationException("Media file is required when replacing advertisement media.");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return advertisement.ToDto();
    }
}
