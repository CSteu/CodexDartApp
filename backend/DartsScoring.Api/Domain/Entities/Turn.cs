namespace DartsScoring.Api.Domain.Entities;

public class Turn
{
    public int Id { get; set; }
    public int LegId { get; set; }
    public int PlayerId { get; set; }
    public int TurnNumber { get; set; }
    public int TotalScored { get; set; }
    public bool WasBust { get; set; }

    public Leg Leg { get; set; } = null!;
    public Player Player { get; set; } = null!;
    public ICollection<Throw> Throws { get; set; } = new List<Throw>();
}
