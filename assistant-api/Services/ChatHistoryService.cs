using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace assistant_api.Services
{
    public class ChatHistoryService
    {
        private readonly IDistributedCache _cache;
        private const int ChatHistoryExpireHours = 24;

        public ChatHistoryService(IDistributedCache cache)
        {
            _cache = cache;
        }

        private static string GetKey(string sessionId) => $"chat:{sessionId}";

        public async Task<List<Dictionary<string, string>>> GetHistoryAsync(string sessionId)
        {
            var json = await _cache.GetStringAsync(GetKey(sessionId));
            return string.IsNullOrEmpty(json)
                ? new List<Dictionary<string, string>>()
                : JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(json);
        }

        public async Task SaveHistoryAsync(string sessionId, List<Dictionary<string, string>> history)
        {
            var json = JsonConvert.SerializeObject(history);
            await _cache.SetStringAsync(
                GetKey(sessionId),
                json,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(ChatHistoryExpireHours)
                }
            );
        }
    }
}
