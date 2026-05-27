using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Application.Abstractions;
using SeraphineFlowers.Domain.Entities;

namespace SeraphineFlowers.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Advertisement> Advertisements => Set<Advertisement>();
    public DbSet<AdvertisementMedia> AdvertisementMedia => Set<AdvertisementMedia>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerReview> CustomerReviews => Set<CustomerReview>();
    public DbSet<FlaggedCustomer> FlaggedCustomers => Set<FlaggedCustomer>();
    public DbSet<PromoCampaign> PromoCampaigns => Set<PromoCampaign>();
    public DbSet<MediaAssetConfig> MediaAssetConfigs => Set<MediaAssetConfig>();
    public DbSet<VisitorProfile> VisitorProfiles => Set<VisitorProfile>();
    public DbSet<CustomerRefreshToken> CustomerRefreshTokens => Set<CustomerRefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.ToTable("admin_users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).ValueGeneratedNever();
            entity.Property(user => user.Username).HasMaxLength(100).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(user => user.TokenVersion).HasDefaultValue(0).IsRequired();
            entity.HasIndex(user => user.Username).IsUnique();
            entity.HasMany(user => user.RefreshTokens).WithOne(token => token.AdminUser).HasForeignKey(token => token.AdminUserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(token => token.Id);
            entity.Property(token => token.Id).ValueGeneratedNever();
            entity.Property(token => token.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(token => token.ReplacedByTokenHash).HasMaxLength(128);
            entity.Ignore(token => token.IsActive);
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => token.AdminUserId);
        });

        modelBuilder.Entity<Advertisement>(entity =>
        {
            entity.ToTable("advertisements");
            entity.HasKey(ad => ad.Id);
            entity.Property(ad => ad.Id).ValueGeneratedNever();
            entity.Property(ad => ad.Text).HasMaxLength(500);
            entity.Property(ad => ad.Url).HasMaxLength(500);
            entity.Property(ad => ad.ActiveFrom).IsRequired();
            entity.HasOne(ad => ad.Media).WithOne(media => media.Advertisement).HasForeignKey<AdvertisementMedia>(media => media.AdvertisementId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(ad => new { ad.IsActive, ad.ActiveFrom });
        });

        modelBuilder.Entity<AdvertisementMedia>(entity =>
        {
            entity.ToTable("advertisement_media");
            entity.HasKey(media => media.Id);
            entity.Property(media => media.Id).ValueGeneratedNever();
            entity.Property(media => media.MediaUrl).HasMaxLength(1200).IsRequired();
            entity.Property(media => media.PublicId).HasMaxLength(500).IsRequired();
            entity.Property(media => media.MediaType)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            entity.HasIndex(media => media.AdvertisementId).IsUnique();
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");
            entity.HasKey(customer => customer.Id);
            entity.Property(customer => customer.Id).ValueGeneratedNever();
            entity.Property(customer => customer.FirebaseUid).HasMaxLength(200);
            entity.Property(customer => customer.Phone).HasMaxLength(20).IsRequired();
            entity.Property(customer => customer.Name).HasMaxLength(160).IsRequired();
            entity.Property(customer => customer.OfferCode).HasMaxLength(40).IsRequired();
            entity.Property(customer => customer.Notes).HasMaxLength(2000);
            entity.HasIndex(customer => customer.FirebaseUid).IsUnique();
            entity.HasIndex(customer => customer.Phone).IsUnique();
        });

        modelBuilder.Entity<CustomerReview>(entity =>
        {
            entity.ToTable("customer_reviews", table => table.HasCheckConstraint("CK_customer_reviews_Rating_Range", "\"Rating\" >= 1 AND \"Rating\" <= 5"));
            entity.HasKey(review => review.Id);
            entity.Property(review => review.Id).ValueGeneratedNever();
            entity.Property(review => review.Phone).HasMaxLength(20).IsRequired();
            entity.Property(review => review.Name).HasMaxLength(160).IsRequired();
            entity.Property(review => review.Text).HasMaxLength(1000).IsRequired();
            entity.Property(review => review.Status).HasMaxLength(40).IsRequired();
            entity.HasIndex(review => review.Phone).IsUnique();
        });

        modelBuilder.Entity<FlaggedCustomer>(entity =>
        {
            entity.ToTable("flagged_customers");
            entity.HasKey(flagged => flagged.Id);
            entity.Property(flagged => flagged.Id).ValueGeneratedNever();
            entity.Property(flagged => flagged.Phone).HasMaxLength(20).IsRequired();
            entity.Property(flagged => flagged.Reason).HasMaxLength(1000);
            entity.HasIndex(flagged => flagged.Phone).IsUnique();
        });

        modelBuilder.Entity<PromoCampaign>(entity =>
        {
            entity.ToTable("promo_campaigns");
            entity.HasKey(promo => promo.Id);
            entity.Property(promo => promo.Id).ValueGeneratedNever();
            entity.Property(promo => promo.AdName).HasMaxLength(200).IsRequired();
            entity.Property(promo => promo.AdDescription).HasMaxLength(2000);
            entity.Property(promo => promo.RedirectType).HasMaxLength(40).IsRequired();
            entity.Property(promo => promo.RedirectValue).HasMaxLength(1000);
            entity.Property(promo => promo.CtaText).HasMaxLength(120).IsRequired();
            entity.Property(promo => promo.ImagesJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<MediaAssetConfig>(entity =>
        {
            entity.ToTable("media_asset_configs");
            entity.HasKey(config => config.Id);
            entity.Property(config => config.Id).ValueGeneratedNever();
            entity.Property(config => config.CollectionKey).HasMaxLength(100).IsRequired();
            entity.Property(config => config.FolderName).HasMaxLength(200).IsRequired();
            entity.Property(config => config.ImageName).HasMaxLength(300).IsRequired();
            entity.Property(config => config.ImageUrl).HasMaxLength(1200);
            entity.Property(config => config.DisplayName).HasMaxLength(300).IsRequired();
            entity.Property(config => config.Description).HasMaxLength(2000);
            entity.Property(config => config.Amount).HasPrecision(10, 2);
            entity.HasIndex(config => new { config.CollectionKey, config.ImageName }).IsUnique();
        });

        modelBuilder.Entity<VisitorProfile>(entity =>
        {
            entity.ToTable("visitor_profiles");
            entity.HasKey(visitor => visitor.Id);
            entity.Property(visitor => visitor.Id).ValueGeneratedNever();
            entity.Property(visitor => visitor.VisitorKey).HasMaxLength(200).IsRequired();
            entity.Property(visitor => visitor.IpAddress).HasMaxLength(100).IsRequired();
            entity.Property(visitor => visitor.FirstUtmJson).HasColumnType("jsonb");
            entity.Property(visitor => visitor.LocationJson).HasColumnType("jsonb").IsRequired();
            entity.Property(visitor => visitor.DeviceJson).HasColumnType("jsonb").IsRequired();
            entity.Property(visitor => visitor.SessionsJson).HasColumnType("jsonb").IsRequired();
            entity.HasIndex(visitor => visitor.VisitorKey).IsUnique();
            entity.HasIndex(visitor => visitor.IpAddress);
        });

        modelBuilder.Entity<CustomerRefreshToken>(entity =>
        {
            entity.ToTable("customer_refresh_tokens");
            entity.HasKey(token => token.Id);
            entity.Property(token => token.Id).ValueGeneratedNever();
            entity.Property(token => token.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(token => token.ReplacedByTokenHash).HasMaxLength(128);
            entity.Ignore(token => token.IsActive);
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => token.CustomerId);
            entity.HasOne(token => token.Customer).WithMany().HasForeignKey(token => token.CustomerId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
