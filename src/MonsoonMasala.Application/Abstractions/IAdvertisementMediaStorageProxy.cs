using Microsoft.AspNetCore.Http;
using MonsoonMasala.Domain.Entities;

namespace MonsoonMasala.Application.Abstractions;

public interface IAdvertisementMediaStorageProxy
{
    Task<IReadOnlyList<UploadedMedia>> UploadAsync(IFormFileCollection files, CancellationToken cancellationToken = default);
}
