using System.Diagnostics;
using BAO_CAO.Models;
using BaoCaoDACS.Models;
using BaoCaoDACS.Models.VnPay;
using BaoCaoDACS.Reponsitory;
using BaoCaoDACS.Reponsitory.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.ML;
using System.Text.Json;

namespace BaoCaoDACS.Controllers
{

    [Route("[controller]")]
    public class PredictController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PredictionEngine<MatchTrainingSample, MatchPredictionOutput> _pred;

        public PredictController(
            AppDbContext context,
            PredictionEngine<MatchTrainingSample, MatchPredictionOutput> pred)
        {
            _context = context;
            _pred = pred;
        }

        public async Task<IActionResult> Index(int? tournamentId, string? matchId)
        {
            var vm = new PredictMatchViewModel();

            // 1. Load danh sách giải đấu
            vm.Tournaments = await _context.Tournaments
               .Select(t => new SelectListItem
               {
                   Value = t.TournamentID.ToString(),
                   Text = t.Name,
                   Selected = (tournamentId.HasValue && tournamentId.Value == t.TournamentID)
               })
               .ToListAsync();


            vm.SelectedTournamentId = tournamentId;

            // 2. Nếu đã chọn giải → load danh sách trận thuộc giải đó
            if (tournamentId.HasValue)
            {
                vm.Matches = await _context.match
                    .Where(m => m.TournamentID == tournamentId.Value)
                    .Select(m => new SelectListItem
                    {
                        Value = m.MatchId,
                        Text = m.MatchId + " - " + m.SanDau + " (" + m.Vongdau + ")",
                        Selected = (m.MatchId == matchId)
                    })
                    .ToListAsync();
            }


            // 3. Nếu đã chọn trận → lấy dữ liệu & dự đoán
            if (!string.IsNullOrEmpty(matchId))
            {
                vm.SelectedMatchId = matchId;

                var match = await _context.match
                    .Include(m => m.Socre)
                        .ThenInclude(s => s.participant)
                    .FirstOrDefaultAsync(m => m.MatchId == matchId);

                if (match != null && match.Socre.Count >= 2)
                {
                    var scores = match.Socre
                        .OrderBy(s => s.ParticipantId)   
                        .ToList();

                    if (scores.Count < 2) return View(vm);

                    var sA = scores[0];
                    var sB = scores[1];

                    var pA = sA.participant;
                    var pB = sB.participant;


                    var ratingA = GetRatingBeforeMatch(pA.UserId, match.TournamentID, match.Date);
                    var ratingB = GetRatingBeforeMatch(pB.UserId, match.TournamentID, match.Date);

                    // Map sang input cho ML.NET
                    var input = new MatchTrainingSample
                    {
                        FighterA_Weight = pA.CanNang ?? 0,
                        FighterA_Height = pA.ChieuCao ?? 0,
                        FighterA_Age = pA.tuoi ?? 0,

                        FighterB_Weight = pB.CanNang ?? 0,
                        FighterB_Height = pB.ChieuCao ?? 0,
                        FighterB_Age = pB.tuoi ?? 0,

                        FighterA_Rating = ratingA,
                        FighterB_Rating = ratingB,
                        RatingDiff = ratingA - ratingB,


                        DiffWeight = (pA.CanNang ?? 0) - (pB.CanNang ?? 0),
                        DiffHeight = (pA.ChieuCao ?? 0) - (pB.ChieuCao ?? 0),
                        DiffAge = (pA.tuoi ?? 0) - (pB.tuoi ?? 0),


                        LoaiHinhThiDauId = match.LoaiHinhThiDauId, // int 
                        HangCan = match.Hangcan!,
                        VongDau = match.Vongdau
                    };
                    Debug.WriteLine("===== PREDICT INPUT =====");
                    Debug.WriteLine($"MatchId: {match.MatchId}");
                    Debug.WriteLine($"Fighter A: {pA.FullName} | Weight={input.FighterA_Weight}, Height={input.FighterA_Height}, Age={input.FighterA_Age}");
                    Debug.WriteLine($"Fighter B: {pB.FullName} | Weight={input.FighterB_Weight}, Height={input.FighterB_Height}, Age={input.FighterB_Age}");
                    Debug.WriteLine($"LoaiHinhThiDauId: {input.LoaiHinhThiDauId}");
                    Debug.WriteLine($"HangCan: {input.HangCan}");
                    Debug.WriteLine($"VongDau: {input.VongDau}");
                    Debug.WriteLine("===== END INPUT =====");

                    var result = _pred.Predict(input);
                    Debug.WriteLine("===== PREDICT OUTPUT =====");
                    Debug.WriteLine($"PredictedLabel (A thắng?): {result.PredictedLabel}");
                    Debug.WriteLine($"Probability (A thắng): {result.Probability}");
                    Debug.WriteLine("===== END OUTPUT =====");

                    // Probability là xác suất A thắng (theo lúc train)
                    var probA = result.Probability * 100f;
                    var probB = 100f - probA;

                    vm.FighterAName = pA.FullName;
                    vm.FighterBName = pB.FullName;
                    vm.FighterAProbPercent = probA;
                    vm.FighterBProbPercent = probB;
                    vm.WinnerName = result.PredictedLabel ? pA.FullName : pB.FullName;
                }
            }

            return View(vm);
        }

        private float GetRatingBeforeMatch(string? userId, int? tournamentId, DateTime matchDate)
        {
            const float DEFAULT_RATING = 1000f;

            if (string.IsNullOrWhiteSpace(userId) || tournamentId is null)
                return DEFAULT_RATING;

            var rating = _context.TournamentRankings
                .Where(r => r.UserId == userId
                         && r.TournamentId == tournamentId.Value
                         && r.UpdatedAt <= matchDate)
                .OrderByDescending(r => r.UpdatedAt)
                .Select(r => (float?)r.Rating)
                .FirstOrDefault();

            return rating ?? DEFAULT_RATING;
        }


        // API load danh sách trận theo giải (dùng cho dropdown động)
        [HttpGet("GetMatchesByTournament")]
        public async Task<IActionResult> GetMatchesByTournament(int tournamentId)
        {
            var matches = await _context.match
                .Where(m => m.TournamentID == tournamentId)
                .Select(m => new
                {
                    id = m.MatchId,
                    text = m.MatchId + " - " + m.SanDau + " (" + m.Vongdau + ")"
                })
                .ToListAsync();

            return Json(matches);
        }
    }


}
