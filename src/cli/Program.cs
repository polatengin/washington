using System.Collections.Concurrent;
using System.CommandLine;
using System.Text.Json;
using System.Text.RegularExpressions;

public class Program
{
  public static async Task<int> Main(string[] args)
  {
    var fileOption = new Option<FileInfo?>(name: "--file", description: "Deployment file (.bicep)") { IsRequired = true };
    var paramsFileOption = new Option<FileInfo?>(name: "--params-file", description: "Deployment configuration file (.bicepparam)") { IsRequired = true };
    var grandTotalOption = new Option<bool>(name: "--grand-total", description: "Show grand total") { IsRequired = false };

    var rootCommand = new RootCommand("Azure Cost Estimator");

    rootCommand.AddOption(fileOption);
    rootCommand.AddOption(paramsFileOption);
    rootCommand.AddOption(grandTotalOption);

    rootCommand.SetHandler(async (file, paramFile, grandTotal) => await CalculateCostEstimation(file!, paramFile!, grandTotal!), fileOption, paramsFileOption, grandTotalOption);

    return await rootCommand.InvokeAsync(args);
  }

  private static async Task CalculateCostEstimation(FileInfo file, FileInfo fileParam, bool grandTotal)
  {
    var console = new ConsoleOutput("Type", "Name", "Location", "Size", "Service", "Estimated Monthly Cost");

    console.PrintLogo();

    await PrepareDeploymentTempFolder(Path.Combine(Path.GetTempPath(), "washington"));

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

    await Parallel.ForEachAsync(template.resources, async (resource, cancellationToken) =>
    {
      var properties = ResourceType.Types.FirstOrDefault(type => type.Name == resource.type);

      if (properties == null) return;

      resource.name = EvaluateExpressions(resource.name);
      resource.serviceName = properties.ServiceName;
      resource.location = properties.Location();
      resource.size = EvaluateExpressions(properties.Size(resource));
      resource.kind = properties.Kind(resource);

      if (string.IsNullOrEmpty(resource.serviceName))
      {
        console.AddRow($"Resource {resource.name}({resource.type}) is skipped for cost estimation (Free).");

        return;
      }

      resource.estimatedMonthlyCost = await GetPriceEstimation(resource.serviceName, resource.kind, resource.location);

      console.AddRow(resource.type, resource.name, resource.location, resource.size, resource.serviceName, string.Format("{0:C2}", resource.estimatedMonthlyCost));
    });

    console.AddGrandTotalRow(template.resources.Sum(r => r.estimatedMonthlyCost));

    console.Write();
  }

  private static async Task<decimal> GetPriceEstimation(string serviceName, string kind, string location)
  {
    var client = new HttpClient();

    var response = await client.GetAsync($"https://azure.microsoft.com/api/v3/pricing/{serviceName}/calculator/");

    var content = await response.Content.ReadAsStringAsync();

    var result = JsonSerializer.Deserialize<PriceResultRoot>(content)!;

    var offer = result.offers?.GetValueOrDefault(kind);

    var perhour = offer?.prices.perhour.GetValueOrDefault(location)?.value;

    return perhour * 24 * 30 ?? 0;
  }

  private static async Task PrepareDeploymentTempFolder(string path)
  {
    Directory.CreateDirectory(path);

    foreach (var file in Directory.GetFiles(path))
    {
      File.Delete(file);
    }

    File.WriteAllText(Path.Combine(path, "bicepconfig.json"), "{ \"analyzers\": { \"core\": { \"enabled\": false } } }");
  }

  private static async Task<string> ReadDeploymentFileContent(FileInfo file)
  {
    if (file.Extension == ".bicep")
    {
      var path = Path.Combine(Path.GetTempPath(), "washington");

      File.Copy(file.FullName, Path.Combine(path, file.Name));

      var outputFilename = Path.Combine(path, Path.GetRandomFileName());

      await Bicep.Cli.Program.Main(new[] { "build", Path.Combine(path, file.Name), "--outfile", outputFilename });

      return File.ReadAllText(outputFilename);
    }

    return File.ReadAllText(file.FullName);
  }

  private static async Task<string> ReadDeploymentParamFileContent(FileInfo file)
  {
    if (file.Extension == ".bicepparam")
    {
      var path = Path.Combine(Path.GetTempPath(), "washington");

      File.Copy(file.FullName, Path.Combine(path, file.Name));

      var outputFilename = Path.Combine(path, Path.GetRandomFileName());

      await Bicep.Cli.Program.Main(new[] { "build-params", Path.Combine(path, file.Name), "--outfile", outputFilename });

      return File.ReadAllText(outputFilename);
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
