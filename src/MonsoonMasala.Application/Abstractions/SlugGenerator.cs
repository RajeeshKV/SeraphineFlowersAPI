using System.Text.RegularExpressions;

namespace MonsoonMasala.Application.Abstractions;

public static partial class SlugGenerator
{
    public static string From(string value)
    {
        var slug = SlugRegex().Replace(value.Trim().ToLowerInvariant(), "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N") : slug;
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex SlugRegex();
}
