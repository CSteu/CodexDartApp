using DartsScoring.Api.Domain.Enums;

namespace DartsScoring.Api.Domain.Entities;

public class Match
{
    public int Id { get; set; }
    public MatchMode Mode { get; set; }
    public int TargetScore { get; set; }
    public bool DoubleOut { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public MatchStatus Status { get; set; }

    public ICollection<Leg> Legs { get; set; } = new List<Leg>();
    public ICollection<MatchPlayer> Players { get; set; } = new List<MatchPlayer>();
}
