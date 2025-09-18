using DartsScoring.Api.Models.Requests;
using DartsScoring.Api.Models.Responses;
using DartsScoring.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DartsScoring.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LegsController(IScoringService scoringService) : ControllerBase
{
    [HttpPost("{legId:int}/turns")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<LegStateResponse>> SubmitTurn(int legId, [FromBody] SubmitTurnRequest request, CancellationToken cancellationToken)
    {
        var state = await scoringService.RecordTurnAsync(legId, request, cancellationToken);
        return Ok(state);
    }

    [HttpGet("{legId:int}/summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LegSummaryResponse>> GetSummary(int legId, CancellationToken cancellationToken)
    {
        var summary = await scoringService.GetLegSummaryAsync(legId, cancellationToken);
        if (summary is null)
        {
            return NotFound();
        }

        return Ok(summary);
    }
}
