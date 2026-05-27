using Microsoft.EntityFrameworkCore;
using MonsoonMasala.Domain.Entities;

namespace MonsoonMasala.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Menu> Menus { get; }
    DbSet<Dish> Dishes { get; }
    DbSet<DishMedia> DishMedia { get; }
    DbSet<AdminUser> AdminUsers { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Advertisement> Advertisements { get; }
    DbSet<AdvertisementMedia> AdvertisementMedia { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
