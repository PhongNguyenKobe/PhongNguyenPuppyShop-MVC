using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhongNguyenPuppy_MVC.Data;
using PhongNguyenPuppy_MVC.ViewModels;

namespace PhongNguyenPuppy_MVC.Controllers
{
    public class HangHoaController : Controller
    {
        private readonly PhongNguyenPuppyContext db;

        public HangHoaController(PhongNguyenPuppyContext context)
        {
            db = context;
        }
        public IActionResult Index(int? loai, int page = 1)
        {
            int pageSize = 12;
            IQueryable<HangHoa> hangHoas = db.HangHoas.Include(p => p.MaLoaiNavigation);

            if (loai.HasValue)
            {
                hangHoas = hangHoas.Where(p => p.MaLoai == loai.Value);
            }
            var totalItems = hangHoas.Count();
            var result = hangHoas
                               .OrderBy(p => p.TenHh)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .Select(p => new HangHoaVM
            {
           MaHh = p.MaHh,
                TenHh = p.TenHh,
                DonGia = p.DonGia ?? 0,
                Hinh = p.Hinh ?? "",
                MoTaNgan = p.MoTaDonVi ?? "",
                TenLoai = p.MaLoaiNavigation.TenLoai
            });
            ViewBag.PageNumber = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.Loai = loai;

            return View(result);
        }

        public IActionResult Search(string? query, int page = 1)
        {
            int pageSize = 12;
            IQueryable<HangHoa> hangHoas = db.HangHoas.Include(p => p.MaLoaiNavigation);

            if (!string.IsNullOrEmpty(query))
            {
                hangHoas = hangHoas.Where(p => p.TenHh.Contains(query));
            }

            var totalItems = hangHoas.Count();

            var result = hangHoas
                .OrderBy(p => p.TenHh)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new HangHoaVM
                {
                    MaHh = p.MaHh,
                    TenHh = p.TenHh,
                    DonGia = p.DonGia ?? 0,
                    Hinh = p.Hinh ?? "",
                    MoTaNgan = p.MoTaDonVi ?? "",
                    TenLoai = p.MaLoaiNavigation.TenLoai
                }).ToList();

            ViewBag.PageNumber = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.Query = query;

            return View(result);
        }

        public IActionResult Detail(int id)
        {
            var data = db.HangHoas
                .Include(p => p.MaLoaiNavigation)
                .SingleOrDefault(p => p.MaHh == id);
            if (data == null)
            {
                TempData["Message"] = $"Không tìm thấy sản phẩm {id}";
                return Redirect("/404)");
            }
            var result = new ChiTietHangHoaVM
            {
                MaHh = data.MaHh,
                TenHh = data.TenHh,
                DonGia = data.DonGia ?? 0,
                ChiTiet = data.MoTa ?? "",
                Hinh = data.Hinh ?? "",
                MoTaNgan = data.MoTaDonVi ?? "",
                TenLoai = data.MaLoaiNavigation.TenLoai,
                SoLuongTon = 10, //tinh sau
                DiemDanhGia = 5, //check sau

            };
            return View(result);
        }
    }
}
