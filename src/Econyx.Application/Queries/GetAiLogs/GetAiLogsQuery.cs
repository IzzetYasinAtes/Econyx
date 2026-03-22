namespace Econyx.Application.Queries.GetAiLogs;

using Econyx.Domain.Repositories;
using MediatR;

public sealed record GetAiLogsQuery(int PageSize = 20, int Page = 1) : IRequest<AiLogsResult>;

public sealed record AiLogsResult(IReadOnlyList<AiLogDto> Items, int TotalCount, decimal TotalCost);

public sealed record AiLogDto(
    Guid Id,
    DateTime CreatedAt,
    string Provider,
    string ModelId,
    string MarketQuestion,
    string Prompt,
    string? Response,
    string? ParsedReasoning,
    decimal? FairValue,
    decimal? Confidence,
    int InputTokens,
    int OutputTokens,
    decimal CostUsd,
    bool IsSuccess,
    bool IsCacheHit,
    string? ErrorMessage);

public sealed class GetAiLogsHandler : IRequestHandler<GetAiLogsQuery, AiLogsResult>
{
    private readonly IAiRequestLogRepository _repository;

    public GetAiLogsHandler(IAiRequestLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<AiLogsResult> Handle(GetAiLogsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.PageSize, request.Page, cancellationToken);

        var dtos = items.Select(l => new AiLogDto(
            l.Id, l.CreatedAt, l.Provider, l.ModelId, l.MarketQuestion,
            l.Prompt, l.Response, l.ParsedReasoning, l.FairValue, l.Confidence,
            l.InputTokens, l.OutputTokens, l.CostUsd,
            l.IsSuccess, l.IsCacheHit, l.ErrorMessage)).ToList();

        var totalCost = items.Sum(l => l.CostUsd);

        return new AiLogsResult(dtos, totalCount, totalCost);
    }
}
