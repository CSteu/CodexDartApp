namespace DartsScoring.Api.Models.Responses;

public record LegStateResponse(
    int Id,
    int LegNumber,
    int StartingPlayerId,
    int? WinnerPlayerId,
    IReadOnlyList<TurnResponse> Turns,
    IReadOnlyList<X01PlayerStateResponse>? X01State,
    IReadOnlyList<CricketPlayerStateResponse>? CricketState
);

public record X01PlayerStateResponse(int PlayerId, int Remaining, double ThreeDartAverage);

public record CricketPlayerStateResponse(
    int PlayerId,
    int N15,
    int N16,
    int N17,
    int N18,
    int N19,
    int N20,
    int BullMarks,
    int Points
);
