using DartsScoring.Api.Models.Requests;
using DartsScoring.Api.Models.Responses;

namespace DartsScoring.Api.Services;

public interface IMatchService
{
    Task<IReadOnlyList<PlayerResponse>> GetPlayersAsync(CancellationToken cancellationToken);
    Task<MatchDetailResponse> CreateMatchAsync(CreateMatchRequest request, CancellationToken cancellationToken);
    Task<MatchDetailResponse?> GetMatchAsync(int matchId, CancellationToken cancellationToken);
}
