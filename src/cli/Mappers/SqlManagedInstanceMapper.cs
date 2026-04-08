using System.Text.Json;
using Washington.Models;

namespace Washington.Mappers;

public class SqlManagedInstanceMapper : IResourceCostMapper
{
    public string ResourceType => "Microsoft.Sql/managedInstances";

    private const decimal HoursPerMonth = 730m;

    public bool CanMap(ResourceDescriptor resource) =>
        resource.ResourceType.Equals(ResourceType, StringComparison.OrdinalIgnoreCase);

    public List<PricingQuery> BuildQueries(ResourceDescriptor resource)
    {
        var region = resource.Location;
        var computeArmSkuName = GetComputeArmSkuName(resource);
        var storageMeterName = GetStorageMeterName(resource);

        return new List<PricingQuery>
        {
            new PricingQuery(
                ServiceName: "SQL Managed Instance",
                ArmRegionName: region,
                ArmSkuName: computeArmSkuName,
                PriceType: "Consumption"
            ),
            new PricingQuery(
                ServiceName: "SQL Managed Instance",
                ArmRegionName: region,
                MeterName: storageMeterName,
                PriceType: "Consumption"
            )
        };
    }

    public MonthlyCost CalculateCost(ResourceDescriptor resource, List<PriceRecord> prices)
    {
        var skuName = GetSkuName(resource);
        var vCores = GetVCores(resource);
        var storageSizeGb = GetStorageSizeGb(resource);
        var computeArmSkuName = GetComputeArmSkuName(resource);
        var storageMeterName = GetStorageMeterName(resource);

        var computePrice = prices
            .Where(p => string.Equals(p.ArmSkuName, computeArmSkuName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(p.MeterName, GetComputeMeterName(resource), StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        computePrice ??= prices
            .Where(p => string.Equals(p.ProductName, GetComputeProductName(resource), StringComparison.OrdinalIgnoreCase)
                && string.Equals(p.MeterName, GetComputeMeterName(resource), StringComparison.OrdinalIgnoreCase)
                && string.Equals(p.SkuName, "vCore", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        var storagePrice = prices
            .Where(p => string.Equals(p.MeterName, storageMeterName, StringComparison.OrdinalIgnoreCase)
                && p.UnitPrice > 0)
            .OrderBy(p => p.UnitPrice)
            .FirstOrDefault();

        if (computePrice == null && storagePrice == null)
            return new MonthlyCost(0, $"SQL MI {skuName} {vCores} vCores - no pricing found");

        var monthlyCost = 0m;
        var detailParts = new List<string>();

        if (computePrice != null)
        {
            monthlyCost += (decimal)computePrice.UnitPrice * HoursPerMonth * vCores;
            detailParts.Add($"SQL MI {skuName} {vCores} vCores @ ${computePrice.UnitPrice:F4}/vCore/hr × {HoursPerMonth:F0} hrs");
        }

        if (storagePrice != null && storageSizeGb > 0)
        {
            monthlyCost += (decimal)storagePrice.UnitPrice * storageSizeGb;
            detailParts.Add($"{storageSizeGb} GB storage @ ${storagePrice.UnitPrice:F4}/GB");
        }

        return new MonthlyCost(monthlyCost, string.Join(" + ", detailParts));
    }

    private static string GetSkuName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString() ?? "GP_Gen5";
        return "GP_Gen5";
    }

    private static string GetTierName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("tier", out var tier) && tier.ValueKind == JsonValueKind.String)
            return tier.GetString() ?? "GeneralPurpose";

        return GetSkuName(resource).StartsWith("BC_", StringComparison.OrdinalIgnoreCase)
            ? "BusinessCritical"
            : "GeneralPurpose";
    }

    private static string GetFamilyName(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("family", out var family) && family.ValueKind == JsonValueKind.String)
            return family.GetString() ?? "Gen5";

        var skuName = GetSkuName(resource);
        var parts = skuName.Split('_', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[^1] : "Gen5";
    }

    private static string GetComputeArmSkuName(ResourceDescriptor resource)
    {
        var tierCode = GetTierName(resource).Equals("BusinessCritical", StringComparison.OrdinalIgnoreCase) ? "BC" : "GP";
        var family = GetFamilyName(resource);
        var zoneRedundantSuffix = IsZoneRedundant(resource) ? "_ZR" : string.Empty;
        return $"SQLMI_{tierCode}_Compute_{family}{zoneRedundantSuffix}";
    }

    private static string GetComputeProductName(ResourceDescriptor resource)
    {
        var tier = GetTierName(resource).Equals("BusinessCritical", StringComparison.OrdinalIgnoreCase)
            ? "Business Critical"
            : "General Purpose";
        var family = GetFamilyName(resource);
        return $"SQL Managed Instance {tier} - Compute {family}";
    }

    private static string GetComputeMeterName(ResourceDescriptor resource) =>
        IsZoneRedundant(resource) ? "Zone Redundancy vCore" : "vCore";

    private static string GetStorageMeterName(ResourceDescriptor resource)
    {
        var tier = GetTierName(resource).Equals("BusinessCritical", StringComparison.OrdinalIgnoreCase)
            ? "Business Critical"
            : "General Purpose";

        return IsZoneRedundant(resource)
            ? $"{tier} Zone Redundancy Data Stored"
            : $"{tier} Data Stored";
    }

    private static bool IsZoneRedundant(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("zoneRedundant", out var zoneRedundant))
        {
            if (zoneRedundant.ValueKind == JsonValueKind.True)
                return true;

            if (zoneRedundant.ValueKind == JsonValueKind.False)
                return false;
        }

        return false;
    }

    private static int GetVCores(ResourceDescriptor resource)
    {
        if (resource.Sku.TryGetValue("capacity", out var cap) && cap.ValueKind == JsonValueKind.Number)
            return cap.GetInt32();
        if (resource.Properties.TryGetValue("vCores", out var vc) && vc.ValueKind == JsonValueKind.Number)
            return vc.GetInt32();
        return 4;
    }

    private static int GetStorageSizeGb(ResourceDescriptor resource)
    {
        if (resource.Properties.TryGetValue("storageSizeInGB", out var storage) && storage.ValueKind == JsonValueKind.Number)
            return storage.GetInt32();

        return 32;
    }
}
