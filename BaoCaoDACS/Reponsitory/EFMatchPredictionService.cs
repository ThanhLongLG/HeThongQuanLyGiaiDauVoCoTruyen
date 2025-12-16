using System.Drawing.Printing;
using BaoCaoDACS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;

namespace BaoCaoDACS.Reponsitory
{
    public class EFMatchPredictionService : IMatchPredictionService
    {
        private readonly MLContext _mlContext;
        private readonly ITransformer _model;
        private readonly AppDbContext _context;
        const float DEFAULT_RATING = 1000f;
        public EFMatchPredictionService(AppDbContext context)
        {
            _context = context; // Gán _context từ DI
            _mlContext = new MLContext();

            // Kiểm tra file model tồn tại để tránh lỗi
            var modelPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Models/match_predictor.zip");
            if (!File.Exists(modelPath))
            {
                throw new FileNotFoundException($"Model file not found: {modelPath}. Please train and save the model first.");
            }

            using var fs = File.OpenRead(modelPath);
            _model = _mlContext.Model.Load(fs, out _);
        }

        public float PredictWinRate(MatchTrainingSample input)
        {
            var engine = _mlContext.Model.CreatePredictionEngine<
                MatchTrainingSample, MatchPredictionOutput>(_model);

            var result = engine.Predict(input);
            return result.Probability * 100f;
        }

        public float GetRatingBeforeMatch(string? userId, int? tournamentId, DateTime matchDate)
        {
            try
            {
                    const float DEFAULT_RATING = 1000f;
                if (string.IsNullOrWhiteSpace(userId) || tournamentId is null) return DEFAULT_RATING;

                var rating = _context.TournamentRankings
                    .Where(r => r.UserId == userId
                             && r.TournamentId == tournamentId.Value
                             && r.UpdatedAt <= matchDate)
                    .OrderByDescending(r => r.UpdatedAt)
                    .Select(r => (float?)r.Rating)
                    .FirstOrDefault();

                return rating ?? DEFAULT_RATING;

            }
            catch (Exception ex)
            {
                // Log lỗi và trả default
                Console.WriteLine($"Error in GetRatingBeforeMatch: {ex.Message}");
                return DEFAULT_RATING;
            }
        }
    }

}
