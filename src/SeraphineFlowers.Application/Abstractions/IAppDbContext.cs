using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Domain.Entities;

namespace SeraphineFlowers.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<AdminUser> AdminUsers { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Advertisement> Advertisements { get; }
    DbSet<AdvertisementMedia> AdvertisementMedia { get; }
    DbSet<Customer> Customers { get; }
    DbSet<CustomerReview> CustomerReviews { get; }
    DbSet<FlaggedCustomer> FlaggedCustomers { get; }
    DbSet<PromoCampaign> PromoCampaigns { get; }
    DbSet<MediaAssetConfig> MediaAssetConfigs { get; }
    DbSet<VisitorProfile> VisitorProfiles { get; }
    DbSet<CustomerRefreshToken> CustomerRefreshTokens { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
