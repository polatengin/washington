using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class RecoveryServicesVaultMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.RecoveryServices/vaults";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Backup",
                ArmRegionName: region,
                MeterName: "Protected Instances",
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var sku = GetSkuName(resource);

        var price = prices
            .Where(p => p.MeterName != null && p.MeterName.Contains("Protected Instance"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Recovery Services Vault ({sku}) - usage-based (backup storage + instances)");

        // Estimate 1 protected instance
        var monthlyCost = (decimal)price.UnitPrice;
        return new MonthlyCost(monthlyCost, $"Recovery Services Vault ({sku}) ~1 instance @ ${price.UnitPrice:F2}/mo + storage");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "Standard";
        return "Standard";
    }
}
