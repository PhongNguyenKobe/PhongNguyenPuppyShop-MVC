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
        public IActionResult Index(int? loai, int page = 1, string sortOrder = "", int? minPrice = null, int? maxPrice = null, string category = "", string search = "")
        {
            int pageSize = 12;
            IQueryable<HangHoa> hangHoas = db.HangHoas.Include(p => p.MaLoaiNavigation);

            if (!string.IsNullOrEmpty(search))
            {
                hangHoas = hangHoas.Where(p => p.TenHh.Contains(search));
            }
            // Lọc theo loại sản phẩm
            if (loai.HasValue)
            {
                hangHoas = hangHoas.Where(p => p.MaLoai == loai.Value);
            }

            // Lọc theo khoảng giá
            if (minPrice.HasValue)
            {
                hangHoas = hangHoas.Where(p => p.DonGia >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                hangHoas = hangHoas.Where(p => p.DonGia <= maxPrice.Value);
            }

            // Lọc theo danh mục đặc biệt
            if (!string.IsNullOrEmpty(category))
            {
                switch (category)
                {
                    case "Fresh":
                        hangHoas = hangHoas.OrderByDescending(p => p.MaHh);
                        break;
                    case "Sale":
                    case "Discount":
                        hangHoas = hangHoas.Where(p => p.GiamGia > 0); 
                        break;
                    
                }
            }
            // Xử lý sắp xếp
            switch (sortOrder)
            {
                case "price_asc":
                    hangHoas = hangHoas.OrderBy(p => p.DonGia);
                    break;
                case "price_desc":
                    hangHoas = hangHoas.OrderByDescending(p => p.DonGia);
                    break;
                case "Fresh":
                    hangHoas = hangHoas.OrderByDescending(p => p.MaHh);
                    break;
                case "Sale":
                    hangHoas = hangHoas.Where(p => p.GiamGia > 0).OrderByDescending(p => p.GiamGia);
                    break;
                default:
                    hangHoas = hangHoas.OrderBy(p => p.MaHh);
                    break;
            }

            var totalItems = hangHoas.Count();

            var result = hangHoas
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

            // Truyền dữ liệu sang View
            ViewBag.Search = search;
            ViewBag.PageNumber = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.Loai = loai;
            ViewBag.SortOrder = sortOrder;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Category = category;

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
                return Redirect("/404");
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
                SoLuongTon = 10, // Giá trị mặc định, chưa tính
                DiemDanhGia = 5, // Giá trị mặc định, chưa check

                // Thêm RelatedProducts (sản phẩm cùng danh mục)
                RelatedProducts = db.HangHoas
                    .Where(r => r.MaLoai == data.MaLoai && r.MaHh != data.MaHh)
                    .Take(6) // Giới hạn 6 sản phẩm liên quan
                    .Select(r => new ChiTietHangHoaVM
                    {
                        MaHh = r.MaHh,
                        TenHh = r.TenHh,
                        DonGia = r.DonGia ?? 0,
                        ChiTiet = r.MoTa ?? "",
                        Hinh = r.Hinh ?? "",
                        MoTaNgan = r.MoTaDonVi ?? "",
                        TenLoai = r.MaLoaiNavigation.TenLoai,
                        SoLuongTon = 10, // Giá trị mặc định
                        DiemDanhGia = 5 // Giá trị mặc định
                    })
                    .ToList()
            };

            return View(result);
        }
    }
}
