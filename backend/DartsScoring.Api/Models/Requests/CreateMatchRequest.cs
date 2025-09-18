using System.ComponentModel.DataAnnotations;
using DartsScoring.Api.Domain.Enums;

namespace DartsScoring.Api.Models.Requests;

public class CreateMatchRequest
{
    [Required]
    public MatchMode Mode { get; set; } = MatchMode.X01;

    [Range(1, 10001)]
    public int TargetScore { get; set; } = 501;

    public bool DoubleOut { get; set; } = true;

    [Required]
    public required List<int> PlayerIds { get; set; }
        = new();

    public int? StartingPlayerId { get; set; }
        = null;
}
