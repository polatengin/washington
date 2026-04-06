using Washington.Models;

namespace Washington.Mappers;

public class DevTestLabMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.DevTestLab/labs";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        // DevTest Labs itself is free - costs come from underlying VMs, disks, etc.
        return new List<PricingQuery>();
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        return new MonthlyCost(0, "DevTest Lab - free (costs from underlying VMs/disks)");
    }
}
