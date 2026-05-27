using Microsoft.EntityFrameworkCore;
using MonsoonMasala.Application.Abstractions;

namespace MonsoonMasala.Application.Advertisements;

public sealed record GetAdvertisementByIdQuery(Guid Id) : IQuery<AdvertisementDto?>;

public sealed class GetAdvertisementByIdQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetAdvertisementByIdQuery, AdvertisementDto?>
{
    public async Task<AdvertisementDto?> HandleAsync(GetAdvertisementByIdQuery query, CancellationToken cancellationToken = default)
    {
        var advertisement = await dbContext.Advertisements
            .AsNoTracking()
            .Include(a => a.Media)
            .FirstOrDefaultAsync(a => a.Id == query.Id, cancellationToken);

        return advertisement?.ToDto();
    }
}
