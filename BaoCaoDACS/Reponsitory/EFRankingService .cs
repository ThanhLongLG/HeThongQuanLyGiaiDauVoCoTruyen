using BaoCaoDACS.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

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

            var ownsTransaction = _context.Database.CurrentTransaction == null;
            var transaction = ownsTransaction
                ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable)
                : null;

            try
            {
                // Chỉ request đầu tiên được quyền cập nhật Elo cho trận này.
                // ExecuteUpdate tạo một thao tác nguyên tử ở DB nên vẫn an toàn khi
                // hai request chấm điểm cùng đến một lúc.
                var claimed = await _context.match
                    .Where(m => m.MatchId == matchId && !m.EloProcessed)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(m => m.EloProcessed, true));

                if (claimed == 0)
                {
                    throw new InvalidOperationException(
                        $"Elo của trận {matchId} đã được xử lý.");
                }

                var rWin = await GetOrCreateAsync(pWin.UserId, tournamentId);
                var rLose = await GetOrCreateAsync(pLose.UserId, tournamentId);

                UpdateElo(rWin, rLose, aWin: true);

                await _context.SaveChangesAsync();

                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }
            }
            catch
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }

                throw;
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
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

        // Gợi ý đối thủ với hard filters theo trận đích.
        public async Task<List<OpponentSuggestionDto>> SuggestOpponentsAsync(
            string userId,
            int tournamentId,
            string matchId,
            int take = 10)
        {
            const int maxAgeDifference = 5;
            const int scheduleConflictMinutes = 60;
            const float weightClassWidth = 5f;

            var targetMatch = await _context.match
                .AsNoTracking()
                .FirstOrDefaultAsync(m =>
                    m.MatchId == matchId &&
                    m.TournamentID == tournamentId);

            var meP = await _context.Participants
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.UserId == userId &&
                    p.TournamentID == tournamentId);

            if (targetMatch == null ||
                meP?.CanNang == null ||
                meP.tuoi == null ||
                !TryGetWeightClassLimit(targetMatch.Hangcan, out var weightClassLimit))
            {
                return new List<OpponentSuggestionDto>();
            }

            var minimumWeight = weightClassLimit - weightClassWidth;
            if (!IsInWeightClass(meP.CanNang.Value, minimumWeight, weightClassLimit))
            {
                return new List<OpponentSuggestionDto>();
            }

            var scheduleStart = targetMatch.Date.AddMinutes(-scheduleConflictMinutes);
            var scheduleEnd = targetMatch.Date.AddMinutes(scheduleConflictMinutes);

            var unavailableParticipantIds = await _context.socre
                .AsNoTracking()
                .Where(s =>
                    s.MatchId != matchId &&
                    s.match.TournamentID == tournamentId &&
                    s.match.Date >= scheduleStart &&
                    s.match.Date <= scheduleEnd)
                .Select(s => s.ParticipantId)
                .Distinct()
                .ToListAsync();

            var myOtherMatchIds = _context.socre
                .AsNoTracking()
                .Where(s =>
                    s.ParticipantId == meP.ParticipantID &&
                    s.MatchId != matchId &&
                    s.match.TournamentID == tournamentId)
                .Select(s => s.MatchId);

            var priorMeetings = await _context.socre
                .AsNoTracking()
                .Where(s =>
                    s.ParticipantId != meP.ParticipantID &&
                    myOtherMatchIds.Contains(s.MatchId))
                .GroupBy(s => s.ParticipantId)
                .Select(g => new
                {
                    ParticipantId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(x => x.ParticipantId, x => x.Count);

            var meR = await GetOrCreateAsync(userId, tournamentId);
            var myRating = meR.Rating;
            var myWeight = meP.CanNang.Value;
            var myHeight = meP.ChieuCao ?? 0f;
            var myAge = meP.tuoi.Value;

            var opponents = await (
                from p in _context.Participants.AsNoTracking()
                where p.TournamentID == tournamentId
                      && p.UserId != null
                      && p.UserId != userId
                      && p.CanNang != null
                      && p.CanNang > minimumWeight
                      && p.CanNang <= weightClassLimit
                      && p.tuoi != null
                      && Math.Abs(p.tuoi.Value - myAge) <= maxAgeDifference
                      && !unavailableParticipantIds.Contains(p.ParticipantID)
                join r in _context.TournamentRankings.AsNoTracking()
                        .Where(x => x.TournamentId == tournamentId)
                    on p.UserId equals r.UserId into participantRankings
                from r in participantRankings.DefaultIfEmpty()
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
                    Losses = r != null ? r.Losses : 0
                })
                .ToListAsync();

            const double wElo = 0.50;
            const double wWeight = 0.25;
            const double wHeight = 0.15;
            const double wAge = 0.10;

            static double ScoreByDiff(double diff, double scale)
                => 1.0 / (1.0 + (diff / scale));

            var ranked = opponents
                .Select(o =>
                {
                    var eloDiff = Math.Abs(o.Rating - myRating);
                    var weightDiff = Math.Abs(o.CanNang!.Value - myWeight);
                    var heightDiff = Math.Abs((o.ChieuCao ?? 0f) - myHeight);
                    var ageDiff = Math.Abs(o.tuoi!.Value - myAge);
                    var meetingCount = priorMeetings.GetValueOrDefault(o.ParticipantID);
                    var sameClub = string.Equals(
                        o.Club?.Trim(),
                        meP.Club?.Trim(),
                        StringComparison.OrdinalIgnoreCase);

                    var baseScore =
                        wElo * ScoreByDiff(eloDiff, 200) +
                        wWeight * ScoreByDiff(weightDiff, 6) +
                        wHeight * ScoreByDiff(heightDiff, 10) +
                        wAge * ScoreByDiff(ageDiff, 5);

                    var rematchFactor = 1.0 / (1.0 + meetingCount);
                    var clubFactor = sameClub ? 0.65 : 1.0;
                    var finalScore = baseScore * rematchFactor * clubFactor;
                    var avoidancePenalty = meetingCount * 2 + (sameClub ? 1 : 0);

                    var notes = new List<string>
                    {
                        $"EloΔ={eloDiff:0}",
                        $"KgΔ={weightDiff:0.0}",
                        $"CmΔ={heightDiff:0.0}",
                        $"AgeΔ={ageDiff:0}"
                    };

                    if (meetingCount > 0)
                    {
                        notes.Add($"đã gặp {meetingCount} lần");
                    }

                    if (sameClub)
                    {
                        notes.Add("cùng CLB");
                    }

                    return new
                    {
                        AvoidancePenalty = avoidancePenalty,
                        Suggestion = new OpponentSuggestionDto
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
                            Reason = string.Join(", ", notes)
                        }
                    };
                })
                .OrderBy(x => x.AvoidancePenalty)
                .ThenByDescending(x => x.Suggestion.MatchScore)
                .ThenBy(x => x.Suggestion.ParticipantId, StringComparer.Ordinal)
                .Take(take)
                .Select(x => x.Suggestion)
                .ToList();

            return ranked;
        }

        private static bool TryGetWeightClassLimit(string? weightClass, out float limit)
        {
            limit = 0;
            var match = Regex.Match(weightClass ?? string.Empty, @"\d+(?:[.,]\d+)?");
            return match.Success &&
                   float.TryParse(
                       match.Value.Replace(',', '.'),
                       NumberStyles.Float,
                       CultureInfo.InvariantCulture,
                       out limit);
        }

        private static bool IsInWeightClass(float weight, float minimumWeight, float maximumWeight)
            => weight > minimumWeight && weight <= maximumWeight;


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
                .ThenBy(m => m.MatchId)
                .ToListAsync();

            foreach (var match in matches)
            {
                match.EloProcessed = false;
            }

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
                match.EloProcessed = true;
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
