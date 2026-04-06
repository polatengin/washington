using Washington.Models;

namespace Washington.Mappers;

public class ManagedIdentityMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.ManagedIdentity/userAssignedIdentities";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        // Managed Identities are free - no pricing queries needed
        return new List<PricingQuery>();
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        return new MonthlyCost(0, "User Assigned Managed Identity - free");
    }
}
