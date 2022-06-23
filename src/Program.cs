using System.CommandLine;
using System.Text.Json;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var fileOption = new Option<FileInfo?>(name: "--file", description: "The file to read and display on the console.") { IsRequired = true };

        var rootCommand = new RootCommand("Bicep and ARM Cost Estimator");

        rootCommand.AddOption(fileOption);

        rootCommand.SetHandler(async (file) =>
        {
            Console.WriteLine($"File: {file}");
            await ReadFile(file!);
        }, fileOption);

        return await rootCommand.InvokeAsync(args);
    }

    public static async Task ReadFile(FileInfo file)
    {
        var filename = file.FullName;

        if (file.Extension == ".bicep")
        {
            filename = Path.GetTempFileName();

            var bicepCliArgs = new string[] { "build", file.FullName, "--outfile", filename };

            await Bicep.Cli.Program.Main(bicepCliArgs);
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

    }
}
