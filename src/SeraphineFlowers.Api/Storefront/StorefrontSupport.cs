using System.Text.Json;
using SeraphineFlowers.Domain.Entities;
using SeraphineFlowers.Infrastructure.Storefront;

namespace SeraphineFlowers.Api.Storefront;

public static class StorefrontSupport
{
    public static string NormalizePhone(string? rawPhone)
    {
        if (string.IsNullOrWhiteSpace(rawPhone))
        {
            return string.Empty;
        }

        return new string(rawPhone.Where(char.IsDigit).ToArray());
    }

    public static bool IsValidIndianPhone(string rawPhone)
    {
        if (string.IsNullOrWhiteSpace(rawPhone) || rawPhone.Any(ch => !char.IsDigit(ch)))
        {
            return false;
        }

        if (rawPhone.Length != 10 || rawPhone[0] is < '6' or > '9')
        {
            return false;
        }

        if (rawPhone.All(ch => ch == rawPhone[0]))
        {
            return false;
        }

        return rawPhone is not "0123456789" and not "1234567890" and not "9876543210" and not "0987654321";
    }

    public static string ComputeOfferCode(int orderCount, OfferOptions options)
    {
        if (orderCount <= 0)
        {
            return "welcome";
        }

        return orderCount % Math.Max(options.LoyaltyEvery, 1) == 0 ? "loyalty" : "none";
    }

    public static OfferDto BuildOffer(Customer customer, OfferOptions options)
    {
        var code = ComputeOfferCode(customer.OrderCount, options);
        var label = code switch
        {
            "welcome" => $"INR {options.WelcomeDiscount} Welcome",
            "loyalty" => $"INR {options.LoyaltyDiscount} Loyalty",
            _ => $"{Math.Max(options.LoyaltyEvery - (customer.OrderCount % Math.Max(options.LoyaltyEvery, 1)), 0)} more until INR {options.LoyaltyDiscount}"
        };

        return new OfferDto(code, label, options.WelcomeDiscount, options.LoyaltyDiscount, options.LoyaltyEvery);
    }

    public static CustomerDto ToCustomerDto(Customer customer, OfferOptions options, bool includeOffer)
    {
        var offer = includeOffer ? BuildOffer(customer, options) : null;
        return new CustomerDto(
            customer.Id,
            customer.Phone,
            customer.Name,
            customer.RegisteredAt,
            customer.OrderCount,
            customer.Notes,
            customer.LastOrderAt,
            offer);
    }

    public static ReviewDto ToReviewDto(CustomerReview review)
    {
        return new ReviewDto(review.Id, review.Phone, review.Name, review.Rating, review.Text, review.Status, review.CreatedAt, review.UpdatedAt);
    }

    public static PromoDto ToPromoDto(PromoCampaign promo)
    {
        var images = DeserializeStringList(promo.ImagesJson);
        return new PromoDto(
            promo.Id,
            promo.Active,
            promo.ActiveFrom,
            promo.ActiveTo,
            promo.AdName,
            promo.AdDescription,
            promo.RedirectType,
            promo.RedirectValue,
            promo.CtaText,
            promo.ShowOncePerDay,
            images,
            promo.UpdatedAt);
    }

    public static IReadOnlyList<string> DeserializeStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static string SerializeStringList(IEnumerable<string> values)
    {
        return JsonSerializer.Serialize(values.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase));
    }

    public static DateTimeOffset? ParseDate(string? value)
    {
        return DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    }
}

public sealed record OfferDto(string Code, string Label, int WelcomeDiscount, int LoyaltyDiscount, int LoyaltyEvery);
public sealed record CustomerDto(Guid Id, string Phone, string Name, DateTimeOffset RegisteredAt, int OrderCount, string Notes, DateTimeOffset? LastOrderAt, OfferDto? Offer);
public sealed record ReviewDto(Guid Id, string Phone, string Name, int Rating, string Text, string Status, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record PromoDto(Guid Id, bool Active, DateTimeOffset? ActiveFrom, DateTimeOffset? ActiveTo, string AdName, string AdDescription, string RedirectType, string? RedirectValue, string CtaText, bool ShowOncePerDay, IReadOnlyList<string> Images, DateTimeOffset UpdatedAt);
