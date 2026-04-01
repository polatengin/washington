using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class ManagedDiskMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Compute/disks";

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource, string currency = "USD")
    {
        var region = resource.Location;
        var skuName = GetSkuName(resource);

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "Storage",
                ArmRegionName: region,
                ProductName: "Premium SSD Managed Disks",
                SkuName: skuName,
                CurrencyCode: currency,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var skuName = GetSkuName(resource);
        var diskSize = GetDiskSizeGb(resource);

        var price = prices
            .Where(p => p.SkuName != null &&
                p.SkuName.Equals(skuName, StringComparison.OrdinalIgnoreCase) &&
                p.MeterName != null && p.MeterName.Contains("Disk"))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        price ??= prices
            .Where(p => p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (price == null)
            return new MonthlyCost(0, $"Managed Disk {skuName} {diskSize} GB — no pricing found");

        decimal monthlyCost;
        if (price.UnitOfMeasure == "1/Month")
            monthlyCost = (decimal)price.UnitPrice;
        else if (price.UnitOfMeasure == "1 Hour")
            monthlyCost = (decimal)price.UnitPrice * 730m;
        else
            monthlyCost = (decimal)price.UnitPrice;

        return new MonthlyCost(monthlyCost, $"Managed Disk {skuName} {diskSize} GB @ ${price.UnitPrice:F4}/{price.UnitOfMeasure}");
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "Premium_LRS";
        return "Premium_LRS";
    }

    private static int GetDiskSizeGb(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("diskSizeGB", out var size) && size.ValueKind == JsonValueKind.Number)
            return size.GetInt32();
        return 128;
    }
}
