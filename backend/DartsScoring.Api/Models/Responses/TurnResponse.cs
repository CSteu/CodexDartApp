namespace DartsScoring.Api.Models.Responses;

public record TurnResponse(
    int Id,
    int TurnNumber,
    int PlayerId,
    int TotalScored,
    bool WasBust,
    IReadOnlyList<ThrowResponse> Throws
);

public record ThrowResponse(int Id, int Multiplier, int Segment, int ScoreValue);
