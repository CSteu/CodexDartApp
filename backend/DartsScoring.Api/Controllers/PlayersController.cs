using DartsScoring.Api.Models.Responses;
using DartsScoring.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DartsScoring.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController(IMatchService matchService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PlayerResponse>>> GetPlayers(CancellationToken cancellationToken)
    {
        var players = await matchService.GetPlayersAsync(cancellationToken);
        return Ok(players);
    }
}
