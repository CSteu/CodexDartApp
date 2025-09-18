using DartsScoring.Api.Models.Requests;
using DartsScoring.Api.Models.Responses;

namespace DartsScoring.Api.Services;

public interface IScoringService
{
    Task<LegStateResponse> RecordTurnAsync(int legId, SubmitTurnRequest request, CancellationToken cancellationToken);
    Task UndoTurnAsync(int turnId, CancellationToken cancellationToken);
    Task<LegStateResponse?> GetLegStateAsync(int legId, CancellationToken cancellationToken);
    Task<LegSummaryResponse?> GetLegSummaryAsync(int legId, CancellationToken cancellationToken);
}
