using BaoCaoDACS.Areas.Admin.Models;
using BaoCaoDACS.Models;
using BaoCaoDACS.Reponsitory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BaoCaoDACS.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class LoaiHinhThiDauController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILoaiHinhreponsitory _loaiHinhreponsitory;

        public LoaiHinhThiDauController(AppDbContext context, ILoaiHinhreponsitory loaiHinhreponsitory)
        {
            _context = context;
            _loaiHinhreponsitory = loaiHinhreponsitory;
        }

        public async Task<IActionResult> Index()
        {
            var loaiHinhList = await _loaiHinhreponsitory.GetAllAsync();
            return View(loaiHinhList);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LoaiHinhThiDau loaiHinh)
        {
            if (ModelState.IsValid)
            {
                _context.loaiHinhThiDau.Add(loaiHinh);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã thêm loại hình thi đấu thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(loaiHinh);
        }

        public async Task<IActionResult> CreateQuickData()
        {
            // Check if there's already data
            if (!_context.loaiHinhThiDau.Any())
            {
                // Add some sample data
                _context.loaiHinhThiDau.AddRange(
                    new LoaiHinhThiDau { Name = "Đối kháng", MonVo = "Vovinam" },
                    new LoaiHinhThiDau { Name = "Quyền", MonVo = "Karate" },
                    new LoaiHinhThiDau { Name = "Đối kháng", MonVo = "Taekwondo" },
                    new LoaiHinhThiDau { Name = "Quyền đồng diễn", MonVo = "Wushu" }
                );
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã tạo dữ liệu mẫu thành công!";
            }
            else
            {
                TempData["InfoMessage"] = "Đã có dữ liệu trong bảng loại hình thi đấu!";
            }
            return RedirectToAction("Index", "GiaiDau");
        }
    }
}
