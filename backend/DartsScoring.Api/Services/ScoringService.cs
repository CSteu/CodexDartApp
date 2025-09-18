using DartsScoring.Api.Data;
using DartsScoring.Api.Domain.Entities;
using DartsScoring.Api.Domain.Enums;
using DartsScoring.Api.Mapping;
using DartsScoring.Api.Models.Requests;
using DartsScoring.Api.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace DartsScoring.Api.Services;

public class ScoringService(DartsContext context) : IScoringService
{
    public async Task<LegStateResponse> RecordTurnAsync(int legId, SubmitTurnRequest request, CancellationToken cancellationToken)
    {
        var leg = await LoadLegAsync(legId, cancellationToken)
            ?? throw new InvalidOperationException("Leg not found.");

        var match = leg.Match;
        var orderedPlayers = match.Players.OrderBy(p => p.Order).ToList();
        if (orderedPlayers.Count != 2)
        {
            throw new InvalidOperationException("Only two players are supported in this demo.");
        }

        if (leg.WinnerPlayerId.HasValue)
        {
            throw new InvalidOperationException("The leg has already finished.");
        }

        var activePlayerId = DetermineActivePlayer(leg, orderedPlayers);
        var turnNumber = (leg.Turns.Count == 0 ? 0 : leg.Turns.Max(t => t.TurnNumber)) + 1;

        var throwsData = request.Throws
            .Select(NormalizeThrow)
            .ToList();

        Turn turn;
        TurnOutcome outcome;

        if (match.Mode == MatchMode.X01)
        {
            outcome = ProcessX01Turn(leg, match, activePlayerId, throwsData);
        }
        else
        {
            outcome = ProcessCricketTurn(leg, match, activePlayerId, throwsData);
        }

        turn = new Turn
        {
            LegId = leg.Id,
            PlayerId = activePlayerId,
            TurnNumber = turnNumber,
            TotalScored = outcome.TotalScored,
            WasBust = outcome.WasBust,
            Throws = throwsData.Select(td => new Throw
            {
                Multiplier = td.Multiplier,
                Segment = td.Segment,
                ScoreValue = td.ScoreValue
            }).ToList()
        };

        leg.Turns.Add(turn);

        if (outcome.WinnerPlayerId.HasValue)
        {
            leg.WinnerPlayerId = outcome.WinnerPlayerId.Value;
            leg.FinishedAt = DateTime.UtcNow;
            match.FinishedAt ??= DateTime.UtcNow;
            match.Status = MatchStatus.Completed;
        }

        await context.SaveChangesAsync(cancellationToken);

        await context.Entry(leg)
            .Collection(l => l.Turns)
            .Query()
            .Include(t => t.Throws)
            .LoadAsync(cancellationToken);

        return leg.ToLegState(match);
    }

    public async Task UndoTurnAsync(int turnId, CancellationToken cancellationToken)
    {
        var turn = await context.Turns
            .Include(t => t.Throws)
            .Include(t => t.Leg)
                .ThenInclude(l => l.Match)
                    .ThenInclude(m => m.Players)
                        .ThenInclude(mp => mp.Player)
            .Include(t => t.Leg)
                .ThenInclude(l => l.CricketStates)
            .FirstOrDefaultAsync(t => t.Id == turnId, cancellationToken)
            ?? throw new InvalidOperationException("Turn not found.");

        var leg = turn.Leg;
        var match = leg.Match;

        context.Throws.RemoveRange(turn.Throws);
        context.Turns.Remove(turn);

        if (leg.WinnerPlayerId.HasValue)
        {
            leg.WinnerPlayerId = null;
            leg.FinishedAt = null;
            match.FinishedAt = null;
            match.Status = MatchStatus.InProgress;
        }

        await context.SaveChangesAsync(cancellationToken);

        if (match.Mode == MatchMode.Cricket)
        {
            await RebuildCricketStateAsync(leg.Id, cancellationToken);
        }
    }

    public async Task<LegStateResponse?> GetLegStateAsync(int legId, CancellationToken cancellationToken)
    {
        var leg = await LoadLegAsync(legId, cancellationToken);
        return leg?.ToLegState(leg.Match);
    }

    public async Task<LegSummaryResponse?> GetLegSummaryAsync(int legId, CancellationToken cancellationToken)
    {
        var leg = await LoadLegAsync(legId, cancellationToken);
        if (leg is null)
        {
            return null;
        }

        var match = leg.Match;
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

        var summaries = match.Players
            .OrderBy(p => p.Order)
            .Select(p => LegMappingExtensions.BuildPlayerLegSummary(leg, match, p.PlayerId))
            .ToList();

        return new LegSummaryResponse(leg.Id, turns, summaries);
    }

    private async Task<Leg?> LoadLegAsync(int legId, CancellationToken cancellationToken)
    {
        return await context.Legs
            .Include(l => l.Match)
                .ThenInclude(m => m.Players)
                    .ThenInclude(mp => mp.Player)
            .Include(l => l.Turns)
                .ThenInclude(t => t.Throws)
            .Include(l => l.CricketStates)
            .AsSplitQuery()
            .FirstOrDefaultAsync(l => l.Id == legId, cancellationToken);
    }

    private static int DetermineActivePlayer(Leg leg, List<MatchPlayer> players)
    {
        if (leg.Turns.Count == 0)
        {
            return players.FirstOrDefault(p => p.PlayerId == leg.StartingPlayerId)?.PlayerId
                ?? players[0].PlayerId;
        }

        var orderedTurns = leg.Turns.OrderBy(t => t.TurnNumber).ThenBy(t => t.Id).ToList();
        var lastPlayerId = orderedTurns.Last().PlayerId;
        var lastIndex = players.FindIndex(p => p.PlayerId == lastPlayerId);
        var nextIndex = (lastIndex + 1) % players.Count;
        return players[nextIndex].PlayerId;
    }

    private static ThrowData NormalizeThrow(ThrowRequest request)
    {
        if (request.Segment == 25 && request.Multiplier > 2)
        {
            throw new InvalidOperationException("Bull can only be single or double.");
        }

        if (request.Segment == 50 && request.Multiplier > 1)
        {
            throw new InvalidOperationException("Inner bull cannot have a multiplier greater than one.");
        }

        var segment = request.Segment;
        var multiplier = request.Multiplier;
        var scoreValue = segment switch
        {
            50 => 50,
            _ => multiplier * segment
        };

        return new ThrowData(multiplier, segment, scoreValue);
    }

    private TurnOutcome ProcessX01Turn(Leg leg, Match match, int playerId, IReadOnlyList<ThrowData> throwsData)
    {
        var currentScore = match.TargetScore - leg.Turns
            .Where(t => t.PlayerId == playerId && !t.WasBust)
            .Sum(t => t.TotalScored);

        var turnScore = 0;
        var wasBust = false;
        bool finished = false;

        foreach (var dart in throwsData)
        {
            var prospective = currentScore - dart.ScoreValue;
            var isDouble = IsDouble(dart);

            if (prospective < 0 || (match.DoubleOut && prospective == 1))
            {
                wasBust = true;
                break;
            }

            if (prospective == 0)
            {
                if (match.DoubleOut && !isDouble)
                {
                    wasBust = true;
                }
                else
                {
                    turnScore += dart.ScoreValue;
                    currentScore = 0;
                    finished = true;
                }
                break;
            }

            turnScore += dart.ScoreValue;
            currentScore = prospective;
        }

        if (wasBust)
        {
            turnScore = 0;
        }

        var winnerId = finished ? playerId : (int?)null;
        return new TurnOutcome(turnScore, wasBust, winnerId);
    }

    private TurnOutcome ProcessCricketTurn(Leg leg, Match match, int playerId, IReadOnlyList<ThrowData> throwsData)
    {
        if (leg.CricketStates.Count == 0)
        {
            var participants = match.Players.Select(mp => mp.PlayerId).ToList();
            foreach (var participant in participants)
            {
                leg.CricketStates.Add(new CricketState
                {
                    PlayerId = participant,
                    LegId = leg.Id
                });
            }
        }

        var playerState = leg.CricketStates.First(c => c.PlayerId == playerId);
        var opponentState = leg.CricketStates.First(c => c.PlayerId != playerId);
        var turnPoints = 0;

        foreach (var dart in throwsData)
        {
            var number = dart.Segment == 50 ? 25 : dart.Segment;
            if (!IsValidCricketNumber(number))
            {
                continue;
            }

            var marksAwarded = dart.Segment switch
            {
                50 => 2,
                25 => Math.Min(dart.Multiplier, 2),
                _ => dart.Multiplier
            };

            var (playerMarks, opponentMarks) = GetCricketMarks(playerState, opponentState, number);
            var totalMarks = playerMarks + marksAwarded;
            var scoringMarks = 0;

            if (totalMarks > 3)
            {
                scoringMarks = totalMarks - 3;
                totalMarks = 3;
            }

            if (opponentMarks >= 3)
            {
                scoringMarks = 0;
            }

            SetCricketMarks(playerState, number, totalMarks);

            if (scoringMarks > 0)
            {
                var baseValue = number == 25 ? 25 : number;
                turnPoints += scoringMarks * baseValue;
                playerState.Points += scoringMarks * baseValue;
            }
        }

        var winnerId = CheckCricketWinner(playerState, opponentState) ? playerId : (int?)null;

        return new TurnOutcome(turnPoints, false, winnerId);
    }

    private async Task RebuildCricketStateAsync(int legId, CancellationToken cancellationToken)
    {
        var leg = await context.Legs
            .Include(l => l.Match)
                .ThenInclude(m => m.Players)
            .Include(l => l.CricketStates)
            .Include(l => l.Turns)
                .ThenInclude(t => t.Throws)
            .FirstAsync(l => l.Id == legId, cancellationToken);

        if (leg.CricketStates.Count == 0)
        {
            return;
        }

        foreach (var state in leg.CricketStates)
        {
            ResetCricketState(state);
        }

        var orderedTurns = leg.Turns
            .OrderBy(t => t.TurnNumber)
            .ThenBy(t => t.Id)
            .ToList();

        foreach (var turn in orderedTurns)
        {
            var throwsData = turn.Throws
                .OrderBy(th => th.Id)
                .Select(th => new ThrowData(th.Multiplier, th.Segment, th.ScoreValue))
                .ToList();

            ProcessCricketTurn(leg, leg.Match, turn.PlayerId, throwsData);
        }

        leg.WinnerPlayerId = null;
        leg.FinishedAt = null;
        leg.Match.Status = MatchStatus.InProgress;
        leg.Match.FinishedAt = null;

        await context.SaveChangesAsync(cancellationToken);
    }

    private static void ResetCricketState(CricketState state)
    {
        state.N15 = 0;
        state.N16 = 0;
        state.N17 = 0;
        state.N18 = 0;
        state.N19 = 0;
        state.N20 = 0;
        state.BullMarks = 0;
        state.Points = 0;
    }

    private static bool CheckCricketWinner(CricketState playerState, CricketState opponentState)
    {
        return HasClosedAll(playerState) && playerState.Points >= opponentState.Points;
    }

    private static bool HasClosedAll(CricketState state)
    {
        return state.N15 >= 3 && state.N16 >= 3 && state.N17 >= 3 &&
               state.N18 >= 3 && state.N19 >= 3 && state.N20 >= 3 && state.BullMarks >= 3;
    }

    private static (int playerMarks, int opponentMarks) GetCricketMarks(CricketState player, CricketState opponent, int number)
    {
        return number switch
        {
            15 => (player.N15, opponent.N15),
            16 => (player.N16, opponent.N16),
            17 => (player.N17, opponent.N17),
            18 => (player.N18, opponent.N18),
            19 => (player.N19, opponent.N19),
            20 => (player.N20, opponent.N20),
            25 => (player.BullMarks, opponent.BullMarks),
            _ => (0, 0)
        };
    }

    private static void SetCricketMarks(CricketState player, int number, int value)
    {
        switch (number)
        {
            case 15:
                player.N15 = value;
                break;
            case 16:
                player.N16 = value;
                break;
            case 17:
                player.N17 = value;
                break;
            case 18:
                player.N18 = value;
                break;
            case 19:
                player.N19 = value;
                break;
            case 20:
                player.N20 = value;
                break;
            case 25:
                player.BullMarks = value;
                break;
        }
    }

    private static bool IsValidCricketNumber(int number)
    {
        return number is >= 15 and <= 20 or 25;
    }

    private static bool IsDouble(ThrowData dart)
    {
        if (dart.Segment == 50)
        {
            return true;
        }

        if (dart.Segment == 25)
        {
            return dart.Multiplier == 2;
        }

        return dart.Multiplier == 2;
    }

    private record ThrowData(int Multiplier, int Segment, int ScoreValue);

    private record TurnOutcome(int TotalScored, bool WasBust, int? WinnerPlayerId);
}
