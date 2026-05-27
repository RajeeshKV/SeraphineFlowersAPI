using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeraphineFlowers.Application.Abstractions;
using SeraphineFlowers.Infrastructure.Auth;
using SeraphineFlowers.Infrastructure.Firebase;
using SeraphineFlowers.Infrastructure.Media;
using SeraphineFlowers.Infrastructure.Persistence;
using SeraphineFlowers.Infrastructure.Storefront;

namespace SeraphineFlowers.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<CloudinaryOptions>(configuration.GetSection(CloudinaryOptions.SectionName));
        services.Configure<StorefrontOptions>(configuration.GetSection(StorefrontOptions.SectionName));

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(DatabaseConnectionString.Resolve(configuration.GetConnectionString("DefaultConnection"))));

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IAuthTokenService, AuthTokenService>();
        services.AddSingleton<IFirebaseTokenVerifier, FirebaseTokenVerifier>();
        services.AddSingleton<CloudinaryStorefrontService>();
        services.AddScoped<IMediaStorageProxy, CloudinaryMediaStorageProxy>();
        services.AddScoped<IAdvertisementMediaStorageProxy, CloudinaryAdvertisementMediaStorageProxy>();

        return services;
    }
}
