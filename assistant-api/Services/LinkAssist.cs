using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class LinkAssist
{
    // Data folder path relative to app root
    static readonly string dataFolder = Path.Combine("data");

    // Preload data once for the session (to improve performance)
    static readonly List<JObject> dataset = LoadAllJson();

    // Now returns also the extracted query as out param
    public static string ProcessAssistantReply(string assistantReply, out string query)
    {
        query = null;
        if (string.IsNullOrWhiteSpace(assistantReply))
            return null;

        // Handle {"response": "..."}
        if (assistantReply.TrimStart().StartsWith("{") && assistantReply.Contains("\"response\""))
        {
            try
            {
                var jobj = JObject.Parse(assistantReply);
                return $"Response: {jobj["response"]?.ToString() ?? "No response field."}";
            }
            catch (Exception ex)
            {
                return "Invalid response JSON: " + ex.Message;
            }
        }

        // Handle {"query": "..."}
        if (assistantReply.TrimStart().StartsWith("{") && assistantReply.Contains("\"query\""))
        {
            try
            {
                var jobj = JObject.Parse(assistantReply);
                query = jobj["query"]?.ToString();
                if (string.IsNullOrWhiteSpace(query))
                    return "No query string found in assistant reply.";

                if (dataset.Count == 0)
                    return "No product JSON data loaded.";

                var masterArray = new JArray(dataset);

                var results = masterArray.SelectTokens(query).ToList();
                if (results.Count == 0)
                    return "No matches found for the given query.";

                var sb = new System.Text.StringBuilder();
                int matchCount = 0;
                foreach (var result in results)
                {
                    sb.AppendLine(result.ToString(Newtonsoft.Json.Formatting.None));
                    matchCount++;
                }
                sb.AppendLine($"\nTotal matches: {matchCount}");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Invalid JSONPath or error: " + ex.Message;
            }
        }

        // Not valid structure
        return "Unrecognized assistant reply format.";
    }

    static List<JObject> LoadAllJson()
    {
        var list = new List<JObject>();
        if (!Directory.Exists(dataFolder))
        {
            Console.WriteLine($"Data folder '{dataFolder}' not found!");
            return list;
        }
        var files = Directory.GetFiles(dataFolder, "*.json");
        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var jObj = JObject.Parse(json);
                list.Add(jObj);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load {file}: {ex.Message}");
            }
        }
        return list;
    }
}
