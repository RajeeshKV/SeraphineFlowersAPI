using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MonsoonMasala.Application.Abstractions;
using MonsoonMasala.Domain.Entities;
using System.Text.Json;

namespace MonsoonMasala.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<Dish> Dishes => Set<Dish>();
    public DbSet<DishMedia> DishMedia => Set<DishMedia>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Advertisement> Advertisements => Set<Advertisement>();
    public DbSet<AdvertisementMedia> AdvertisementMedia => Set<AdvertisementMedia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var ingredientsComparer = new ValueComparer<List<string>>(
            (left, right) => JsonSerializer.Serialize(left, JsonSerializerOptions.Default) == JsonSerializer.Serialize(right, JsonSerializerOptions.Default),
            value => JsonSerializer.Serialize(value, JsonSerializerOptions.Default).GetHashCode(),
            value => JsonSerializer.Deserialize<List<string>>(JsonSerializer.Serialize(value, JsonSerializerOptions.Default), JsonSerializerOptions.Default) ?? new List<string>());

        modelBuilder.Entity<Menu>(entity =>
        {
            entity.ToTable("menus");
            entity.HasKey(menu => menu.Id);
            entity.Property(menu => menu.Id).ValueGeneratedNever();
            entity.Property(menu => menu.Name).HasMaxLength(160).IsRequired();
            entity.Property(menu => menu.Slug).HasMaxLength(180).IsRequired();
            entity.HasIndex(menu => menu.Slug).IsUnique();
            entity.HasMany(menu => menu.Dishes).WithOne(dish => dish.Menu).HasForeignKey(dish => dish.MenuId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Dish>(entity =>
        {
            entity.ToTable("dishes", table => table.HasCheckConstraint("CK_dishes_Order_Positive", "\"Order\" > 0"));
            entity.HasKey(dish => dish.Id);
            entity.Property(dish => dish.Id).ValueGeneratedNever();
            entity.Property(dish => dish.Name).HasMaxLength(180).IsRequired();
            entity.Property(dish => dish.Slug).HasMaxLength(200).IsRequired();
            entity.Property(dish => dish.Description).HasMaxLength(1200);
            entity.Property(dish => dish.Price).HasPrecision(10, 2);
            entity.Property(dish => dish.FoodType)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(dish => dish.Ingredients)
                .HasColumnType("jsonb")
                .HasConversion(
                    value => JsonSerializer.Serialize(value, JsonSerializerOptions.Default),
                    value => JsonSerializer.Deserialize<List<string>>(value, JsonSerializerOptions.Default) ?? new List<string>())
                .Metadata.SetValueComparer(ingredientsComparer);
            entity.Property(dish => dish.Metadata).HasColumnType("jsonb");
            entity.HasIndex(dish => new { dish.MenuId, dish.Slug }).IsUnique();
            entity.HasMany(dish => dish.Media).WithOne(media => media.Dish).HasForeignKey(media => media.DishId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DishMedia>(entity =>
        {
            entity.ToTable("dish_media");
            entity.HasKey(media => media.Id);
            entity.Property(media => media.Id).ValueGeneratedNever();
            entity.Property(media => media.Url).HasMaxLength(1200).IsRequired();
            entity.Property(media => media.PublicId).HasMaxLength(500).IsRequired();
            entity.Property(media => media.MediaType)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            entity.HasIndex(media => media.DishId);
        });

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
    }
}
