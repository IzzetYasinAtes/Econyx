namespace Econyx.Infrastructure.AiServices.PromptTemplates;

using Econyx.Application.Ports;

internal static class MarketAnalysisPrompt
{
    public static string Build(MarketAnalysisRequest request)
    {
        var outcomesSection = string.Join("\n", request.OutcomeNames.Select((name, i) =>
        {
            var price = i < request.CurrentPrices.Count ? request.CurrentPrices[i] : 0m;
            return $"  - \"{name}\": current market price = {price:F4} ({price * 100:F1}%)";
        }));

        return $$"""
            You are an expert prediction market analyst. Your task is to estimate fair probabilities for market outcomes.

            ## Market Details
            **Question:** {{request.Question}}
            **Description:** {{request.Description}}
            **Category:** {{request.Category}}
            **24h Volume (USD):** ${{request.VolumeUsd:N0}}

            ## Current Outcomes & Market Prices
            {{outcomesSection}}

            ## Instructions
            1. Analyze the question using your knowledge and reasoning.
            2. Estimate the fair probability (0.0 to 1.0) for EACH outcome.
            3. Probabilities should sum to approximately 1.0 for binary markets.
            4. Provide a confidence score (0.0 to 1.0) reflecting how certain you are in your estimates.
            5. Explain your reasoning in 1 sentence.

            ## Response Format
            Respond ONLY with valid JSON in this exact format (no markdown, no code fences):
            {
              "outcomes": [
                { "name": "outcome_name", "fairValue": 0.00 }
              ],
              "confidence": 0.00,
              "reasoning": "Your explanation here."
            }
            """;
    }
}
