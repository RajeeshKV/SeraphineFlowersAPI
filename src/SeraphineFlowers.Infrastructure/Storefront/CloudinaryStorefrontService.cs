using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SeraphineFlowers.Infrastructure.Media;

namespace SeraphineFlowers.Infrastructure.Storefront;

public sealed class CloudinaryStorefrontService(IOptions<CloudinaryOptions> cloudinaryOptions)
{
    private readonly CloudinaryOptions _cloudinaryOptions = cloudinaryOptions.Value;

    public async Task<JsonDocument?> FetchRawJsonAsync(string folder, string publicId, CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient();
        var url = BuildRawUrl(folder, publicId);
        using var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
    }

    public async Task<CloudinaryResourcePage> ListImagesAsync(string folder, int pageSize, string? nextCursor, CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient();
        var authBytes = Encoding.UTF8.GetBytes($"{_cloudinaryOptions.ApiKey}:{_cloudinaryOptions.ApiSecret}");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

        var builder = new StringBuilder($"https://api.cloudinary.com/v1_1/{_cloudinaryOptions.CloudName}/resources/image/upload?prefix={Uri.EscapeDataString(folder + "/")}&max_results={pageSize}");
        if (!string.IsNullOrWhiteSpace(nextCursor))
        {
            builder.Append("&next_cursor=").Append(Uri.EscapeDataString(nextCursor));
        }

        using var response = await client.GetAsync(builder.ToString(), cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);

        var items = new List<CloudinaryImageResource>();
        if (json.RootElement.TryGetProperty("resources", out var resources))
        {
            foreach (var resource in resources.EnumerateArray())
            {
                var publicId = resource.GetProperty("public_id").GetString() ?? string.Empty;
                items.Add(new CloudinaryImageResource(
                    publicId,
                    resource.TryGetProperty("secure_url", out var secureUrl) ? secureUrl.GetString() ?? string.Empty : string.Empty,
                    Path.GetFileName(publicId)));
            }
        }

        var cursor = json.RootElement.TryGetProperty("next_cursor", out var next) ? next.GetString() : null;
        return new CloudinaryResourcePage(items, cursor);
    }

    public string BuildRawUrl(string folder, string publicId)
    {
        return $"https://res.cloudinary.com/{_cloudinaryOptions.CloudName}/raw/upload/{folder}/{publicId}";
    }

    public string BuildImageUrl(string folder, string imageName)
    {
        var publicId = imageName.Contains('/', StringComparison.Ordinal)
            ? imageName
            : $"{folder}/{imageName}";

        var escapedPublicId = string.Join("/", publicId.Split('/').Select(Uri.EscapeDataString));
        return $"https://res.cloudinary.com/{_cloudinaryOptions.CloudName}/image/upload/{escapedPublicId}";
    }
}

public sealed record CloudinaryImageResource(string PublicId, string Url, string Name);
public sealed record CloudinaryResourcePage(IReadOnlyList<CloudinaryImageResource> Items, string? NextCursor);
