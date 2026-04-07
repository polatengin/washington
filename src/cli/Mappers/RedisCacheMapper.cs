using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class RedisCacheMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Cache/redis";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;
        var skuName = GetSkuName(resource);

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Cache for Redis",
                ArmRegionName: region,
                SkuName: skuName,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var skuName = GetSkuName(resource);
        var family = GetFamily(resource);
        var capacity = GetCapacity(resource);
        var cacheCode = $"{family}{capacity}";

        var price = prices
            .Where(p => IsHourlyPrice(p)
                && MatchesTier(p, skuName)
                && MatchesCacheCode(p, cacheCode))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => IsHourlyPrice(p) && MatchesCacheCode(p, cacheCode))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => IsHourlyPrice(p) && MatchesTier(p, skuName))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Redis {skuName} {family}{capacity} - no pricing found");

        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth;
        return new MonthlyCost(monthlyCost, $"Redis {skuName} {family}{capacity} @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "Basic";
        if (resource.Properties.TryGetValue("sku", out var skuProp) &&
            skuProp.ValueKind == JsonValueKind.Object &&
            skuProp.TryGetProperty("name", out var skuName) &&
            skuName.ValueKind == JsonValueKind.String)
            return skuName.GetString() ?? "Basic";
        return "Basic";
    }

    private static string GetFamily(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("family", out var fam) && fam.ValueKind == JsonValueKind.String)
            return fam.GetString() ?? "C";
        if (resource.Properties.TryGetValue("sku", out var skuProp) &&
            skuProp.ValueKind == JsonValueKind.Object &&
            skuProp.TryGetProperty("family", out var family) &&
            family.ValueKind == JsonValueKind.String)
            return family.GetString() ?? "C";
        return "C";
    }

    private static int GetCapacity(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("capacity", out var cap) && cap.ValueKind == JsonValueKind.Number)
            return cap.GetInt32();
        if (resource.Properties.TryGetValue("sku", out var skuProp) &&
            skuProp.ValueKind == JsonValueKind.Object &&
            skuProp.TryGetProperty("capacity", out var capacity) &&
            capacity.ValueKind == JsonValueKind.Number)
            return capacity.GetInt32();
        return 0;
    }

    private static bool IsHourlyPrice(PriceRecord price) =>
        string.Equals(price.UnitOfMeasure, "1 Hour", StringComparison.OrdinalIgnoreCase)
        && price.UnitPrice > 0;

    private static bool MatchesTier(PriceRecord price, string skuName) =>
        string.Equals(price.SkuName, skuName, StringComparison.OrdinalIgnoreCase)
        || price.ProductName?.Contains(skuName, StringComparison.OrdinalIgnoreCase) == true;

    private static bool MatchesCacheCode(PriceRecord price, string cacheCode)
    {
        var normalizedCode = Normalize(price.MeterName);
        var expected = Normalize(cacheCode);

        return normalizedCode == expected
            || normalizedCode == $"{expected}CACHE"
            || normalizedCode.Contains(expected, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string? value) =>
        string.Concat((value ?? string.Empty).Where(char.IsLetterOrDigit)).ToUpperInvariant();
}
