using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Text;

namespace assistant_api.Services;

public class JsonDataLoader
{
    private readonly IConfiguration _config;
    private readonly IDistributedCache _cache;
    private readonly string _cacheKey;

    public JsonDataLoader(IConfiguration config, IDistributedCache cache)
    {
        _config = config;
        _cache = cache;
        _cacheKey = _config["Redis:CacheKey"] ?? "json_data_cache";
    }

    public async Task<List<JObject>> LoadDataAsync()
    {
        var cached = await _cache.GetStringAsync(_cacheKey);
        if (!string.IsNullOrEmpty(cached))
        {
            return JArray.Parse(cached).Select(obj => (JObject)obj).ToList();
        }

        var dataFolder = Path.Combine(Directory.GetCurrentDirectory(), _config["JsonDataPath"] ?? "src/data");

        var allObjects = LoadFromDisk(dataFolder);
        await CacheDataAsync(allObjects);

        return allObjects;
    }

    public List<JObject> LoadFromDisk(string folder)
    {
        var allObjects = new List<JObject>();
        foreach (var file in Directory.GetFiles(folder, "*.json"))
        {
            try
            {
                var text = File.ReadAllText(file);
                var obj = JObject.Parse(text);
                allObjects.Add(obj);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading {file}: {ex.Message}");
            }
        }
        return allObjects;
    }

    public async Task CacheDataAsync(List<JObject> data)
    {
        var json = JArray.FromObject(data).ToString();
        await _cache.SetStringAsync(_cacheKey, json);
    }

    public async Task RefreshFromDisk()
    {
        var dataFolder = Path.Combine(Directory.GetCurrentDirectory(), _config["JsonDataPath"] ?? "src/data");
        var allObjects = LoadFromDisk(dataFolder);
        await CacheDataAsync(allObjects);
    }
}
