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
            // THÊM PHẦN SEO Data
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            ViewData["SeoData"] = new PhongNguyenPuppy_MVC.Helpers.SeoData
            {
                Title = "PhongNguyen Puppy - Chuyên Thức Ăn & Đồ Dùng Cho Chó Chất Lượng",
                Description = "PhongNguyen Puppy Shop cung cấp thức ăn, đồ chơi, phụ kiện cho chó chất lượng cao. Giao hàng toàn quốc, giá tốt nhất. Mua ngay!",
                Keywords = "thức ăn chó, đồ dùng chó, phụ kiện chó, puppy shop, royal canin, pedigree",
                ImageUrl = "/img/hero_1.png", // Hình đại diện cho trang chủ
                CanonicalUrl = $"{baseUrl}/",
                Type = "website"
            };
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