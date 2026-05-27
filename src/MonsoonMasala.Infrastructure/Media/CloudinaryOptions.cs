namespace MonsoonMasala.Infrastructure.Media;

public sealed class CloudinaryOptions
{
    public const string SectionName = "Cloudinary";
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string Folder { get; set; } = "monsoon-masala/dishes";
    public string AdvertisementFolder { get; set; } = "monsoon-masala/advertisements";
}
