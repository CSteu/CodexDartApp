namespace DartsScoring.Api.Domain.Entities;

public class CricketState
{
    public int Id { get; set; }
    public int LegId { get; set; }
    public int PlayerId { get; set; }
    public int N15 { get; set; }
    public int N16 { get; set; }
    public int N17 { get; set; }
    public int N18 { get; set; }
    public int N19 { get; set; }
    public int N20 { get; set; }
    public int BullMarks { get; set; }
    public int Points { get; set; }

    public Leg Leg { get; set; } = null!;
    public Player Player { get; set; } = null!;
}
