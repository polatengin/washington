using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class WebAppMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Web/sites";

    public bool CanMap(ResourceDescriptor resource)
    {
        if (!resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase))
            return false;

        var kind = GetKind(resource);
        if (kind.Contains("functionapp", StringComparison.OrdinalIgnoreCase))
            return false;

        return HasServerFarm(resource) || kind.Contains("app", StringComparison.OrdinalIgnoreCase);
    }

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        return new List<PricingQuery>();
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var details = HasServerFarm(resource)
            ? "Web App - billed through App Service Plan"
            : "Web App - no standalone site charge modeled";

        return new MonthlyCost(0, details);
    }

    private static string GetKind(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("_kind", out var kind) && kind.ValueKind == JsonValueKind.String)
            return kind.GetString() ?? "";
        return "";
    }

    private static bool HasServerFarm(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("serverFarmId", out var serverFarmId) &&
            serverFarmId.ValueKind == JsonValueKind.String)
            return !string.IsNullOrWhiteSpace(serverFarmId.GetString());

        return false;
    }
}
