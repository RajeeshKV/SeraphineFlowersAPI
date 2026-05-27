namespace MonsoonMasala.Infrastructure.Persistence;

public static class DatabaseConnectionString
{
    public static string Resolve(string? configuredConnectionString)
    {
        if (!string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return Normalize(configuredConnectionString);
        }

        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        return !string.IsNullOrWhiteSpace(databaseUrl)
            ? Normalize(databaseUrl)
            : "Host=localhost;Port=5432;Database=monsoon_masala;Username=postgres;Password=postgres";
    }

    private static string Normalize(string value)
    {
        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        var uri = new Uri(value);
        var credentials = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(credentials[0]);
        var password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : string.Empty;
        var database = uri.AbsolutePath.TrimStart('/');
        var port = uri.Port > 0 ? uri.Port : 5432;

        return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
}
