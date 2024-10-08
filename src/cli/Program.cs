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

    var fileOption = new Option<FileInfo?>(name: "--file", description: "Deployment file (.bicep)") { IsRequired = true };
    var paramsFileOption = new Option<FileInfo?>(name: "--params-file", description: "Deployment configuration file (.bicepparam)") { IsRequired = true };

    var rootCommand = new RootCommand("Azure Cost Estimator");

    rootCommand.AddOption(fileOption);
    rootCommand.AddOption(paramsFileOption);

    rootCommand.SetHandler(async (file, paramFile) => await CalculateCostEstimation(file!, paramFile!), fileOption, paramsFileOption);

    return await rootCommand.InvokeAsync(args);
  }

  private static async Task CalculateCostEstimation(FileInfo file, FileInfo fileParam)
  {
    var deploymentFileContent = await ReadDeploymentFileContent(file);

    var template = JsonSerializer.Deserialize<ARMTemplate>(deploymentFileContent);

    if (template == null)
    {
      Console.WriteLine("Parsing input file failed...");

      return;
    }

    var deploymentParamFileContent = await ReadDeploymentParamFileContent(fileParam);

    var parameters = JsonSerializer.Deserialize<ARMParameter>(deploymentParamFileContent);

    if (parameters == null)
    {
      Console.WriteLine("Parsing input parameter file failed...");

      return;
    }

    var client = new HttpClient();

    var table = new ConsoleOutput("Type", "Name", "Location", "Size", "Service", "Estimated Monthly Cost");

    await Parallel.ForEachAsync(template.resources, async (resource, cancellationToken) =>
    {
      var properties = ResourceType.Types.FirstOrDefault(type => type.Name == resource.type);

      if (properties == null) return;

      resource.name = EvaluateExpressions(resource.name);
      resource.serviceName = properties.ServiceName;
      resource.location = properties.Location();
      resource.size = properties.Size(resource);
      resource.kind = properties.Kind(resource);

      if (string.IsNullOrEmpty(resource.serviceName))
      {
        table.AddRow($"Resource {resource.name}({resource.type}) is skipped for cost estimation (Free).");

        return;
      }

      var response = await client.GetAsync($"https://azure.microsoft.com/api/v3/pricing/{resource.serviceName}/calculator/");

      var content = await response.Content.ReadAsStringAsync();

      var result = JsonSerializer.Deserialize<PriceResultRoot>(content)!;

      var offer = result.offers?.GetValueOrDefault(resource.kind);

      var perhour = offer?.prices.perhour.GetValueOrDefault(resource.location)?.value;

      resource.estimatedMonthlyCost = perhour * 24 * 30 ?? 0;

      table.AddRow(resource.type, resource.name, resource.location, resource.size, resource.serviceName, string.Format("{0:C2}", resource.estimatedMonthlyCost));
    });

    table.Write();
  }

  private static async Task<string> ReadDeploymentFileContent(FileInfo file)
  {
    if (file.Extension == ".bicep")
    {
      var filename = Path.GetTempFileName();

      await Bicep.Cli.Program.Main(new[] { "build", file.FullName, "--outfile", filename });

      return File.ReadAllText(filename);
    }

    return File.ReadAllText(file.FullName);
  }

  private static async Task<string> ReadDeploymentParamFileContent(FileInfo file)
  {
    if (file.Extension == ".bicepparam")
    {
      var filename = Path.GetTempFileName();

      await Bicep.Cli.Program.Main(new[] { "build-params", file.FullName, "--outfile", filename });

      return File.ReadAllText(filename);
    }

    return File.ReadAllText(file.FullName);
  }

  private static string EvaluateExpressions(string input)
  {
    string _EvaluateFormatExpressions(string input)
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

    string _EvaluateParametersExpressions(string input)
    {
      var pattern = @"\[\s*parameters\(\s*'([^']+)'\s*\)\s*\]";

      var match = Regex.Match(input, pattern);

      if (match.Success)
      {
        var parameterName = match.Groups[1].Value;

        return parameterName;
      }
      else
      {
        return input;
      }
    }

    var output = input;

    output = _EvaluateFormatExpressions(output);
    output = _EvaluateParametersExpressions(output);

    return output;
  }
}
