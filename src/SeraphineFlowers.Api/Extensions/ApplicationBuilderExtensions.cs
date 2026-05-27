using Microsoft.EntityFrameworkCore;
using SeraphineFlowers.Infrastructure.Persistence;

namespace SeraphineFlowers.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        if (!bool.TryParse(app.Configuration["RUN_MIGRATIONS_ON_STARTUP"], out var shouldMigrate) || !shouldMigrate)
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
