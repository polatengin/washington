using System.Net.Http.Json;
using Washington.Cache;
using Washington.Models;

namespace Washington.Services;

public class PricingApiClient
{
    private static readonly HttpClient _httpClient = new();
    private const string BaseUrl = "https://prices.azure.com/api/retail/prices";
    private readonly FilePricingCache _cache;

    public PricingApiClient(FilePricingCache cache)
    {
        _cache = cache;
    }

    public async Task<List<PriceRecord>> QueryPricesAsync(PricingQuery query)
    {
        var cacheKey = query.ToCacheKey();
        var cached = _cache.Get(cacheKey);
        if (cached != null)
            return cached;

        var allItems = new List<PriceRecord>();
        var filter = query.ToFilterString();
        var url = $"{BaseUrl}?$filter={Uri.EscapeDataString(filter)}";

        var retryCount = 0;
        const int maxRetries = 3;

        while (!string.IsNullOrEmpty(url))
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<PriceApiResponse>(url);
                if (response?.Items != null)
                {
                    allItems.AddRange(response.Items);
                }
                url = response?.NextPageLink;
                retryCount = 0; // Reset on success
            }
            catch (HttpRequestException) when (retryCount < maxRetries)
            {
                retryCount++;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
            }
        }

        // Filter out spot/low-priority
        allItems = allItems
            .Where(p => p.SkuName == null || (!p.SkuName.Contains("Spot", StringComparison.OrdinalIgnoreCase)
                && !p.SkuName.Contains("Low Priority", StringComparison.OrdinalIgnoreCase)))
            .Where(p => p.Type != "DevTestConsumption")
            .ToList();

        _cache.Set(cacheKey, allItems);
        return allItems;
    }
}
