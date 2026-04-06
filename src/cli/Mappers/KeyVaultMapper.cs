using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class KeyVaultMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.KeyVault/vaults";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;
        var skuName = GetSkuName(resource);

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Key Vault",
                ArmRegionName: region,
                ProductName: $"Key Vault {skuName}",
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var skuName = GetSkuName(resource);

        // Look for operations-based pricing (secrets, keys)
        var price = prices
            .Where(p => p.MeterName != null &&
                (p.MeterName.Contains("Operations") || p.MeterName.Contains("Secret")))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        // Fallback: any matching price
        price ??= prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Key Vault {skuName} - base cost + usage");

        // Estimate 10,000 operations/month as baseline
        var estimatedOps = 10_000m;
        var unitSize = 10_000m; // pricing is per 10,000 operations
        var monthlyCost = (decimal)price.UnitPrice * (estimatedOps / unitSize);
        return new MonthlyCost(monthlyCost, $"Key Vault {skuName} ~{estimatedOps:N0} ops @ ${price.UnitPrice:F4}/10K ops + usage");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "standard";
        if (resource.Properties.TryGetValue("sku", out var skuProp) &&
            skuProp.ValueKind == JsonValueKind.Object &&
            skuProp.TryGetProperty("name", out var skuName) &&
            skuName.ValueKind == JsonValueKind.String)
            return skuName.GetString() ?? "standard";
        return "standard";
    }
}
