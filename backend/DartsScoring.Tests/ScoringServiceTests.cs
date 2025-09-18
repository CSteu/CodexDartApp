using DartsScoring.Api.Data;
using DartsScoring.Api.Domain.Entities;
using DartsScoring.Api.Domain.Enums;
using DartsScoring.Api.Models.Requests;
using DartsScoring.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace DartsScoring.Tests;

public class ScoringServiceTests
{
    [Fact]
    public async Task X01_DoubleOut_InvalidFinish_BustsTurn()
    {
        var options = new DbContextOptionsBuilder<DartsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new DartsContext(options);
        var players = CreatePlayers();
        context.Players.AddRange(players);

        var match = new Match
        {
            Id = 1,
            Mode = MatchMode.X01,
            TargetScore = 25,
            DoubleOut = true,
            StartedAt = DateTime.UtcNow,
            Status = MatchStatus.InProgress
        };

        match.Players = CreateMatchPlayers(match, players);
        var leg = new Leg
        {
            Id = 1,
            Match = match,
            LegNumber = 1,
            StartingPlayerId = players[0].Id
        };
        match.Legs = new List<Leg> { leg };

        context.Matches.Add(match);
        await context.SaveChangesAsync();

        var service = new ScoringService(context);
        var request = new SubmitTurnRequest
        {
            Throws = new List<ThrowRequest>
            {
                new() { Multiplier = 1, Segment = 25 }
            }
        };

        var state = await service.RecordTurnAsync(leg.Id, request, CancellationToken.None);

        var playerState = state.X01State!.First(s => s.PlayerId == players[0].Id);
        Assert.Equal(25, playerState.Remaining);
        Assert.True(state.Turns.Last().WasBust);
        Assert.False(state.Turns.Last().TotalScored > 0);
        Assert.Null(state.WinnerPlayerId);
    }

    [Fact]
    public async Task Cricket_ClosedNumber_ScoresWhenOpponentOpen()
    {
        var options = new DbContextOptionsBuilder<DartsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new DartsContext(options);
        var players = CreatePlayers();
        context.Players.AddRange(players);

        var match = new Match
        {
            Id = 2,
            Mode = MatchMode.Cricket,
            TargetScore = 0,
            DoubleOut = false,
            StartedAt = DateTime.UtcNow,
            Status = MatchStatus.InProgress
        };

        match.Players = CreateMatchPlayers(match, players);
        var leg = new Leg
        {
            Id = 2,
            Match = match,
            LegNumber = 1,
            StartingPlayerId = players[0].Id,
            CricketStates = new List<CricketState>
            {
                new() { PlayerId = players[0].Id },
                new() { PlayerId = players[1].Id }
            }
        };
        match.Legs = new List<Leg> { leg };

        context.Matches.Add(match);
        await context.SaveChangesAsync();

        var service = new ScoringService(context);

        // Player 1 closes 20s
        await service.RecordTurnAsync(leg.Id, new SubmitTurnRequest
        {
            Throws = new List<ThrowRequest> { new() { Multiplier = 3, Segment = 20 } }
        }, CancellationToken.None);

        // Player 2 plays elsewhere
        await service.RecordTurnAsync(leg.Id, new SubmitTurnRequest
        {
            Throws = new List<ThrowRequest> { new() { Multiplier = 3, Segment = 19 } }
        }, CancellationToken.None);

        // Player 1 scores on 20s
        var finalState = await service.RecordTurnAsync(leg.Id, new SubmitTurnRequest
        {
            Throws = new List<ThrowRequest> { new() { Multiplier = 2, Segment = 20 } }
        }, CancellationToken.None);

        var cricketState = finalState.CricketState!.First(s => s.PlayerId == players[0].Id);
        Assert.Equal(3, cricketState.N20);
        Assert.Equal(40, cricketState.Points);
        Assert.Equal(40, finalState.Turns.Last().TotalScored);
    }

    [Fact]
    public async Task Cricket_InnerBull_CountsAsTwoMarksAndAwardsFiftyPoints()
    {
        var options = new DbContextOptionsBuilder<DartsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new DartsContext(options);
        var players = CreatePlayers();
        context.Players.AddRange(players);

        var match = new Match
        {
            Id = 3,
            Mode = MatchMode.Cricket,
            TargetScore = 0,
            DoubleOut = false,
            StartedAt = DateTime.UtcNow,
            Status = MatchStatus.InProgress
        };

        match.Players = CreateMatchPlayers(match, players);
        var leg = new Leg
        {
            Id = 3,
            Match = match,
            LegNumber = 1,
            StartingPlayerId = players[0].Id,
            CricketStates = new List<CricketState>
            {
                new() { PlayerId = players[0].Id },
                new() { PlayerId = players[1].Id }
            }
        };

        match.Legs = new List<Leg> { leg };

        context.Matches.Add(match);
        await context.SaveChangesAsync();

        var service = new ScoringService(context);

        // Player 1 collects two marks on bull
        await service.RecordTurnAsync(leg.Id, new SubmitTurnRequest
        {
            Throws = new List<ThrowRequest> { new() { Multiplier = 2, Segment = 25 } }
        }, CancellationToken.None);

        // Player 2 takes their turn
        await service.RecordTurnAsync(leg.Id, new SubmitTurnRequest
        {
            Throws = new List<ThrowRequest> { new() { Multiplier = 3, Segment = 20 } }
        }, CancellationToken.None);

        // Player 1 closes bull with an outer bull
        await service.RecordTurnAsync(leg.Id, new SubmitTurnRequest
        {
            Throws = new List<ThrowRequest> { new() { Multiplier = 1, Segment = 25 } }
        }, CancellationToken.None);

        // Player 2 throws elsewhere again
        await service.RecordTurnAsync(leg.Id, new SubmitTurnRequest
        {
            Throws = new List<ThrowRequest> { new() { Multiplier = 3, Segment = 19 } }
        }, CancellationToken.None);

        // Player 1 hits inner bull for points
        var state = await service.RecordTurnAsync(leg.Id, new SubmitTurnRequest
        {
            Throws = new List<ThrowRequest> { new() { Multiplier = 1, Segment = 50 } }
        }, CancellationToken.None);

        var cricketState = state.CricketState!.First(s => s.PlayerId == players[0].Id);
        Assert.Equal(3, cricketState.BullMarks);
        Assert.Equal(50, cricketState.Points);
        Assert.Equal(50, state.Turns.Last().TotalScored);
    }

    private static Player[] CreatePlayers() =>
    [
        new Player { Id = 1, DisplayName = "Tester A" },
        new Player { Id = 2, DisplayName = "Tester B" }
    ];

    private static List<MatchPlayer> CreateMatchPlayers(Match match, Player[] players) =>
    [
        new MatchPlayer { Match = match, Player = players[0], PlayerId = players[0].Id, Order = 0 },
        new MatchPlayer { Match = match, Player = players[1], PlayerId = players[1].Id, Order = 1 }
    ];
}
