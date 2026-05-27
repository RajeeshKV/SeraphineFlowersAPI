namespace SeraphineFlowers.Infrastructure.Storefront;

public sealed class StorefrontOptions
{
    public const string SectionName = "Storefront";

    public OfferOptions Offer { get; set; } = new();
    public CloudinaryFolderOptions Cloudinary { get; set; } = new();
    public ImportSourceOptions Imports { get; set; } = new();
    public bool RequireCustomerOtp { get; set; }
}

public sealed class OfferOptions
{
    public int WelcomeDiscount { get; set; } = 50;
    public int LoyaltyDiscount { get; set; } = 100;
    public int LoyaltyEvery { get; set; } = 5;
}

public sealed class CloudinaryFolderOptions
{
    public string GalleryFolder { get; set; } = "Seraphine-Gallery";
    public string TrendingFolder { get; set; } = "Seraphine-Trending";
    public string CustomerCollectionFolder { get; set; } = "Seraphine-CustomerCollection";
    public string PromoAdsFolder { get; set; } = "Seraphine-PromoAds";
}

public sealed class ImportSourceOptions
{
    public RawJsonSource Customers { get; set; } = new("Seraphine-Customers", "seraphine-customers.json");
    public RawJsonSource Reviews { get; set; } = new("Seraphine-Customers", "seraphine-reviews.json");
    public RawJsonSource Flagged { get; set; } = new("Seraphine-Customers", "seraphine-flagged.json");
    public RawJsonSource Analytics { get; set; } = new("Seraphine-Analytics", "seraphine-analytics.json");
    public RawJsonSource Promo { get; set; } = new("Seraphine-PromoAds", "seraphine-promo.json");
    public RawJsonSource GalleryConfig { get; set; } = new("Seraphine-Gallery", "seraphine-config.json");
    public RawJsonSource TrendingConfig { get; set; } = new("Seraphine-Trending", "seraphine-config.json");
    public RawJsonSource CustomerCollectionConfig { get; set; } = new("Seraphine-CustomerCollection", "seraphine-config.json");
}

public sealed record RawJsonSource(string Folder, string PublicId);
