using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MonsoonMasala.Application.Abstractions;
using MonsoonMasala.Infrastructure.Auth;
using MonsoonMasala.Infrastructure.Media;
using MonsoonMasala.Infrastructure.Persistence;

namespace MonsoonMasala.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<CloudinaryOptions>(configuration.GetSection(CloudinaryOptions.SectionName));

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(DatabaseConnectionString.Resolve(configuration.GetConnectionString("DefaultConnection"))));

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IAuthTokenService, AuthTokenService>();
        services.AddScoped<IMediaStorageProxy, CloudinaryMediaStorageProxy>();
        services.AddScoped<IAdvertisementMediaStorageProxy, CloudinaryAdvertisementMediaStorageProxy>();

        return services;
    }
}
