using DartsScoring.Api.Models.Requests;
using DartsScoring.Api.Models.Responses;
using DartsScoring.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DartsScoring.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchesController(IMatchService matchService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<MatchDetailResponse>> CreateMatch([FromBody] CreateMatchRequest request, CancellationToken cancellationToken)
    {
        var match = await matchService.CreateMatchAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetMatch), new { id = match.Id }, match);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatchDetailResponse>> GetMatch(int id, CancellationToken cancellationToken)
    {
        var match = await matchService.GetMatchAsync(id, cancellationToken);
        if (match is null)
        {
            return NotFound();
        }

        return Ok(match);
    }
}
