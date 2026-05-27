using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Application.Abstractions;
using SeraphineFlowers.Application.Common;

namespace SeraphineFlowers.Application.Advertisements;

public sealed record GetAdvertisementsQuery(int Page = 1, int PageSize = 10) : IQuery<PaginatedResult<AdvertisementDto>>;

public sealed class GetAdvertisementsQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetAdvertisementsQuery, PaginatedResult<AdvertisementDto>>
{
    public async Task<PaginatedResult<AdvertisementDto>> HandleAsync(GetAdvertisementsQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var totalCount = await dbContext.Advertisements.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var advertisements = await dbContext.Advertisements
            .AsNoTracking()
            .Include(a => a.Media)
            .OrderBy(a => a.Order)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = advertisements.Select(a => a.ToDto()).ToList();

        return new PaginatedResult<AdvertisementDto>(items, page, pageSize, totalCount, totalPages);
    }
}
