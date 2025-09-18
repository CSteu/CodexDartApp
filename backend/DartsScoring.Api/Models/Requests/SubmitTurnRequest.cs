using System.ComponentModel.DataAnnotations;

namespace DartsScoring.Api.Models.Requests;

public class SubmitTurnRequest
{
    [MinLength(1)]
    [MaxLength(3)]
    public List<ThrowRequest> Throws { get; set; } = new();
}

public class ThrowRequest
{
    [Range(1, 3)]
    public int Multiplier { get; set; }

    [Range(1, 60)]
    public int Segment { get; set; }
}
