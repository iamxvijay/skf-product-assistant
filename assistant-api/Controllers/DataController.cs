using Microsoft.AspNetCore.Mvc;
using assistant_api.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace assistant_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DataController : ControllerBase
    {
        private readonly JsonDataLoader _dataLoader;

        public DataController(JsonDataLoader dataLoader)
        {
            _dataLoader = dataLoader;
        }

        /// <summary>
        /// Get all loaded JSON data from Redis cache (or from disk if Redis was empty).
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            List<JObject> data = await _dataLoader.LoadDataAsync();
            string rawJson = JsonConvert.SerializeObject(data, Formatting.Indented);
            return Content(rawJson, "application/json");
        }

        /// <summary>
        /// Reload all JSON files from disk and refresh Redis cache.
        /// </summary>
        [HttpPost("reload")]
        public async Task<IActionResult> ReloadFromDisk()
        {
            await _dataLoader.RefreshFromDisk();
            return Ok(new { message = "✅ Reloaded JSON files from disk and updated Redis cache." });
        }
    }
}
