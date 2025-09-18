namespace DartsScoring.Api.Domain.Entities;

public class Player
{
    public int Id { get; set; }
    public required string DisplayName { get; set; }

    public ICollection<MatchPlayer> Matches { get; set; } = new List<MatchPlayer>();
    public ICollection<Leg> StartingLegs { get; set; } = new List<Leg>();
    public ICollection<Leg> WonLegs { get; set; } = new List<Leg>();
    public ICollection<Turn> Turns { get; set; } = new List<Turn>();
    public ICollection<CricketState> CricketStates { get; set; } = new List<CricketState>();
}
