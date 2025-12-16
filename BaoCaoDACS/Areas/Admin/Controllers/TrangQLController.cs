using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BaoCaoDACS.Models;

using BaoCaoDACS.Areas.Admin.Models;
using BaoCaoDACS.Reponsitory;

namespace BAO_CAO.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class TrangQLController : Controller
    {
        private readonly INguoidungreponsitory _nguoidungreponsitory;
        private readonly ITranDaureponsitory _tranDaureponsitory;
        private readonly IGiaiDaureponsitory _giaiDaureponsitory;
        private readonly IKetquareponsitory _ketquareponsitory;
        private readonly ILoaiHinhreponsitory _loaihreponsitory;
        private readonly ILogger<NguoiDungController> _logger;
        private readonly AppDbContext _context;

        public TrangQLController(
            INguoidungreponsitory nguoidungreponsitory,
            ILogger<NguoiDungController> logger,
            AppDbContext context,
            IGiaiDaureponsitory giaiDaureponsitory,
            ITranDaureponsitory tranDaureponsitory,
            IKetquareponsitory ketquareponsitory,
            ILoaiHinhreponsitory loaihreponsitory
            )

        {
            _giaiDaureponsitory = giaiDaureponsitory;
            _tranDaureponsitory = tranDaureponsitory;
            _ketquareponsitory = ketquareponsitory;
            _loaihreponsitory = loaihreponsitory;
            _nguoidungreponsitory = nguoidungreponsitory;
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.khachhangcount = await _nguoidungreponsitory.GetTotalCountAsync();
            ViewBag.trandaucount = await _tranDaureponsitory.GetTotalCountAsync();
            ViewBag.Giadaucount = await _giaiDaureponsitory.GetTotalCountAsync();
            return View();
        }
    }
}
