using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class KeyVaultMapper : IResourceCostMapper
{
    private const decimal EstimatedAdvancedOperations = 1_000_000m;
    private const decimal OperationUnitSize = 10_000m;

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
                ProductName: "Key Vault",
                SkuName: skuName,
                MeterName: "Advanced Key Operations",
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var skuName = GetSkuName(resource);

        var price = prices
            .Where(p => string.Equals(p.MeterName, "Advanced Key Operations", StringComparison.OrdinalIgnoreCase)
                && string.Equals(p.SkuName, skuName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(p.UnitOfMeasure, "10K", StringComparison.OrdinalIgnoreCase)
                && p.UnitPrice > 0)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.MeterName != null &&
                p.MeterName.Contains("Advanced Key Operations", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(p.UnitOfMeasure, "10K", StringComparison.OrdinalIgnoreCase) &&
                p.UnitPrice > 0)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.MeterName != null &&
                p.MeterName.Contains("Operations", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(p.UnitOfMeasure, "10K", StringComparison.OrdinalIgnoreCase) &&
                p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Key Vault {skuName} - no advanced operations pricing found");

        var monthlyCost = (decimal)price.UnitPrice * (EstimatedAdvancedOperations / OperationUnitSize);
        return new MonthlyCost(monthlyCost, $"Key Vault {skuName} ~{EstimatedAdvancedOperations:F0} Advanced Operations @ ${price.UnitPrice:F4}/10K ops");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return NormalizeSkuName(name.GetString());
        if (resource.Properties.TryGetValue("sku", out var skuProp) &&
            skuProp.ValueKind == JsonValueKind.Object &&
            skuProp.TryGetProperty("name", out var skuName) &&
            skuName.ValueKind == JsonValueKind.String)
            return NormalizeSkuName(skuName.GetString());
        return "Standard";
    }

    private static string NormalizeSkuName(string? skuName) =>
        skuName?.Trim().ToLowerInvariant() switch
        {
            "premium" => "Premium",
            _ => "Standard"
        };
}
