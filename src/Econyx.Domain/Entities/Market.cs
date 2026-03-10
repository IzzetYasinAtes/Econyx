using Econyx.Core.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;

namespace Econyx.Domain.Entities;

public sealed class Market : AggregateRoot<Guid>
{
    private readonly List<MarketOutcome> _outcomes = [];

    public string ExternalId { get; private set; } = null!;
    public PlatformType Platform { get; private set; }
    public string Question { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string Category { get; private set; } = null!;
    public IReadOnlyList<MarketOutcome> Outcomes => _outcomes.AsReadOnly();
    public decimal VolumeUsd { get; private set; }
    public decimal Spread { get; private set; }
    public MarketStatus Status { get; private set; }
    public DateTime? ResolutionDate { get; private set; }
    public string? ResolvedOutcome { get; private set; }

    private Market() { }

    public static Market Create(
        string externalId,
        PlatformType platform,
        string question,
        string description,
        string category,
        IEnumerable<MarketOutcome> outcomes,
        decimal volumeUsd,
        decimal spread,
        DateTime? resolutionDate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(question);

        var market = new Market
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId,
            Platform = platform,
            Question = question,
            Description = description ?? string.Empty,
            Category = category ?? string.Empty,
            VolumeUsd = volumeUsd,
            Spread = spread,
            Status = MarketStatus.Open,
            ResolutionDate = resolutionDate
        };

        market._outcomes.AddRange(outcomes);
        return market;
    }

    public void UpdateOutcomes(IEnumerable<MarketOutcome> outcomes, decimal volumeUsd, decimal spread)
    {
        _outcomes.Clear();
        _outcomes.AddRange(outcomes);
        VolumeUsd = volumeUsd;
        Spread = spread;
        IncrementVersion();
    }

    public void Close()
    {
        Status = MarketStatus.Closed;
        IncrementVersion();
    }

    public void Resolve(string outcome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);
        Status = MarketStatus.Resolved;
        ResolvedOutcome = outcome;
        IncrementVersion();
    }
}
