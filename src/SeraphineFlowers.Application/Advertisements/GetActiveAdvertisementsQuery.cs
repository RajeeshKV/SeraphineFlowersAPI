using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Application.Abstractions;

namespace SeraphineFlowers.Application.Advertisements;

public sealed record GetCurrentAdvertisementQuery : IQuery<AdvertisementDto?>;

public sealed class GetCurrentAdvertisementQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetCurrentAdvertisementQuery, AdvertisementDto?>
{
    public async Task<AdvertisementDto?> HandleAsync(GetCurrentAdvertisementQuery query, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var advertisement = await dbContext.Advertisements
            .AsNoTracking()
            .Include(a => a.Media)
            .Where(a => a.IsActive && a.ActiveFrom <= now)
            .OrderByDescending(a => a.ActiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        return advertisement?.ToDto();
    }
}
