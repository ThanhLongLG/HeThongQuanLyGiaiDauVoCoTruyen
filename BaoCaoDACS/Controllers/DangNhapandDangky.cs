using System.Diagnostics;
using BaoCaoDACS.Models;
using Microsoft.AspNetCore.Mvc;

namespace BaoCaoDACS.Controllers
{
    public class DangNhapandDangky : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public DangNhapandDangky(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

     
    }
}
