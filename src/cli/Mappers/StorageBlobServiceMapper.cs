using Washington.Models;

namespace Washington.Mappers;

public class StorageBlobServiceMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Storage/storageAccounts/blobServices";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        return new List<PricingQuery>();
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        return new MonthlyCost(0, "Blob Service - configuration only; advanced features billed through storage usage are not modeled");
    }
}