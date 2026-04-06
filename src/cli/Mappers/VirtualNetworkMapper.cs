using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class VirtualNetworkMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Network/virtualNetworks";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Virtual Network",
                ArmRegionName: region,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var peeringCount = GetPeeringCount(resource);

        if (peeringCount == 0)
            return new MonthlyCost(0, "Virtual Network - free (no peering)");

        // VNet peering data transfer pricing
        var price = prices
            .Where(p => p.MeterName != null &&
                (p.MeterName.Contains("Peering") || p.MeterName.Contains("Data Transfer")))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Virtual Network ({peeringCount} peerings) - peering data billed on usage");

        // Estimate 100 GB/month data transfer per peering
        var estimatedGb = 100m * peeringCount;
        var monthlyCost = (decimal)price.UnitPrice * estimatedGb;
        return new MonthlyCost(monthlyCost, $"VNet ({peeringCount} peerings) ~{estimatedGb} GB @ ${price.UnitPrice:F4}/GB");
    }

    private static int GetPeeringCount(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("virtualNetworkPeerings", out var peerings) &&
            peerings.ValueKind == JsonValueKind.Array)
            return peerings.GetArrayLength();
        return 0;
    }
}
