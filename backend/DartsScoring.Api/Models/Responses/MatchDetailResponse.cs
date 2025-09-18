using DartsScoring.Api.Domain.Enums;

namespace DartsScoring.Api.Models.Responses;

public record MatchDetailResponse(
    int Id,
    MatchMode Mode,
    int TargetScore,
    bool DoubleOut,
    MatchStatus Status,
    DateTime StartedAt,
    DateTime? FinishedAt,
    IReadOnlyList<PlayerResponse> Players,
    IReadOnlyList<LegStateResponse> Legs
);
