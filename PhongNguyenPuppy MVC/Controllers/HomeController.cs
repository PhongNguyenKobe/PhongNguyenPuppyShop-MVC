using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PhongNguyenPuppy_MVC.Models;
using PhongNguyenPuppy_MVC.ViewModels;
using Microsoft.EntityFrameworkCore;
using PhongNguyenPuppy_MVC.Data; 

namespace PhongNguyenPuppy_MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly PhongNguyenPuppyContext _context; 

        public HomeController(ILogger<HomeController> logger, PhongNguyenPuppyContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            var model = _context.HangHoas
                .Include(h => h.MaLoaiNavigation)
                .Select(h => new ChiTietHangHoaVM
                {
                    MaHh = h.MaHh,
                    TenHh = h.TenHh,
                    DonGia = h.DonGia ?? 0,
                    Hinh = h.Hinh ?? "",
                    MoTaNgan = h.MoTaDonVi ?? "",
                    TenLoai = h.MaLoaiNavigation.TenLoai
                })
                .ToList();
            return View(model);
        }

        [Route("/404")]
        public IActionResult PageNotFound()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}