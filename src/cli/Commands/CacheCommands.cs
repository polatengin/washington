using System.CommandLine;
using Washington.Cache;

namespace Washington.Commands;

public class CacheClearCommand
{
    public static Command Create()
    {
        var command = new Command("clear", "Clear the pricing cache");

        command.SetAction(_ =>
        {
            var cache = new FilePricingCache();
            cache.Clear();
            Console.WriteLine("Pricing cache cleared.");
        });

        return command;
    }
}

public class CacheInfoCommand
{
    public static Command Create()
    {
        var command = new Command("info", "Show pricing cache information");

        command.SetAction(_ =>
        {
            var cache = new FilePricingCache();
            var (fileCount, totalBytes) = cache.GetInfo();
            Console.WriteLine($"Cache entries: {fileCount}");
            Console.WriteLine($"Cache size: {totalBytes / 1024.0:F1} KB");
        });

        return command;
    }
}
