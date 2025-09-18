using DartsScoring.Api.Domain.Entities;
using DartsScoring.Api.Domain.Enums;
using DartsScoring.Api.Models.Responses;

namespace DartsScoring.Api.Mapping;

public static class LegMappingExtensions
{
    public static LegStateResponse ToLegState(this Leg leg, Match match)
    {
        var turns = leg.Turns
            .OrderBy(t => t.TurnNumber)
            .ThenBy(t => t.Id)
            .Select(t => new TurnResponse(
                t.Id,
                t.TurnNumber,
                t.PlayerId,
                t.TotalScored,
                t.WasBust,
                t.Throws
                    .OrderBy(th => th.Id)
                    .Select(th => new ThrowResponse(th.Id, th.Multiplier, th.Segment, th.ScoreValue))
                    .ToList()))
            .ToList();

        IReadOnlyList<X01PlayerStateResponse>? x01State = null;
        IReadOnlyList<CricketPlayerStateResponse>? cricketState = null;

        if (match.Mode == MatchMode.X01)
        {
            x01State = match.Players
                .OrderBy(p => p.Order)
                .Select(p => BuildX01State(leg, match.TargetScore, p.PlayerId))
                .ToList();
        }
        else if (match.Mode == MatchMode.Cricket)
        {
            cricketState = match.Players
                .OrderBy(p => p.Order)
                .Select(p => BuildCricketState(leg, p.PlayerId))
                .ToList();
        }

        return new LegStateResponse(
            leg.Id,
            leg.LegNumber,
            leg.StartingPlayerId,
            leg.WinnerPlayerId,
            turns,
            x01State,
            cricketState);
    }

    public static PlayerLegSummary BuildPlayerLegSummary(Leg leg, Match match, int playerId)
    {
        var dartsThrown = leg.Turns.Where(t => t.PlayerId == playerId).Sum(t => t.Throws.Count);
        var scored = leg.Turns.Where(t => t.PlayerId == playerId && !t.WasBust).Sum(t => t.TotalScored);
        var average = dartsThrown == 0 ? 0 : Math.Round(scored / (double)dartsThrown * 3, 2);

        int? remaining = null;
        int? cricketPoints = null;

        if (match.Mode == MatchMode.X01)
        {
            remaining = match.TargetScore - scored;
        }
        else if (match.Mode == MatchMode.Cricket)
        {
            cricketPoints = leg.CricketStates.FirstOrDefault(c => c.PlayerId == playerId)?.Points ?? 0;
        }

        return new PlayerLegSummary(playerId, average, remaining, cricketPoints);
    }

    private static X01PlayerStateResponse BuildX01State(Leg leg, int targetScore, int playerId)
    {
        var scored = leg.Turns.Where(t => t.PlayerId == playerId && !t.WasBust).Sum(t => t.TotalScored);
        var dartsThrown = leg.Turns.Where(t => t.PlayerId == playerId).Sum(t => t.Throws.Count);
        var average = dartsThrown == 0 ? 0 : Math.Round(scored / (double)dartsThrown * 3, 2);
        return new X01PlayerStateResponse(playerId, targetScore - scored, average);
    }

    private static CricketPlayerStateResponse BuildCricketState(Leg leg, int playerId)
    {
        var state = leg.CricketStates.FirstOrDefault(c => c.PlayerId == playerId)
            ?? new CricketState { PlayerId = playerId };

        return new CricketPlayerStateResponse(
            playerId,
            state.N15,
            state.N16,
            state.N17,
            state.N18,
            state.N19,
            state.N20,
            state.BullMarks,
            state.Points);
    }
}
