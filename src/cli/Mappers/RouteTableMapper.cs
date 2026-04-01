using Washington.Models;

namespace Washington.Mappers;

public class RouteTableMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Network/routeTables";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        // Route Tables are free — no pricing queries needed
        return new List<PricingQuery>();
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        return new MonthlyCost(0, "Route Table — free");
    }
}
