using System.Text.Json;
using System.Text.RegularExpressions;
using Washington.Models;

namespace Washington.Mappers;

public class MySqlFlexibleServerMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.DBforMySQL/flexibleServers";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure Database for MySQL",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var skuName = GetSkuName(resource);
        var tierSearchTokens = GetTierSearchTokens(resource);
        var seriesIdentifier = GetSeriesIdentifier(resource);
        var vCores = GetVCores(resource);
        var storageSizeGb = GetStorageSizeGb(resource);

        var exactComputePrice = prices
            .Where(p => IsFlexibleServerComputePrice(p)
                && (MatchesNormalizedSkuValue(p.SkuName, skuName)
                    || MatchesNormalizedSkuValue(p.ArmSkuName, skuName)
                    || (MatchesSeriesCompute(p, tierSearchTokens, seriesIdentifier)
                        && MatchesExactCoreCount(p, vCores))))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        var seriesVCorePrice = prices
            .Where(p => MatchesSeriesCompute(p, tierSearchTokens, seriesIdentifier)
                && IsPerVCorePrice(p))
            .OrderBy(GetPerVCorePreference)
            .ThenBy(p => p.UnitPrice)
            .FirstOrDefault();

        seriesVCorePrice ??= prices
            .Where(p => MatchesTierCompute(p, tierSearchTokens)
                && IsPerVCorePrice(p))
            .OrderBy(GetPerVCorePreference)
            .ThenBy(p => p.UnitPrice)
            .FirstOrDefault();

        var storagePrice = prices
            .Where(IsStoragePrice)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (exactComputePrice == null && seriesVCorePrice == null && storagePrice == null)
            return new MonthlyCost(0, $"MySQL Flexible {skuName} - no pricing found");

        var monthlyCost = 0m;
        var detailParts = new List<string>();

        if (exactComputePrice != null)
        {
            monthlyCost += (decimal)exactComputePrice.UnitPrice * HoursPerMonth;
            detailParts.Add($"MySQL Flexible {skuName} @ ${exactComputePrice.UnitPrice:F4}/hr × {HoursPerMonth:F0} hrs");
        }
        else if (seriesVCorePrice != null)
        {
            monthlyCost += (decimal)seriesVCorePrice.UnitPrice * HoursPerMonth * vCores;
            detailParts.Add($"MySQL Flexible {skuName} {vCores} vCores @ ${seriesVCorePrice.UnitPrice:F4}/vCore/hr × {HoursPerMonth:F0} hrs");
        }

        if (storagePrice != null && storageSizeGb > 0)
        {
            monthlyCost += (decimal)storagePrice.UnitPrice * storageSizeGb;
            detailParts.Add($"{storageSizeGb} GB storage @ ${storagePrice.UnitPrice:F4}/GB");
        }

        return new MonthlyCost(monthlyCost, string.Join(" + ", detailParts));
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "Standard_D2ds_v4";
        return "Standard_D2ds_v4";
    }

    private static string GetTierName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("tier", out var tier) && tier.ValueKind == JsonValueKind.String)
            return tier.GetString() ?? "GeneralPurpose";

        return "GeneralPurpose";
    }

    private static IReadOnlyList<string> GetTierSearchTokens(ResourceDescriptor resource) => GetTierName(resource) switch
    {
        "GeneralPurpose" => new[] { "General Purpose" },
        "BusinessCritical" => new[] { "Memory Optimized", "Business Critical" },
        "MemoryOptimized" => new[] { "Memory Optimized", "Business Critical" },
        "Burstable" => new[] { "Burstable" },
        var tierName => new[] { tierName }
    };

    private static int GetVCores(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("capacity", out var capacity) && capacity.ValueKind == JsonValueKind.Number)
            return capacity.GetInt32();

        var normalizedSkuName = NormalizeSkuName(GetSkuName(resource));
        var match = Regex.Match(normalizedSkuName, @"^[A-Za-z]+(?<primary>\d+)(?:-(?<secondary>\d+))?");

        if (match.Success)
        {
            var value = match.Groups["secondary"].Success
                ? match.Groups["secondary"].Value
                : match.Groups["primary"].Value;

            if (int.TryParse(value, out var vCores))
                return vCores;
        }

        return 2;
    }

    private static int GetStorageSizeGb(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("storage", out var storage)
            && storage.ValueKind == JsonValueKind.Object
            && storage.TryGetProperty("storageSizeGB", out var storageSize)
            && storageSize.ValueKind == JsonValueKind.Number)
        {
            return storageSize.GetInt32();
        }

        if (resource.Properties.TryGetValue("storageSizeGB", out var flatStorageSize)
            && flatStorageSize.ValueKind == JsonValueKind.Number)
        {
            return flatStorageSize.GetInt32();
        }

        return 32;
    }

    private static string GetSeriesIdentifier(ResourceDescriptor resource)
    {
        if (GetTierName(resource).Equals("Burstable", StringComparison.OrdinalIgnoreCase))
            return "BS";

        var normalizedSkuName = NormalizeSkuName(GetSkuName(resource));
        var parts = normalizedSkuName.Split('_', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return normalizedSkuName;

        var familyToken = parts[0];
        var seriesFamily = Regex.Replace(familyToken, @"[\d-]", string.Empty);
        var versionToken = parts.Length > 1 && parts[^1].StartsWith("v", StringComparison.OrdinalIgnoreCase)
            ? parts[^1]
            : string.Empty;

        return string.IsNullOrEmpty(versionToken)
            ? seriesFamily
            : $"{seriesFamily}{versionToken}";
    }

    private static string NormalizeSkuName(string skuName) => skuName.StartsWith("Standard_", StringComparison.OrdinalIgnoreCase)
        ? skuName["Standard_".Length..]
        : skuName;

    private static bool MatchesSeriesCompute(PriceRecord price, IReadOnlyList<string> tierSearchTokens, string seriesIdentifier) =>
        MatchesTierCompute(price, tierSearchTokens)
        && (NormalizePriceText(price.ProductName).Contains(seriesIdentifier, StringComparison.OrdinalIgnoreCase)
            || NormalizePriceText(price.ArmSkuName).Contains(seriesIdentifier, StringComparison.OrdinalIgnoreCase));

    private static bool MatchesTierCompute(PriceRecord price, IReadOnlyList<string> tierSearchTokens) =>
        IsFlexibleServerComputePrice(price)
        && tierSearchTokens.Any(token => NormalizePriceText(price.ProductName).Contains(token, StringComparison.OrdinalIgnoreCase)
            || NormalizePriceText(price.ArmSkuName).Contains(token, StringComparison.OrdinalIgnoreCase));

    private static string NormalizePriceText(string? value) =>
        (value ?? string.Empty)
            .Replace('_', ' ')
            .Replace('�', ' ');

    private static bool MatchesExactCoreCount(PriceRecord price, int vCores) =>
        string.Equals(price.MeterName, $"{vCores} vCore", StringComparison.OrdinalIgnoreCase)
        || string.Equals(price.SkuName, $"{vCores} vCore", StringComparison.OrdinalIgnoreCase)
        || string.Equals(price.MeterName, $"{vCores} vCores", StringComparison.OrdinalIgnoreCase)
        || string.Equals(price.SkuName, $"{vCores} vCores", StringComparison.OrdinalIgnoreCase);

    private static bool MatchesNormalizedSkuValue(string? left, string right)
    {
        if (string.IsNullOrWhiteSpace(left))
            return false;

        return NormalizeSkuForComparison(left) == NormalizeSkuForComparison(right);
    }

    private static string NormalizeSkuForComparison(string value) =>
        string.Concat(value.Where(char.IsLetterOrDigit)).ToLowerInvariant();

    private static bool IsFlexibleServerComputePrice(PriceRecord price) =>
        price.UnitPrice > 0
        && IsHourlyPrice(price)
        && price.ProductName?.Contains("Flexible Server", StringComparison.OrdinalIgnoreCase) == true
        && price.ProductName.Contains("Compute", StringComparison.OrdinalIgnoreCase);

    private static bool IsPerVCorePrice(PriceRecord price) =>
        string.Equals(price.SkuName, "1 vCore", StringComparison.OrdinalIgnoreCase)
        || string.Equals(price.SkuName, "vCore", StringComparison.OrdinalIgnoreCase)
        || string.Equals(price.MeterName, "1 vCore", StringComparison.OrdinalIgnoreCase)
        || string.Equals(price.MeterName, "vCore", StringComparison.OrdinalIgnoreCase);

    private static int GetPerVCorePreference(PriceRecord price)
    {
        if (string.Equals(price.SkuName, "1 vCore", StringComparison.OrdinalIgnoreCase)
            || string.Equals(price.MeterName, "1 vCore", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (string.Equals(price.SkuName, "vCore", StringComparison.OrdinalIgnoreCase)
            || string.Equals(price.MeterName, "vCore", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return 2;
    }

    private static bool IsStoragePrice(PriceRecord price) =>
        price.UnitPrice > 0
        && string.Equals(price.MeterName, "Storage Data Stored", StringComparison.OrdinalIgnoreCase)
        && price.ProductName?.Contains("Flexible Server Storage", StringComparison.OrdinalIgnoreCase) == true
        && (string.Equals(price.UnitOfMeasure, "1 GB/Month", StringComparison.OrdinalIgnoreCase)
            || string.Equals(price.UnitOfMeasure, "1 GiB/Month", StringComparison.OrdinalIgnoreCase));

    private static bool IsHourlyPrice(PriceRecord price) =>
        string.Equals(price.UnitOfMeasure, "1 Hour", StringComparison.OrdinalIgnoreCase)
        || string.Equals(price.UnitOfMeasure, "1/Hour", StringComparison.OrdinalIgnoreCase);
}
