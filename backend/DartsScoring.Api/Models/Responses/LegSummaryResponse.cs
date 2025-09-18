namespace DartsScoring.Api.Models.Responses;

public record LegSummaryResponse(
    int LegId,
    IReadOnlyList<TurnResponse> Turns,
    IReadOnlyList<PlayerLegSummary> PlayerSummaries
);

public record PlayerLegSummary(
    int PlayerId,
    double ThreeDartAverage,
    int? Remaining,
    int? CricketPoints
);
