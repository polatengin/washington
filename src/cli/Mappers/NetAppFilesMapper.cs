using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class NetAppFilesMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.NetApp/netAppAccounts/capacityPools";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Azure NetApp Files",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var serviceLevel = GetServiceLevel(resource);
        var sizeInTiB = GetSizeTiB(resource);

        var price = prices
            .Where(p => p.MeterName != null &&
                p.MeterName.Contains(serviceLevel, StringComparison.OrdinalIgnoreCase) &&
                p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"NetApp Files ({serviceLevel}) {sizeInTiB} TiB — no pricing found");

        // Pricing is typically per GiB/hr
        var sizeInGiB = sizeInTiB * 1024m;
        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth * sizeInGiB;
        return new MonthlyCost(monthlyCost, $"NetApp Files ({serviceLevel}) {sizeInTiB} TiB @ ${price.UnitPrice:F6}/GiB/hr × {HoursPerMonth} hrs");
    }

    private static string GetServiceLevel(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("serviceLevel", out var sl) &&
            sl.ValueKind == JsonValueKind.String)
            return sl.GetString() ?? "Premium";
        return "Premium";
    }

    private static decimal GetSizeTiB(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("size", out var size) &&
            size.ValueKind == JsonValueKind.Number)
        {
            // Size is in bytes, convert to TiB
            var bytes = size.GetInt64();
            return bytes / (1024m * 1024m * 1024m * 1024m);
        }
        return 4; // Default 4 TiB (minimum pool size)
    }
}
