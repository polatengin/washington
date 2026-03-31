using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class StorageAccountMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Storage/storageAccounts";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource, string currency = "USD")
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Storage",
                ArmRegionName: region,
                ProductName: GetProductName(resource),
                MeterName: "Hot LRS Data Stored",
                CurrencyCode: currency,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var sku = GetSkuDescription(resource);

        // Look for data stored pricing (per GB/month)
        var price = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Data Stored"))
            .OrderBy(p => p.TierMinimumUnits)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"{sku} — base cost + usage");

        // Estimate 100 GB as a baseline
        var estimatedGb = 100m;
        var monthlyCost = (decimal)price.UnitPrice * estimatedGb;
        return new MonthlyCost(monthlyCost, $"{sku} ~{estimatedGb} GB @ ${price.UnitPrice:F4}/GB + usage");
    }

    private static string GetProductName(ResourceDescriptor resource)
    {
        var redundancy = GetRedundancy(resource);
        var kind = GetKind(resource);

        if (kind.Contains("BlobStorage", StringComparison.OrdinalIgnoreCase))
            return $"Blob Storage";

        return redundancy switch
        {
            "Standard_LRS" => "Tables",
            "Standard_GRS" => "Tables",
            "Standard_RAGRS" => "Tables",
            "Standard_ZRS" => "Tables",
            _ => "General Block Blob"
        };
    }

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
        var accessTier = "Hot";
        if (resource.Properties.TryGetValue("accessTier", out var tier) && tier.ValueKind == JsonValueKind.String)
            accessTier = tier.GetString() ?? "Hot";
        return $"{kind} {redundancy} {accessTier}";
    }
}
