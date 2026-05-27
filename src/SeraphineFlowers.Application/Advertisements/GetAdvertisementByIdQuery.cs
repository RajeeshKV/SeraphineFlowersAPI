using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Application.Abstractions;

namespace SeraphineFlowers.Application.Advertisements;

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
