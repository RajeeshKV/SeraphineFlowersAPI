using Microsoft.AspNetCore.Http;
using MonsoonMasala.Domain.Entities;

namespace MonsoonMasala.Application.Abstractions;

public sealed record UploadedMedia(string Url, string PublicId, MediaType MediaType);

public interface IMediaStorageProxy
{
    Task<IReadOnlyList<UploadedMedia>> UploadAsync(IFormFileCollection files, CancellationToken cancellationToken = default);
}
