using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MonsoonMasala.Application.Abstractions;
using MonsoonMasala.Domain.Entities;

namespace MonsoonMasala.Application.Advertisements;

public sealed record CreateAdvertisementCommand(
    string? Text,
    string? Url,
    bool IsActive,
    DateTimeOffset? ActiveFrom,
    int Order,
    IFormFileCollection Files) : ICommand<AdvertisementDto>;

public sealed class CreateAdvertisementCommandHandler(IAppDbContext dbContext, IAdvertisementMediaStorageProxy mediaStorageProxy) : ICommandHandler<CreateAdvertisementCommand, AdvertisementDto>
{
    public async Task<AdvertisementDto> HandleAsync(CreateAdvertisementCommand command, CancellationToken cancellationToken = default)
    {
        var maxOrder = await dbContext.Advertisements
            .Select(a => (int?)a.Order)
            .MaxAsync(cancellationToken) ?? 0;

        var uploads = await mediaStorageProxy.UploadAsync(command.Files, cancellationToken);
        var upload = uploads.FirstOrDefault();

        if (upload is null)
        {
            throw new InvalidOperationException("Media file is required for advertisement.");
        }

        var advertisement = new Advertisement
        {
            Text = command.Text?.Trim(),
            Url = command.Url,
            IsActive = command.IsActive,
            ActiveFrom = command.ActiveFrom ?? DateTimeOffset.UtcNow,
            Order = command.Order > 0 ? command.Order : maxOrder + 1,
            Media = new AdvertisementMedia
            {
                MediaUrl = upload.Url,
                PublicId = upload.PublicId,
                MediaType = upload.MediaType
            }
        };

        dbContext.Advertisements.Add(advertisement);
        await dbContext.SaveChangesAsync(cancellationToken);

        return advertisement.ToDto();
    }
}
