
using BaoCaoDACS.Models;

namespace BaoCaoDACS.Reponsitory
{
    public interface IMatchPredictionService
    {
        float PredictWinRate(MatchTrainingSample input);

        float GetRatingBeforeMatch(string? userId, int? tournamentId, DateTime matchDate);
    }

}
