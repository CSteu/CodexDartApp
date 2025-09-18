namespace DartsScoring.Api.Domain.Entities;

public class Throw
{
    public int Id { get; set; }
    public int TurnId { get; set; }
    public int Multiplier { get; set; }
    public int Segment { get; set; }
    public int ScoreValue { get; set; }

    public Turn Turn { get; set; } = null!;
}
