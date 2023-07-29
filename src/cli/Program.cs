using System.CommandLine;
using System.Text.Json;

public class Program
{
  public static async Task<int> Main(string[] args)
  {
    var fileOption = new Option<FileInfo?>(name: "--file", description: "The file to read and display on the console.") { IsRequired = true };

    var rootCommand = new RootCommand("Bicep and ARM Cost Estimator");

    rootCommand.AddOption(fileOption);

    rootCommand.SetHandler(async (file) => await ReadFile(file!), fileOption);

    return await rootCommand.InvokeAsync(args);
  }

  public static async Task ReadFile(FileInfo file)
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

    foreach (var resource in template.resources)
    {
      var properties = ResourceType.Types.FirstOrDefault(type => type.Name == resource.type);

      if (properties == null) continue;

      resource.size = properties.Size(resource.properties);
      resource.serviceName = properties.ServiceName;
      resource.offer = properties.Offer(resource.size);
    }

    var buffer = new Dictionary<string, PriceResultRoot>();

    foreach (var serviceName in template.resources.Select(resource => resource.serviceName).Distinct())
    {
      var client = new HttpClient();
      var stream = await client.GetStreamAsync($"https://azure.microsoft.com/api/v3/pricing/{serviceName}/calculator/");
      var result = await JsonSerializer.DeserializeAsync<PriceResultRoot>(stream);

      if (!buffer.ContainsKey(serviceName))
      {
        buffer.Add(serviceName, result!);
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
