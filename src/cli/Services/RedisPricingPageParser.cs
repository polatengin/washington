using System.Text.Json;
using System.Text.RegularExpressions;
using Washington.Models;

namespace Washington.Services;

internal static partial class RedisPricingPageParser
{
    private const string PricingPageUrl = "https://azure.microsoft.com/en-us/pricing/details/cache/";

    private static readonly IReadOnlyDictionary<string, string> ArmRegionToPricingPageRegion =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["australiacentral"] = "australia-central",
            ["australiacentral2"] = "australia-central-2",
            ["australiaeast"] = "australia-east",
            ["australiasoutheast"] = "australia-southeast",
            ["austriaeast"] = "austria-east",
            ["belgiumcentral"] = "belgium-central",
            ["brazilsouth"] = "brazil-south",
            ["brazilsoutheast"] = "brazil-southeast",
            ["canadacentral"] = "canada-central",
            ["canadaeast"] = "canada-east",
            ["centralindia"] = "central-india",
            ["centralus"] = "us-central",
            ["chilecentral"] = "chile-central",
            ["denmarkeast"] = "denmark-east",
            ["eastasia"] = "asia-pacific-east",
            ["eastus"] = "us-east",
            ["eastus2"] = "us-east-2",
            ["francecentral"] = "france-central",
            ["francesouth"] = "france-south",
            ["germanynorth"] = "germany-north",
            ["germanywestcentral"] = "germany-west-central",
            ["indonesiacentral"] = "indonesia-central",
            ["israelcentral"] = "israel-central",
            ["italynorth"] = "italy-north",
            ["japaneast"] = "japan-east",
            ["japanwest"] = "japan-west",
            ["koreacentral"] = "korea-central",
            ["koreasouth"] = "korea-south",
            ["malaysiawest"] = "malaysia-west",
            ["mexicocentral"] = "mexico-central",
            ["newzealandnorth"] = "new-zealand-north",
            ["northcentralus"] = "us-north-central",
            ["northeurope"] = "europe-north",
            ["norwayeast"] = "norway-east",
            ["norwaywest"] = "norway-west",
            ["polandcentral"] = "poland-central",
            ["qatarcentral"] = "qatar-central",
            ["southafricanorth"] = "south-africa-north",
            ["southafricawest"] = "south-africa-west",
            ["southcentralus"] = "us-south-central",
            ["southeastasia"] = "asia-pacific-southeast",
            ["southindia"] = "south-india",
            ["spaincentral"] = "spain-central",
            ["swedencentral"] = "sweden-central",
            ["swedensouth"] = "sweden-south",
            ["switzerlandnorth"] = "switzerland-north",
            ["switzerlandwest"] = "switzerland-west",
            ["uaecentral"] = "uae-central",
            ["uaenorth"] = "uae-north",
            ["uksouth"] = "united-kingdom-south",
            ["ukwest"] = "united-kingdom-west",
            ["usgovarizona"] = "usgov-arizona",
            ["usgovtexas"] = "usgov-texas",
            ["usgovvirginia"] = "usgov-virginia",
            ["westcentralus"] = "us-west-central",
            ["westeurope"] = "europe-west",
            ["westindia"] = "west-india",
            ["westus"] = "us-west",
            ["westus2"] = "us-west-2",
            ["westus3"] = "us-west-3"
        };

    public static bool CanParse(PricingQuery query) =>
        string.Equals(query.ServiceName, "Azure Cache for Redis", StringComparison.OrdinalIgnoreCase);

    public static async Task<List<PriceRecord>> FetchPricesAsync(HttpClient httpClient, PricingQuery query)
    {
        if (!CanParse(query) || !TryGetPricingPageRegion(query.ArmRegionName, out var pricingPageRegion))
            return new List<PriceRecord>();

        var html = await httpClient.GetStringAsync(PricingPageUrl);
        return Parse(html, query.ArmRegionName, pricingPageRegion, query.SkuName);
    }

    internal static List<PriceRecord> Parse(
        string html,
        string armRegionName,
        string pricingPageRegion,
        string? requestedTier = null)
    {
        var prices = new List<PriceRecord>();

        foreach (Match sectionMatch in PricingSectionRegex().Matches(html))
        {
            var tier = sectionMatch.Groups["tier"].Value.Trim();
            if (!string.IsNullOrEmpty(requestedTier)
                && !string.Equals(tier, requestedTier, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (Match rowMatch in PricingRowRegex().Matches(sectionMatch.Groups["content"].Value))
            {
                if (!TryGetRegionalAmount(rowMatch.Groups["amount"].Value, pricingPageRegion, out var hourlyPrice))
                    continue;

                prices.Add(new PriceRecord
                {
                    ArmRegionName = armRegionName,
                    MeterName = rowMatch.Groups["code"].Value.Trim(),
                    ProductName = "Azure Cache for Redis",
                    ServiceName = "Azure Cache for Redis",
                    SkuName = tier,
                    UnitOfMeasure = "1 Hour",
                    UnitPrice = (double)hourlyPrice,
                    RetailPrice = (double)hourlyPrice,
                    Type = "Consumption"
                });
            }
        }

        return prices;
    }

    private static bool TryGetPricingPageRegion(string armRegionName, out string pricingPageRegion) =>
        ArmRegionToPricingPageRegion.TryGetValue(armRegionName, out pricingPageRegion!);

    private static bool TryGetRegionalAmount(string dataAmountJson, string pricingPageRegion, out decimal hourlyPrice)
    {
        using var doc = JsonDocument.Parse(dataAmountJson);

        if (doc.RootElement.TryGetProperty("regional", out var regional)
            && regional.TryGetProperty(pricingPageRegion, out var amount)
            && amount.ValueKind == JsonValueKind.Number)
        {
            hourlyPrice = amount.GetDecimal();
            return true;
        }

        hourlyPrice = 0m;
        return false;
    }

    [GeneratedRegex(@"<h2 class=""text-heading3"">(?<tier>Basic|Standard|Premium)</h2>(?<content>.*?)(?=<h2 class=""text-heading3"">|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex PricingSectionRegex();

    [GeneratedRegex(@"<tr>.*?<span>\s*(?<code>[A-Z]\d+)\s*</span>.*?data-amount='(?<amount>\{""regional"":\{.*?\}\})'", RegexOptions.Singleline)]
    private static partial Regex PricingRowRegex();
}