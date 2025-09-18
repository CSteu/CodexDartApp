using DartsScoring.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DartsScoring.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TurnsController(IScoringService scoringService) : ControllerBase
{
    [HttpPost("{turnId:int}/undo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UndoTurn(int turnId, CancellationToken cancellationToken)
    {
        await scoringService.UndoTurnAsync(turnId, cancellationToken);
        return NoContent();
    }
}
