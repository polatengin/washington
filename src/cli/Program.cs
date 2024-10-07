using System.Collections.Concurrent;
using System.CommandLine;
using System.Text.Json;
using System.Text.RegularExpressions;

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

    var rootCommand = new RootCommand("Azure Cost Estimator");

    rootCommand.AddOption(fileOption);

    rootCommand.SetHandler(async file => await CalculateCostEstimation(file!), fileOption);

    return await rootCommand.InvokeAsync(args);
  }

  private static async Task CalculateCostEstimation(FileInfo file)
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

    var client = new HttpClient();

    await Parallel.ForEachAsync(template.resources, async (resource, cancellationToken) =>
    {
      var properties = ResourceType.Types.FirstOrDefault(type => type.Name == resource.type);

      if (properties == null) continue;

      resource.name = EvaluateFormatExpression(resource.name);
      resource.serviceName = properties.ServiceName;
      resource.location = properties.Location();
      resource.size = properties.Size(resource);
      resource.kind = properties.Kind(resource);

      if (string.IsNullOrEmpty(resource.serviceName))
      {
        Console.WriteLine($"Resource {resource.name}({resource.type}) is skipped for cost estimation.");

        continue;
      }

      var response = await client.GetAsync($"https://azure.microsoft.com/api/v3/pricing/{resource.serviceName}/calculator/");

      var content = await response.Content.ReadAsStringAsync();

      var result = JsonSerializer.Deserialize<PriceResultRoot>(content)!;

      var offer = result.offers?.GetValueOrDefault(resource.kind);

      var perhour = offer?.prices.perhour.GetValueOrDefault(resource.location)?.value;

      resource.estimatedMonthlyCost = perhour * 24 * 30 ?? 0;

      Console.WriteLine($"{resource.name}({resource.serviceName}/{resource.size}) estimated monthly cost: {string.Format("{0:C2}", resource.estimatedMonthlyCost)}");
    });
  }

  private static string EvaluateParametersExpression(string input)
  {
    var pattern = @"\[\s*parameters\(\s*'([^']+)'(?:\s*,\s*'([^']+)')*\s*\)\s*\]";

    var match = Regex.Match(input, pattern);

    if (match.Success)
    {
      var parameter = match.Groups[1].Value;

      var argumentPattern = @"'([^']+)'";

      var argumentMatches = Regex.Matches(input, argumentPattern);

      var arguments = new List<string>();
      for (int i = 1; i < argumentMatches.Count; i++)
      {
        arguments.Add(argumentMatches[i].Groups[1].Value);
      }

      return string.Format(parameter, arguments.ToArray());
    }
    else
    {
      return input;
    }
  }

  private static string EvaluateFormatExpression(string input)
  {
    var pattern = @"\[\s*format\(\s*'([^']+)'(?:\s*,\s*'([^']+)')*\s*\)\s*\]";

    var match = Regex.Match(input, pattern);

    if (match.Success)
    {
      var format = match.Groups[1].Value;

      var argumentPattern = @"'([^']+)'";

      var argumentMatches = Regex.Matches(input, argumentPattern);

      var arguments = new List<string>();
      for (int i = 1; i < argumentMatches.Count; i++)
      {
        arguments.Add(argumentMatches[i].Groups[1].Value);
      }

      return string.Format(format, arguments.ToArray());
    }
    else
    {
      return input;
    }
  }
}
