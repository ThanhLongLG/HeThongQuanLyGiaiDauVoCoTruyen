using BaoCaoDACS.Models;

namespace BaoCaoDACS.Reponsitory
{
    public interface IRankingService
    {
        Task<TournamentRanking> GetOrCreateAsync(string userId, int tournamentId);

        Task UpdateAfterMatchAsync(string matchId);
        Task<TournamentRanking> CreateAsync(string userId, int tournamentId, float rating = 1000f);
        Task<bool> DeleteAsync(string userId, int tournamentId);

        Task RebuildTournamentAsync(int tournamentId);
        Task<List<LeaderboardRowDto>> GetLeaderboardAsync(int tournamentId, int take = 50);

        Task<List<OpponentSuggestionDto>> SuggestOpponentsAsync(
            string userId,
            int tournamentId,
            string matchId,
            int take = 10);
    }

    public class LeaderboardRowDto
    {
        public string UserId { get; set; }
        public string Fullname { get; set; }
        public float Rating { get; set; }
        public int Tier { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int MatchesPlayed { get; set; }
    }

    public class OpponentSuggestionDto
    {
        public string ParticipantId { get; set; }
        public string FullName { get; set; }
        public string Club { get; set; }
        public float? CanNang { get; set; }
        public float? ChieuCao { get; set; }
        public int? Tuoi { get; set; }

        public float Rating { get; set; }
        public int Tier { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public double MatchScore { get; set; }    

        public string Reason { get; set; }
    }
}
