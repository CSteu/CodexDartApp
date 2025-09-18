using DartsScoring.Api.Domain.Entities;
using DartsScoring.Api.Models.Responses;

namespace DartsScoring.Api.Mapping;

public static class MatchMappingExtensions
{
    public static MatchDetailResponse ToDetailResponse(this Match match)
    {
        var players = match.Players
            .OrderBy(p => p.Order)
            .Select(p => new PlayerResponse(p.PlayerId, p.Player.DisplayName))
            .ToList();

        var legs = match.Legs
            .OrderBy(l => l.LegNumber)
            .ThenBy(l => l.Id)
            .Select(l => l.ToLegState(match))
            .ToList();

        return new MatchDetailResponse(
            match.Id,
            match.Mode,
            match.TargetScore,
            match.DoubleOut,
            match.Status,
            match.StartedAt,
            match.FinishedAt,
            players,
            legs);
    }
}
