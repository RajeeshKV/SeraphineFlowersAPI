using Microsoft.AspNetCore.Http;
using SeraphineFlowers.Domain.Entities;

namespace SeraphineFlowers.Application.Abstractions;

public interface IAdvertisementMediaStorageProxy
{
    Task<IReadOnlyList<UploadedMedia>> UploadAsync(IFormFileCollection files, CancellationToken cancellationToken = default);
}
