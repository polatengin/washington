using Washington.Models;

namespace Washington.Mappers;

public class SqlServerMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Sql/servers";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        return new List<PricingQuery>();
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        return new MonthlyCost(0, "SQL logical server - configuration only; billed through child databases and elastic pools");
    }
}
