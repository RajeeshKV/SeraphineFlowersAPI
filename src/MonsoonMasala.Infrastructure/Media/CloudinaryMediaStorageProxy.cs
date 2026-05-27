using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MonsoonMasala.Application.Abstractions;
using MonsoonMasala.Domain.Entities;

namespace MonsoonMasala.Infrastructure.Media;

public sealed class CloudinaryMediaStorageProxy(IOptions<CloudinaryOptions> options) : IMediaStorageProxy
{
    private readonly CloudinaryOptions _options = options.Value;

    public async Task<IReadOnlyList<UploadedMedia>> UploadAsync(IFormFileCollection files, CancellationToken cancellationToken = default)
    {
        if (files.Count == 0)
        {
            return [];
        }

        var account = new Account(_options.CloudName, _options.ApiKey, _options.ApiSecret);
        var cloudinary = new Cloudinary(account);
        var uploads = new List<UploadedMedia>();

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var stream = file.OpenReadStream();
            var mediaType = file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ? MediaType.Video : MediaType.Image;
            RawUploadResult result;

            if (mediaType == MediaType.Video)
            {
                result = await cloudinary.UploadAsync(new VideoUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = _options.Folder,
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = false
                });
            }
            else
            {
                result = await cloudinary.UploadAsync(new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = _options.Folder,
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = false
                });
            }

            if (result.Error is not null)
            {
                throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");
            }

            uploads.Add(new UploadedMedia(result.SecureUrl?.ToString() ?? result.Url?.ToString() ?? string.Empty, result.PublicId, mediaType));
        }

        return uploads;
    }
}
