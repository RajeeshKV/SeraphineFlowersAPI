using Microsoft.EntityFrameworkCore;
using MonsoonMasala.Application.Abstractions;

namespace MonsoonMasala.Application.Menus;

public sealed record GetPublicMenusQuery : IQuery<IReadOnlyList<MenuSummaryDto>>;

public sealed class GetPublicMenusQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetPublicMenusQuery, IReadOnlyList<MenuSummaryDto>>
{
    public async Task<IReadOnlyList<MenuSummaryDto>> HandleAsync(GetPublicMenusQuery query, CancellationToken cancellationToken = default)
    {
        return await dbContext.Menus
            .AsNoTracking()
            .Where(menu => menu.IsActive)
            .OrderBy(menu => menu.Order)
            .ThenBy(menu => menu.Name)
            .Select(menu => new MenuSummaryDto(
                menu.Id,
                menu.Name,
                menu.Order))
            .ToListAsync(cancellationToken);
    }
}
