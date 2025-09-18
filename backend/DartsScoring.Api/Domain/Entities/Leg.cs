namespace DartsScoring.Api.Domain.Entities;

public class Leg
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public int LegNumber { get; set; }
    public int StartingPlayerId { get; set; }
    public int? WinnerPlayerId { get; set; }
    public DateTime? FinishedAt { get; set; }

    public Match Match { get; set; } = null!;
    public Player StartingPlayer { get; set; } = null!;
    public Player? WinnerPlayer { get; set; }

    public ICollection<Turn> Turns { get; set; } = new List<Turn>();
    public ICollection<CricketState> CricketStates { get; set; } = new List<CricketState>();
}
