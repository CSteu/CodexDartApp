using DartsScoring.Api.Data;
using DartsScoring.Api.Domain.Entities;
using DartsScoring.Api.Domain.Enums;
using DartsScoring.Api.Mapping;
using DartsScoring.Api.Models.Requests;
using DartsScoring.Api.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace DartsScoring.Api.Services;

public class MatchService(DartsContext context) : IMatchService
{
    public async Task<IReadOnlyList<PlayerResponse>> GetPlayersAsync(CancellationToken cancellationToken)
    {
        return await context.Players
            .OrderBy(p => p.DisplayName)
            .Select(p => new PlayerResponse(p.Id, p.DisplayName))
            .ToListAsync(cancellationToken);
    }

    public async Task<MatchDetailResponse> CreateMatchAsync(CreateMatchRequest request, CancellationToken cancellationToken)
    {
        var playerIds = request.PlayerIds.Distinct().ToList();
        if (playerIds.Count != 2)
        {
            throw new InvalidOperationException("Exactly two players are required for the demo match.");
        }

        var players = await context.Players
            .Where(p => playerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        if (players.Count != 2)
        {
            throw new InvalidOperationException("One or more players could not be found.");
        }

        var targetScore = request.Mode == MatchMode.X01 ? request.TargetScore : 0;
        var doubleOut = request.Mode == MatchMode.X01 && request.DoubleOut;
        var startingPlayerId = request.StartingPlayerId.HasValue && playerIds.Contains(request.StartingPlayerId.Value)
            ? request.StartingPlayerId.Value
            : playerIds[0];

        var leg = new Leg
        {
            LegNumber = 1,
            StartingPlayerId = startingPlayerId
        };

        var match = new Match
        {
            Mode = request.Mode,
            TargetScore = targetScore,
            DoubleOut = doubleOut,
            StartedAt = DateTime.UtcNow,
            Status = MatchStatus.InProgress,
            Players = new List<MatchPlayer>
            {
                new() { PlayerId = playerIds[0], Order = 0 },
                new() { PlayerId = playerIds[1], Order = 1 }
            },
            Legs = new List<Leg> { leg }
        };

        if (match.Mode == MatchMode.Cricket)
        {
            leg.CricketStates = match.Players
                .Select(mp => new CricketState
                {
                    PlayerId = mp.PlayerId
                })
                .ToList();
        }

        context.Matches.Add(match);
        await context.SaveChangesAsync(cancellationToken);

        return (await GetMatchAsync(match.Id, cancellationToken))!;
    }

    public async Task<MatchDetailResponse?> GetMatchAsync(int matchId, CancellationToken cancellationToken)
    {
        var match = await context.Matches
            .Include(m => m.Players)
                .ThenInclude(mp => mp.Player)
            .Include(m => m.Legs)
                .ThenInclude(l => l.Turns)
                    .ThenInclude(t => t.Throws)
            .Include(m => m.Legs)
                .ThenInclude(l => l.CricketStates)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == matchId, cancellationToken);

        return match?.ToDetailResponse();
    }
}
