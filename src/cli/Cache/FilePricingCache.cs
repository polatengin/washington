using System.Text.Json;
using Washington.Models;

namespace Washington.Cache;

public class FilePricingCache
{
    private readonly string _cacheDir;
    private readonly TimeSpan _ttl;

    public FilePricingCache(TimeSpan? ttl = null)
    {
        _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".bicep-cost-estimator", "cache");
        _ttl = ttl ?? TimeSpan.FromHours(24);
        Directory.CreateDirectory(_cacheDir);
    }

    public List<PriceRecord>? Get(string cacheKey)
    {
        var filePath = GetFilePath(cacheKey);
        if (!File.Exists(filePath))
            return null;

        var info = new FileInfo(filePath);
        if (DateTime.UtcNow - info.LastWriteTimeUtc > _ttl)
        {
            File.Delete(filePath);
            return null;
        }

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<List<PriceRecord>>(json);
    }

    public void Set(string cacheKey, List<PriceRecord> records)
    {
        var filePath = GetFilePath(cacheKey);
        var json = JsonSerializer.Serialize(records);
        File.WriteAllText(filePath, json);
    }

    public void Clear()
    {
        if (Directory.Exists(_cacheDir))
        {
            foreach (var file in Directory.GetFiles(_cacheDir))
                File.Delete(file);
        }
    }

    public (int FileCount, long TotalBytes) GetInfo()
    {
        if (!Directory.Exists(_cacheDir))
            return (0, 0);

        var files = Directory.GetFiles(_cacheDir);
        var totalBytes = files.Sum(f => new FileInfo(f).Length);
        return (files.Length, totalBytes);
    }

    private string GetFilePath(string cacheKey)
    {
        var hash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(cacheKey)))[..16];
        return Path.Combine(_cacheDir, $"{hash}.json");
    }
}
