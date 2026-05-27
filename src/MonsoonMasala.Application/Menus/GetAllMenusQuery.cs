using Microsoft.EntityFrameworkCore;
using MonsoonMasala.Application.Abstractions;

namespace MonsoonMasala.Application.Menus;

public sealed record GetAllMenusQuery : IQuery<IReadOnlyList<AdminMenu>>;

public sealed class GetAllMenusQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetAllMenusQuery, IReadOnlyList<AdminMenu>>
{
    public async Task<IReadOnlyList<AdminMenu>> HandleAsync(GetAllMenusQuery query, CancellationToken cancellationToken = default)
    {
        return await dbContext.Menus
            .AsNoTracking()
            .OrderBy(menu => menu.Order)
            .ThenBy(menu => menu.Name)
            .Select(menu => new AdminMenu(
                menu.Id,
                menu.Name,
                menu.Order))
            .ToListAsync(cancellationToken);
    }
}
