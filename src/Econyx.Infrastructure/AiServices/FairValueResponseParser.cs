namespace Econyx.Infrastructure.AiServices;

using System.Text.Json;
using Econyx.Application.Ports;
using Econyx.Domain.ValueObjects;

internal static class FairValueResponseParser
{
    public static FairValueResult Parse(string json, decimal apiCost)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var outcomes = new List<OutcomeFairValue>();
            if (root.TryGetProperty("outcomes", out var outcomesArray))
            {
                foreach (var item in outcomesArray.EnumerateArray())
                {
                    var name = item.GetProperty("name").GetString() ?? "Unknown";
                    var fairValue = Math.Clamp(item.GetProperty("fairValue").GetDecimal(), 0m, 1m);
                    outcomes.Add(new OutcomeFairValue(name, Probability.Create(fairValue)));
                }
            }

            var confidence = root.TryGetProperty("confidence", out var confProp)
                ? Math.Clamp(confProp.GetDecimal(), 0m, 1m)
                : 0.5m;

            var reasoning = root.TryGetProperty("reasoning", out var reasonProp)
                ? reasonProp.GetString() ?? string.Empty
                : string.Empty;

            return new FairValueResult(outcomes, confidence, reasoning, apiCost);
        }
        catch (JsonException)
        {
            return new FairValueResult([], 0m, $"Failed to parse AI response: {json[..Math.Min(200, json.Length)]}", apiCost);
        }
    }
}
