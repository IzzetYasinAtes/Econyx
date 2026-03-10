namespace Econyx.Application.Queries.GetMarketAnalysis;

using Econyx.Application.Ports;
using Econyx.Domain.Entities;
using Econyx.Domain.Repositories;
using MediatR;

public sealed class GetMarketAnalysisHandler : IRequestHandler<GetMarketAnalysisQuery, MarketAnalysisDto?>
{
    private readonly IMarketRepository _marketRepository;
    private readonly IAiProviderFactory _providerFactory;

    public GetMarketAnalysisHandler(
        IMarketRepository marketRepository,
        IAiProviderFactory providerFactory)
    {
        _marketRepository = marketRepository;
        _providerFactory = providerFactory;
    }

    public async Task<MarketAnalysisDto?> Handle(GetMarketAnalysisQuery request, CancellationToken cancellationToken)
    {
        var market = await _marketRepository.GetByIdAsync(request.MarketId, cancellationToken);

        if (market is null)
            return null;

        var analysisRequest = new MarketAnalysisRequest(
            market.Question,
            market.Description,
            market.Category,
            market.Outcomes.Select(o => o.Name).ToList(),
            market.Outcomes.Select(o => o.Price.Value).ToList(),
            market.VolumeUsd);

        FairValueResult? aiResult = null;
        try
        {
            var aiService = await _providerFactory.GetProviderAsync(cancellationToken);
            aiResult = await aiService.AnalyzeMarketAsync(analysisRequest, cancellationToken);
        }
        catch
        {
        }

        var outcomes = market.Outcomes
            .Select(o => new OutcomeDto(o.Name, o.Price.Value, o.Token.Value))
            .ToList();

        return new MarketAnalysisDto(
            market.Id,
            market.Question,
            market.Description,
            market.Category,
            market.Platform,
            market.Status,
            market.VolumeUsd,
            market.Spread,
            outcomes,
            aiResult);
    }
}
