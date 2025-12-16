using System.Diagnostics;
using BAO_CAO.Models;
using BaoCaoDACS.Areas.Admin.Models;
using BaoCaoDACS.Controllers;
using BaoCaoDACS.Models;
using BaoCaoDACS.Reponsitory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BaoCaoDACS.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ThongKeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INguoidungreponsitory _nguoidungreponsitory;
        public ThongKeController(
            INguoidungreponsitory nguoidungreponsitory,
              UserManager<ApplicationUser> UserManager,
            ILogger<HomeController> logger,
            AppDbContext context)
        {
            _nguoidungreponsitory = nguoidungreponsitory;
            _userManager = UserManager;
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> SoNguoiThamGiaTheoThang()
        {
            var data = await _context.Tournaments
                .Where(t => t.participant.Any())
                .GroupBy(t => new { t.StartDate.Year, t.StartDate.Month })
                .Select(g => new {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    SoNguoi = g.SelectMany(t => t.participant).Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            // Định dạng lại Thang sau khi đã lấy dữ liệu
            var result = data.Select(x => new {
                Thang = $"{x.Year}-{x.Month.ToString("D2")}", // Định dạng YYYY-MM
                SoNguoi = x.SoNguoi
            });

            return Json(result);
        }



    }
}
