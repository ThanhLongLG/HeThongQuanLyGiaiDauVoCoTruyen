using System.Drawing.Printing;
using BAO_CAO.Models;
using BaoCaoDACS.Areas.Admin.Models;
using BaoCaoDACS.Models;
using BaoCaoDACS.Reponsitory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BAO_CAO.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class NguoiDungController : Controller
    {

        private readonly INguoidungreponsitory _nguoidungreponsitory;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NguoiDungController> _logger;
        private readonly AppDbContext _context;
        private readonly IRankingService _rankingService;
        public NguoiDungController(
            INguoidungreponsitory nguoidungreponsitory, 
            ILogger<NguoiDungController> logger,
            UserManager<ApplicationUser> userManager,
            AppDbContext context,
            IRankingService rankingService)
        {
            _userManager = userManager;
            _nguoidungreponsitory= nguoidungreponsitory;
                 _logger = logger;
            _context = context;
            _rankingService= rankingService;
        }

        public async Task<IActionResult> Index(string? searchValue)
        {
            var participants = (await _nguoidungreponsitory.GetAllAsync(searchValue)).ToList();
            var userIds = participants
                .Where(p => !string.IsNullOrWhiteSpace(p.UserId))
                .Select(p => p.UserId!)
                .Distinct()
                .ToList();
            var tournamentIds = participants
                .Where(p => p.TournamentID.HasValue)
                .Select(p => p.TournamentID!.Value)
                .Distinct()
                .ToList();

            var rankings = userIds.Count == 0 || tournamentIds.Count == 0
                ? new List<TournamentRanking>()
                : await _context.TournamentRankings
                    .AsNoTracking()
                    .Where(r => userIds.Contains(r.UserId)
                        && tournamentIds.Contains(r.TournamentId))
                    .ToListAsync();

            ViewBag.CurrentRankings = participants
                .Where(p => !string.IsNullOrWhiteSpace(p.UserId) && p.TournamentID.HasValue)
                .Select(p => new
                {
                    p.ParticipantID,
                    Ranking = rankings.FirstOrDefault(r =>
                        r.UserId == p.UserId
                        && r.TournamentId == p.TournamentID!.Value)
                })
                .Where(x => x.Ranking != null)
                .ToDictionary(x => x.ParticipantID, x => x.Ranking!);
            ViewBag.searchValue = searchValue;  
            return View(participants);
        }

        public IActionResult Add()
        {
            // Tạo danh sách giải đấu cho dropdown
            ViewBag.Tournaments = _context.Tournaments
                .Select(t => new SelectListItem
                {
                    Value = t.TournamentID.ToString(), // Convert int to string for dropdown
                    Text = t.Name
                })
                .ToList();

            // Thêm tùy chọn "Không tham gia giải đấu"
            ViewBag.Tournaments.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "-- Không tham gia giải đấu --"
            });

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Participant participant)
        {
            try
            {
                _logger.LogInformation($"Đang thêm người dùng: ID={participant.ParticipantID}, Tên={participant.FullName}");

                if (!ModelState.IsValid)
                {
                    // Lấy lại danh sách giải đấu khi form không hợp lệ
                    PrepareToumamentDropdown(participant.TournamentID);
                    return View(participant);
                }

                // Kiểm tra ID đã tồn tại
                if (await _nguoidungreponsitory.GetByIdAsync(participant.ParticipantID) != null)
                {
                    ModelState.AddModelError("ParticipantID", "Mã người dùng này đã tồn tại");
                    PrepareToumamentDropdown(participant.TournamentID);
                    return View(participant);
                }

    

                // TournamentID đã là nullable int, không cần xử lý thêm

                await _nguoidungreponsitory.AddAsync(participant);
                TempData["SuccessMessage"] = "Đã thêm người dùng thành công!";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm người dùng");
                ModelState.AddModelError("", "Có lỗi xảy ra. Vui lòng thử lại.");
                PrepareToumamentDropdown(participant.TournamentID);
                return View(participant);
            }
        }

        // Phương thức helper để chuẩn bị dropdown giải đấu, sử dụng int? thay vì string
        private void PrepareToumamentDropdown(int? selectedTournamentId = null)
        {
            var tournaments = _context.Tournaments
                .Select(t => new SelectListItem
                {
                    Value = t.TournamentID.ToString(),
                    Text = t.Name,
                    Selected = t.TournamentID == selectedTournamentId
                })
                .ToList();

            // Thêm tùy chọn "Không tham gia giải đấu"
            tournaments.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "-- Không tham gia giải đấu --",
                Selected = selectedTournamentId == null
            });

            ViewBag.Tournaments = tournaments;
        }
        public async Task<IActionResult> Display(string id)
        {
            var khachHang = await _nguoidungreponsitory.GetByIdAsync(id);
            if (khachHang == null)
            {
                return NotFound();
            }

            return View(await BuildAdminDetailsViewModelAsync(khachHang));
        }

        public async Task<IActionResult> Delete(string id)
        {
            var khachHang = await _nguoidungreponsitory.GetByIdAsync(id);
            if (khachHang == null)
            {
                return NotFound();
            }
            return View(khachHang);
        }
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {  
            var khachHang = await _nguoidungreponsitory.GetByIdAsync(id);
            if (khachHang == null)
            {
                return NotFound();
            }
            try
            {
              
                var user = await _userManager.FindByIdAsync(khachHang.UserId);
                if (user != null)
                {
                    var result = await _userManager.DeleteAsync(user);

                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        return View("Delete", khachHang);
                    }
                }

                // Xóa người dùng trong CSDL của bạn
                await _nguoidungreponsitory.DeleteAsync(id);


                var tournaments = await _context.match
                .Include(m => m.Socre)  // Load Socre
                  .ThenInclude(s => s.participant)  
                .Where(m => m.Socre.Any(s => s.participant != null && s.participant.UserId == khachHang.UserId))  
                .Select(m => m.TournamentID) 
                .Distinct()  
                .ToListAsync();  

                foreach (var tid in tournaments.Where(t => t.HasValue))  // Duyệt các tid không null
                {
                    await _rankingService.RebuildTournamentAsync(tid.Value);  // Rebuild ranking
                }
                // Thông báo và chuyển hướng
                TempData["SuccessMessage"] = "Đã xóa người dùng thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                ModelState.AddModelError("", "Không thể xóa người dùng: " + ex.Message);
                return View("Delete", khachHang);
            }
        }

        public async Task<IActionResult> Update(string id)
        {
            var participant = await _nguoidungreponsitory.GetByIdAsync(id);
            if (participant == null)
            {
                return NotFound();
            }

            var tournaments = await _context.Tournaments.ToListAsync();
            ViewBag.Tournaments = new SelectList(tournaments, "TournamentID", "Name", participant.TournamentID);

            return View(await BuildAdminDetailsViewModelAsync(participant));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(string id, ParticipantAdminDetailsViewModel model)
        {
            if (id != model.Participant.ParticipantID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Tìm khách hàng cũ để cập nhật
                    var existingCustomer = await _nguoidungreponsitory.GetByIdAsync(id);
                    if (existingCustomer == null)
                    {
                        return NotFound();
                    }

                    // Cập nhật tất cả các thuộc tính
                    existingCustomer.FullName = model.Participant.FullName;
                    existingCustomer.Club = model.Participant.Club;
                    existingCustomer.sdt = model.Participant.sdt;
                    existingCustomer.email = model.Participant.email;
                    existingCustomer.ChieuCao = model.Participant.ChieuCao;
                    existingCustomer.CanNang = model.Participant.CanNang;
                    existingCustomer.tuoi = model.Participant.tuoi;
                    existingCustomer.Diachi = model.Participant.Diachi;
                    existingCustomer.TournamentID = model.Participant.TournamentID;

                    var rankingIds = model.Rankings
                        .Select(r => r.Id)
                        .Distinct()
                        .ToList();
                    var participantRankings = string.IsNullOrWhiteSpace(existingCustomer.UserId)
                        ? new List<TournamentRanking>()
                        : await _context.TournamentRankings
                            .Where(r => r.UserId == existingCustomer.UserId
                                && rankingIds.Contains(r.Id))
                            .ToListAsync();

                    if (participantRankings.Count != rankingIds.Count)
                    {
                        ModelState.AddModelError(
                            nameof(model.Rankings),
                            "Có dữ liệu ranking không hợp lệ hoặc không thuộc người tham gia này.");
                    }
                    else
                    {
                        foreach (var ranking in participantRankings)
                        {
                            var submittedRanking = model.Rankings.First(r => r.Id == ranking.Id);
                            ranking.Rating = submittedRanking.Rating;
                            ranking.Tier = CalculateTier(submittedRanking.Rating);
                            ranking.UpdatedAt = DateTime.UtcNow;
                        }
                    }

                    if (!ModelState.IsValid)
                    {
                        return await ReturnUpdateViewAsync(model);
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đã cập nhật thông tin và điểm ranking thành công.";

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật khách hàng: " + ex.Message);
                }
            }

            return await ReturnUpdateViewAsync(model);
        }

        private async Task<ParticipantAdminDetailsViewModel> BuildAdminDetailsViewModelAsync(
            Participant participant)
        {
            var rankings = new List<ParticipantRankingEditViewModel>();

            if (!string.IsNullOrWhiteSpace(participant.UserId))
            {
                rankings = await _context.TournamentRankings
                    .AsNoTracking()
                    .Where(r => r.UserId == participant.UserId)
                    .OrderByDescending(r => r.Tournament.StartDate)
                    .Select(r => new ParticipantRankingEditViewModel
                    {
                        Id = r.Id,
                        TournamentId = r.TournamentId,
                        TournamentName = r.Tournament.Name,
                        Rating = r.Rating,
                        Tier = r.Tier,
                        MatchesPlayed = r.MatchesPlayed,
                        Wins = r.Wins,
                        Losses = r.Losses,
                        UpdatedAt = r.UpdatedAt
                    })
                    .ToListAsync();
            }

            return new ParticipantAdminDetailsViewModel
            {
                Participant = participant,
                Rankings = rankings
            };
        }

        private async Task<IActionResult> ReturnUpdateViewAsync(
            ParticipantAdminDetailsViewModel submittedModel)
        {
            var tournaments = await _context.Tournaments.ToListAsync();
            ViewBag.Tournaments = new SelectList(
                tournaments,
                "TournamentID",
                "Name",
                submittedModel.Participant.TournamentID);

            var participant = await _nguoidungreponsitory
                .GetByIdAsync(submittedModel.Participant.ParticipantID);
            if (participant == null)
            {
                return NotFound();
            }

            var viewModel = await BuildAdminDetailsViewModelAsync(participant);
            var submittedRatings = submittedModel.Rankings
                .ToDictionary(r => r.Id, r => r.Rating);

            foreach (var ranking in viewModel.Rankings)
            {
                if (submittedRatings.TryGetValue(ranking.Id, out var rating))
                {
                    ranking.Rating = rating;
                }
            }

            viewModel.Participant = submittedModel.Participant;
            return View("Update", viewModel);
        }

        private static int CalculateTier(float rating)
        {
            if (rating < 900) return 0;
            if (rating < 1100) return 1;
            if (rating < 1300) return 2;
            return 3;
        }
    }   
}
