using Washington.Models;

namespace Washington.Mappers;

public class StorageBlobContainerMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Storage/storageAccounts/blobServices/containers";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        return new List<PricingQuery>();
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        return new MonthlyCost(0, "Blob Container — billed through parent storage account usage");
    }
}