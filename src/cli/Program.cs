using System.CommandLine;
using System.Text.Json;
using System.Collections.Concurrent;

public class Program
{
  public static async Task<int> Main(string[] args)
  {
    Console.WriteLine(@"
                                 _____          _     ______     _   _                 _
    /\                          / ____|        | |   |  ____|   | | (_)               | |
   /  \    _____   _ _ __ ___  | |     ___  ___| |_  | |__   ___| |_ _ _ __ ___   __ _| |_ ___  _ __
  / /\ \  |_  / | | | '__/ _ \ | |    / _ \/ __| __| |  __| / __| __| | '_ ` _ \ / _` | __/ _ \| '__|
 / ____ \  / /| |_| | | |  __/ | |___| (_) \__ \ |_  | |____\__ \ |_| | | | | | | (_| | || (_) | |
/_/    \_\/___|\__,_|_|  \___|  \_____\___/|___/\__| |______|___/\__|_|_| |_| |_|\__,_|\__\___/|_|

    ");

    var fileOption = new Option<FileInfo?>(name: "--file", description: "The file to read and display on the console.") { IsRequired = true };
    var locationOption = new Option<string>(name: "--location", description: "Azure location to deploy resources to") { IsRequired = true };

    var rootCommand = new RootCommand("Azure Cost Estimator");

    rootCommand.AddOption(fileOption);
    rootCommand.AddOption(locationOption);

    rootCommand.SetHandler(async (file, location) => await CalculateCostEstimation(file!, location), fileOption, locationOption);

    return await rootCommand.InvokeAsync(args);
  }

  private static async Task CalculateCostEstimation(FileInfo file, string location)
  {
    var filename = file.FullName;

    if (file.Extension == ".bicep")
    {
      filename = Path.GetTempFileName();

      await Bicep.Cli.Program.Main(new[] { "build", file.FullName, "--outfile", filename });
    }

    var jsonFileContent = File.ReadAllText(filename);

    var template = JsonSerializer.Deserialize<ARMTemplate>(jsonFileContent);

    if (template == null)
    {
      Console.WriteLine("Parsing input file failed...");

      return;
    }

    var buffer = new ConcurrentDictionary<string, PriceResultRoot>();

    foreach (var resource in template.resources)
    {
      var properties = ResourceType.Types.FirstOrDefault(type => type.Name == resource.type);

      if (properties == null) continue;

      resource.serviceName = properties.ServiceName;
      resource.location = properties.Location();
      resource.size = properties.Size(resource.properties);
      resource.kind = properties.Kind(resource.size);
    }

    var client = new HttpClient();

    foreach (var serviceName in template.resources.Select(resource => resource.serviceName).Distinct())
    {
      if (string.IsNullOrWhiteSpace(serviceName)) continue;

      if (!buffer.ContainsKey(serviceName))
      {
        var response = await client.GetAsync($"https://azure.microsoft.com/api/v3/pricing/{serviceName}/calculator/");

        var content = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<PriceResultRoot>(content);

        buffer.TryAdd(serviceName, result!);
      }
    }

    foreach (var resource in template!.resources)
    {
      var offers = buffer.GetValueOrDefault(resource.serviceName)?.offers;

      var offer = offers?.GetValueOrDefault(resource.offer);

      var perhour = offer?.prices.perhour.GetValueOrDefault("us-west")?.value;

      resource.estimatedMonthlyCost = perhour * 24 * 30 ?? 0;

      Console.WriteLine($"{resource.name}({resource.serviceName}/{resource.size}) estimated monthly cost: {string.Format("{0:C2}", resource.estimatedMonthlyCost)}");
    }
  }
}
