using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class StorageAccountMapper : IResourceCostMapper
{
    private const decimal EstimatedStorageGb = 1000m;

    public string ResourceType => "Microsoft.Storage/storageAccounts";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Storage",
                ArmRegionName: region,
                MeterName: GetMeterName(resource),
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var sku = GetSkuDescription(resource);
        var meterName = GetMeterName(resource);
        var preferredProducts = GetPreferredProductNames(resource);

        var price = preferredProducts
            .Select(productName => prices
                .Where(p => string.Equals(p.MeterName, meterName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(p.ProductName, productName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.TierMinimumUnits)
                .FirstOrDefault())
            .FirstOrDefault(p => p != null);

        price ??= prices
            .Where(p => string.Equals(p.MeterName, meterName, StringComparison.OrdinalIgnoreCase)
                && p.ProductName != null
                && p.ProductName.Contains("Blob", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.TierMinimumUnits)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Data Stored", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.TierMinimumUnits)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"{sku} - no storage pricing found");

        var monthlyCost = (decimal)price.UnitPrice * EstimatedStorageGb;
        return new MonthlyCost(monthlyCost, $"{sku} ~{EstimatedStorageGb:F0} GB @ ${price.UnitPrice:F4}/GB");
    }

    private static IReadOnlyList<string> GetPreferredProductNames(ResourceDescriptor resource)
    {
        var kind = GetKind(resource);

        if (IsHierarchicalNamespaceEnabled(resource))
        {
            return new[]
            {
                "General Block Blob v2 Hierarchical Namespace",
                "Azure Data Lake Storage Gen2 Hierarchical Namespace",
                "Azure Data Lake Storage Gen2 Flat Namespace",
                "General Block Blob v2"
            };
        }

        if (kind.Contains("BlobStorage", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                "Blob Storage",
                "General Block Blob"
            };
        }

        return new[]
        {
            "General Block Blob v2",
            "Blob Storage",
            "General Block Blob"
        };
    }

    private static bool IsHierarchicalNamespaceEnabled(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("isHnsEnabled", out var isHnsEnabled))
        {
            if (isHnsEnabled.ValueKind == JsonValueKind.True)
                return true;
            if (isHnsEnabled.ValueKind == JsonValueKind.False)
                return false;
            if (isHnsEnabled.ValueKind == JsonValueKind.String &&
                bool.TryParse(isHnsEnabled.GetString(), out var parsed))
                return parsed;
        }

        return false;
    }

    private static string GetMeterName(ResourceDescriptor resource) =>
        $"{GetAccessTier(resource)} {GetRedundancyLabel(resource)} Data Stored";

    private static string GetAccessTier(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("accessTier", out var tier) && tier.ValueKind == JsonValueKind.String)
            return tier.GetString() ?? "Hot";

        return "Hot";
    }

    private static string GetRedundancyLabel(ResourceDescriptor resource) =>
        GetRedundancy(resource) switch
        {
            "Standard_LRS" => "LRS",
            "Premium_LRS" => "LRS",
            "Standard_GRS" => "GRS",
            "Standard_RAGRS" => "RA-GRS",
            "Standard_ZRS" => "ZRS",
            "Standard_GZRS" => "GZRS",
            "Standard_RAGZRS" => "RA-GZRS",
            var other => other.Replace("Standard_", "").Replace("Premium_", "").Replace('_', '-')
        };

    private static string GetRedundancy(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var skuName) && skuName.ValueKind == JsonValueKind.String)
            return skuName.GetString() ?? "Standard_LRS";
        return "Standard_LRS";
    }

    private static string GetKind(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("_kind", out var kind) && kind.ValueKind == JsonValueKind.String)
            return kind.GetString() ?? "StorageV2";
        return "StorageV2";
    }

    private static string GetSkuDescription(ResourceDescriptor resource)
    {
        var redundancy = GetRedundancy(resource);
        var kind = GetKind(resource);
        var accessTier = GetAccessTier(resource);
        return $"{kind} {redundancy} {accessTier}";
    }
}
