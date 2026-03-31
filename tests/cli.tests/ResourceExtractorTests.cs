using Washington.Services;
using Washington.Models;
using Xunit;

namespace Washington.Tests;

public class ResourceExtractorTests
{
    private readonly ResourceExtractor _extractor = new();

    [Fact]
    public void Extract_SingleVm_ReturnsOneDescriptor()
    {
        var json = File.ReadAllText(GetFixturePath("simple-vm.arm.json"));
        var resources = _extractor.Extract(json);

        Assert.Single(resources);
        Assert.Equal("Microsoft.Compute/virtualMachines", resources[0].ResourceType);
        Assert.Equal("test-vm", resources[0].Name);
        Assert.Equal("eastus", resources[0].Location);
    }

    [Fact]
    public void Extract_MultiResource_ReturnsAllResources()
    {
        var json = File.ReadAllText(GetFixturePath("multi-resource.arm.json"));
        var resources = _extractor.Extract(json);

        Assert.Equal(4, resources.Count);
        Assert.Contains(resources, r => r.ResourceType == "Microsoft.Compute/virtualMachines");
        Assert.Contains(resources, r => r.ResourceType == "Microsoft.Storage/storageAccounts");
        Assert.Contains(resources, r => r.ResourceType == "Microsoft.Web/serverfarms");
        Assert.Contains(resources, r => r.ResourceType == "Microsoft.Network/networkInterfaces");
    }

    [Fact]
    public void Extract_VmHasHardwareProfile()
    {
        var json = File.ReadAllText(GetFixturePath("simple-vm.arm.json"));
        var resources = _extractor.Extract(json);
        var vm = resources[0];

        Assert.True(vm.Properties.ContainsKey("hardwareProfile"));
    }

    [Fact]
    public void Extract_StorageAccountHasSku()
    {
        var json = File.ReadAllText(GetFixturePath("multi-resource.arm.json"));
        var resources = _extractor.Extract(json);
        var storage = resources.First(r => r.ResourceType == "Microsoft.Storage/storageAccounts");

        Assert.True(storage.Sku.ContainsKey("name"));
    }

    [Fact]
    public void Extract_SkipsConditionFalse()
    {
        var json = """
        {
            "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
            "contentVersion": "1.0.0.0",
            "resources": [
                {
                    "condition": false,
                    "type": "Microsoft.Compute/virtualMachines",
                    "apiVersion": "2023-09-01",
                    "name": "skipped-vm",
                    "location": "eastus",
                    "properties": {}
                },
                {
                    "type": "Microsoft.Compute/virtualMachines",
                    "apiVersion": "2023-09-01",
                    "name": "included-vm",
                    "location": "eastus",
                    "properties": {}
                }
            ]
        }
        """;

        var resources = _extractor.Extract(json);
        Assert.Single(resources);
        Assert.Equal("included-vm", resources[0].Name);
    }

    [Fact]
    public void Extract_EmptyTemplate_ReturnsEmpty()
    {
        var json = """
        {
            "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
            "contentVersion": "1.0.0.0",
            "resources": []
        }
        """;

        var resources = _extractor.Extract(json);
        Assert.Empty(resources);
    }

    private static string GetFixturePath(string fileName) =>
        Path.Combine("fixtures", fileName);
}
