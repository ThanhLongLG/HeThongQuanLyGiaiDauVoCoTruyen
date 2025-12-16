using BaoCaoDACS.Models;
using Microsoft.EntityFrameworkCore;

namespace BaoCaoDACS.Reponsitory
{
    public class EFRankingService : IRankingService
    {
        private readonly AppDbContext _context;

        public EFRankingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TournamentRanking> GetOrCreateAsync(string userId, int tournamentId)
        {
            var r = await _context.TournamentRankings
                .FirstOrDefaultAsync(x => x.UserId == userId && x.TournamentId == tournamentId);

            if (r != null) return r;

            r = new TournamentRanking
            {
                UserId = userId,
                TournamentId = tournamentId,
                Rating = 1000f,
                Tier = CalcTier(1000f),
                UpdatedAt = DateTime.UtcNow
            };

            _context.TournamentRankings.Add(r);
            await _context.SaveChangesAsync();
            return r;
        }

        public async Task<TournamentRanking> CreateAsync(string userId, int tournamentId, float rating = 1000f)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId không hợp lệ");

            // tránh tạo trùng (UserId, TournamentId)
            var exists = await _context.TournamentRankings
                .AnyAsync(r => r.UserId == userId && r.TournamentId == tournamentId);

            if (exists)
                throw new InvalidOperationException("Ranking đã tồn tại cho user trong giải này.");

            var ranking = new TournamentRanking
            {
                UserId = userId,
                TournamentId = tournamentId,
                Rating = rating,
                Tier = CalcTier(rating),
                MatchesPlayed = 0,
                Wins = 0,
                Losses = 0,
                UpdatedAt = DateTime.UtcNow
            };

            _context.TournamentRankings.Add(ranking);
            await _context.SaveChangesAsync();

            return ranking;
        }

        public async Task<bool> DeleteAsync(string userId, int tournamentId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId không hợp lệ");

            var ranking = await _context.TournamentRankings
                .FirstOrDefaultAsync(r => r.UserId == userId && r.TournamentId == tournamentId);

            if (ranking == null) return false;

            _context.TournamentRankings.Remove(ranking);
            await _context.SaveChangesAsync();

            return true;
        }


        public async Task UpdateAfterMatchAsync(string matchId)
        {
            // Match cần có 2 score, cả 2 có Kq khác null, winner Kq=1, loser Kq=0
            var match = await _context.match
                .Include(m => m.Socre)
                    .ThenInclude(s => s.participant)
                .FirstOrDefaultAsync(m => m.MatchId == matchId);

            if (match == null) return;
            if (match.TournamentID == null) return;

            var scores = match.Socre?.ToList() ?? new List<Socre>();
            if (scores.Count != 2) return;
            if (scores.Any(s => s.Kq == null)) return;

            var winnerScore = scores.FirstOrDefault(s => s.Kq == 1);
            var loserScore = scores.FirstOrDefault(s => s.Kq == 0);
            if (winnerScore == null || loserScore == null) return;

            var pWin = winnerScore.participant;
            var pLose = loserScore.participant;

            if (pWin?.UserId == null || pLose?.UserId == null) return;

            int tournamentId = match.TournamentID.Value;

            var rWin = await GetOrCreateAsync(pWin.UserId, tournamentId);
            var rLose = await GetOrCreateAsync(pLose.UserId, tournamentId);

            UpdateElo(rWin, rLose, aWin: true);

            await _context.SaveChangesAsync();
        }

        public async Task<List<LeaderboardRowDto>> GetLeaderboardAsync(int tournamentId, int take = 50)
        {
            return await _context.TournamentRankings
                .Where(r => r.TournamentId == tournamentId)
                .OrderByDescending(r => r.Rating)
                .Take(take)
                .Select(r => new LeaderboardRowDto
                {
                    UserId = r.UserId,
                    Fullname = r.User.Fullname,
                    Rating = r.Rating,
                    Tier = r.Tier,
                    Wins = r.Wins,
                    Losses = r.Losses,
                    MatchesPlayed = r.MatchesPlayed
                })
                .ToListAsync();
        }

        //goi y nguoi thi dau
        public async Task<List<OpponentSuggestionDto>> SuggestOpponentsAsync(
     string userId, int tournamentId, int take = 10)
        {
            // 1) Lấy participant của mình trong giải
            var meP = await _context.Participants
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId && p.TournamentID == tournamentId);

            if (meP == null) return new List<OpponentSuggestionDto>();

            // 2) Lấy ranking của mình (nếu chưa có thì tạo)
            var meR = await GetOrCreateAsync(userId, tournamentId);

            float myRating = meR.Rating;
            float myWeight = meP.CanNang ?? 0f;
            float myHeight = meP.ChieuCao ?? 0f;
            int myAge = meP.tuoi ?? 0;

            // 3) Lấy toàn bộ đối thủ trong giải (khác user)
            // LEFT JOIN ranking: ai chưa có ranking thì rating=1000
            var opponents = await (
                from p in _context.Participants.AsNoTracking()
                where p.TournamentID == tournamentId
                      && p.UserId != null
                      && p.UserId != userId
                join r in _context.TournamentRankings.AsNoTracking()
                      .Where(x => x.TournamentId == tournamentId)
                      on p.UserId equals r.UserId into pr
                from r in pr.DefaultIfEmpty()
                select new
                {
                    p.ParticipantID,
                    p.FullName,
                    p.Club,
                    p.CanNang,
                    p.ChieuCao,
                    p.tuoi,
                    Rating = r != null ? r.Rating : 1000f,
                    Tier = r != null ? r.Tier : CalcTier(1000f),
                    Wins = r != null ? r.Wins : 0,
                    Losses = r != null ? r.Losses : 0,
                }
            ).ToListAsync();

            if (!opponents.Any()) return new List<OpponentSuggestionDto>();

            // 4) Tính điểm phù hợp (Score)
            // Bạn có thể chỉnh trọng số ở đây
            double wElo = 0.50;
            double wWeight = 0.25;
            double wHeight = 0.15;
            double wAge = 0.10;

            double ScoreByDiff(double diff, double scale)
            {
                // diff càng nhỏ -> score càng gần 1
                // scale càng lớn -> cho phép lệch nhiều hơn
                return 1.0 / (1.0 + (diff / scale));
            }

            var ranked = opponents.Select(o =>
            {
                double eloDiff = Math.Abs(o.Rating - myRating);
                double weightDiff = Math.Abs((o.CanNang ?? 0f) - myWeight);
                double heightDiff = Math.Abs((o.ChieuCao ?? 0f) - myHeight);
                double ageDiff = Math.Abs((o.tuoi ?? 0) - myAge);

                // scale: bạn chỉnh theo “thực tế võ”
                // Elo lệch 200 vẫn ok -> scale 200
                // Cân lệch 6kg bắt đầu khó -> scale 6
                // Cao lệch 10cm -> scale 10
                // Tuổi lệch 5 -> scale 5
                double sElo = ScoreByDiff(eloDiff, 200);
                double sWeight = ScoreByDiff(weightDiff, 6);
                double sHeight = ScoreByDiff(heightDiff, 10);
                double sAge = ScoreByDiff(ageDiff, 5);

                double finalScore =
                    wElo * sElo +
                    wWeight * sWeight +
                    wHeight * sHeight +
                    wAge * sAge;

                string reason = $"EloΔ={eloDiff:0}, KgΔ={weightDiff:0.0}, CmΔ={heightDiff:0.0}, AgeΔ={ageDiff:0}";

                return new OpponentSuggestionDto
                {
                    ParticipantId = o.ParticipantID,
                    FullName = o.FullName,
                    Club = o.Club,
                    CanNang = o.CanNang,
                    ChieuCao = o.ChieuCao,
                    Tuoi = o.tuoi,
                    Rating = o.Rating,
                    Tier = o.Tier,
                    Wins = o.Wins,
                    Losses = o.Losses,
                    MatchScore = finalScore,
                    Reason = reason
                };
            })
            .OrderByDescending(x => x.MatchScore)
            .Take(take)
            .ToList();

            return ranked;
        }


        // ===== Elo  =====
        private static float Expected(float ra, float rb)
            => 1f / (1f + (float)Math.Pow(10, (rb - ra) / 400f));

        private static int CalcTier(float rating)
        {
            if (rating < 900) return 0;     // Bronze
            if (rating < 1100) return 1;    // Silver
            if (rating < 1300) return 2;    // Gold
            return 3;                       // Platinum
        }

        public async Task RebuildTournamentAsync(int tournamentId)
        {
            // 1️⃣ Lấy toàn bộ ranking của giải
            var rankings = await _context.TournamentRankings
                .Where(r => r.TournamentId == tournamentId)
                .ToListAsync();

            // Reset ranking
            foreach (var r in rankings)
            {
                r.Rating = 1000f;
                r.Tier = CalcTier(1000f);
                r.MatchesPlayed = 0;
                r.Wins = 0;
                r.Losses = 0;
                r.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // 2️⃣ Lấy các trận đã có kết quả hợp lệ
            var matches = await _context.match
                .Include(m => m.Socre)
                    .ThenInclude(s => s.participant)
                .Where(m => m.TournamentID == tournamentId)
                .OrderBy(m => m.Date)
                .ToListAsync();

            foreach (var match in matches)
            {
                var scores = match.Socre?.ToList() ?? new List<Socre>();

                // chỉ xử lý trận có đủ 2 score và đã chấm Kq
                if (scores.Count != 2 || scores.Any(s => s.Kq == null))
                    continue;

                var winnerScore = scores.FirstOrDefault(s => s.Kq == 1);
                var loserScore = scores.FirstOrDefault(s => s.Kq == 0);

                if (winnerScore == null || loserScore == null)
                    continue;

                var pWin = winnerScore.participant;
                var pLose = loserScore.participant;

                if (pWin?.UserId == null || pLose?.UserId == null)
                    continue;

                // 3️⃣ Lấy ranking của 2 user
                var rWin = await GetOrCreateAsync(pWin.UserId, tournamentId);
                var rLose = await GetOrCreateAsync(pLose.UserId, tournamentId);

                // 4️⃣ Update Elo
                UpdateElo(rWin, rLose, aWin: true);
            }

            await _context.SaveChangesAsync();
        }


        private static void UpdateElo(TournamentRanking a, TournamentRanking b, bool aWin)
        {
            float kA = a.MatchesPlayed < 10 ? 40 : 20;
            float kB = b.MatchesPlayed < 10 ? 40 : 20;

            float ea = Expected(a.Rating, b.Rating);
            float eb = Expected(b.Rating, a.Rating);

            float sa = aWin ? 1f : 0f;
            float sb = 1f - sa;

            a.Rating = a.Rating + kA * (sa - ea);
            b.Rating = b.Rating + kB * (sb - eb);

            a.MatchesPlayed++; b.MatchesPlayed++;
            if (aWin) { a.Wins++; b.Losses++; } else { a.Losses++; b.Wins++; }

            a.Tier = CalcTier(a.Rating);
            b.Tier = CalcTier(b.Rating);

            a.UpdatedAt = DateTime.UtcNow;
            b.UpdatedAt = DateTime.UtcNow;
        }
    }
}
