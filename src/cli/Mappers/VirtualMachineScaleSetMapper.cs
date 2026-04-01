using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class VirtualMachineScaleSetMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Compute/virtualMachineScaleSets";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource, string currency = "USD")
    {
        var vmSize = GetVmSize(resource);
        var region = resource.Location;

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Virtual Machines",
                ArmRegionName: region,
                ArmSkuName: vmSize,
                CurrencyCode: currency,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var vmSize = GetVmSize(resource);
        var instanceCount = GetInstanceCount(resource);

        var price = prices
            .Where(p => !string.IsNullOrEmpty(p.ArmSkuName)
                && p.ArmSkuName.Equals(vmSize, StringComparison.OrdinalIgnoreCase)
                && p.UnitOfMeasure == "1 Hour"
                && p.MeterName != null && !p.MeterName.Contains("Low Priority")
                && p.MeterName != null && !p.MeterName.Contains("Spot"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"VMSS {vmSize} × {instanceCount} — no pricing found");

        var monthlyCost = (decimal)price.UnitPrice * HoursPerMonth * instanceCount;
        return new MonthlyCost(monthlyCost, $"VMSS {vmSize} × {instanceCount} @ ${price.UnitPrice:F4}/hr × {HoursPerMonth} hrs");
    }

    private static string GetVmSize(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var skuName) && skuName.ValueKind == JsonValueKind.String)
            return skuName.GetString() ?? "Standard_D2s_v3";

        if (resource.Properties.TryGetValue("virtualMachineProfile", out var profile) &&
            profile.ValueKind == JsonValueKind.Object &&
            profile.TryGetProperty("hardwareProfile", out var hwProfile) &&
            hwProfile.ValueKind == JsonValueKind.Object &&
            hwProfile.TryGetProperty("vmSize", out var vmSizeProp) &&
            vmSizeProp.ValueKind == JsonValueKind.String)
        {
            return vmSizeProp.GetString() ?? "Standard_D2s_v3";
        }
        return "Standard_D2s_v3";
    }

    private static int GetInstanceCount(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("capacity", out var capacity))
        {
            if (capacity.ValueKind == JsonValueKind.Number)
                return capacity.GetInt32();
            if (capacity.ValueKind == JsonValueKind.String && int.TryParse(capacity.GetString(), out var parsed))
                return parsed;
        }
        return 2;
    }
}
