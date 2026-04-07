using Washington.Services;
using Xunit;

namespace Washington.Tests;

public class RedisPricingPageParserTests
{
    [Fact]
    public void Parse_ExtractsHourlyPricesForRequestedTierAndRegion()
    {
        var html = """
        <h2 class="text-heading3">Basic</h2>
        <div class="data-table-base data-table--pricing">
          <table>
            <tbody>
              <tr>
                <td><span>
        C1                </span></td>
                <td class="webdirect-price">
                  <span class='price-data' data-amount='{"regional":{"us-east":0.0550,"europe-west":0.0690}}'></span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <h2 class="text-heading3">Standard</h2>
        <div class="data-table-base data-table--pricing">
          <table>
            <tbody>
              <tr>
                <td><span>
        C1                </span></td>
                <td class="webdirect-price">
                  <span class='price-data' data-amount='{"regional":{"us-east":0.1380}}'></span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        """;

        var prices = RedisPricingPageParser.Parse(html, "eastus", "us-east", "Basic");

        var price = Assert.Single(prices);
        Assert.Equal("Azure Cache for Redis", price.ServiceName);
        Assert.Equal("Basic", price.SkuName);
        Assert.Equal("C1", price.MeterName);
        Assert.Equal("1 Hour", price.UnitOfMeasure);
        Assert.Equal(0.055d, price.UnitPrice, 3);
    }
}