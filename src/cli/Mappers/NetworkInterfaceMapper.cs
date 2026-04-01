using Washington.Models;

namespace Washington.Mappers;

public class NetworkInterfaceMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Network/networkInterfaces";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        // NICs are free — no pricing queries needed
        return new List<PricingQuery>();
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        return new MonthlyCost(0, "Network Interface — free");
    }
}
