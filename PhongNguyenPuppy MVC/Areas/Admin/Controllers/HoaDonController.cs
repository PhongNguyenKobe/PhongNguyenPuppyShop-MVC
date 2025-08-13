using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhongNguyenPuppy_MVC.Areas.Admin.ViewModels;
using PhongNguyenPuppy_MVC.Data;
using PhongNguyenPuppy_MVC.Helpers;

namespace PhongNguyenPuppy_MVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "AdminScheme")]
    public class HoaDonController : Controller
    {
        private readonly PhongNguyenPuppyContext _context;
        private const int PageSize = 10;
        public HoaDonController(PhongNguyenPuppyContext context)
        {
            _context = context;
        }

        // Danh sách hóa đơn
        public async Task<IActionResult> Index(string tuKhoa, string locThoiGian = "tatca", int? nam = null, int page = 1)
        {
            var query = _context.HoaDons
                .Include(h => h.MaTrangThaiNavigation)
                .Include(h => h.ChiTietHds)
                .Include(h => h.MaKhNavigation)
                .Select(h => new HoaDonViewModel
                {
                    MaHd = h.MaHd,
                    HoTen = h.HoTen ?? h.MaKhNavigation.HoTen,
                    NgayDat = h.NgayDat,
                    TrangThai = h.MaTrangThaiNavigation.TenTrangThai,
                    TongTien = h.ChiTietHds.Sum(ct => (ct.DonGia * ct.SoLuong) * (1 - ct.GiamGia / 100)) - h.GiamGia + h.PhiVanChuyen
                });
            // Lọc theo tên và mahđ
            if (!string.IsNullOrEmpty(tuKhoa))
            {
                query = query.Where(h =>
                    h.HoTen.Contains(tuKhoa) ||
                    h.MaHd.ToString().Contains(tuKhoa));
            }
            // Lọc theo thời gian
            var now = DateTime.Now;

            if (locThoiGian == "homnay")
            {
                query = query.Where(h => h.NgayDat.Date == now.Date);//đảm bảo hóa đơn mới nhất (ngày đặt gần nhất) sẽ hiển thị đầu tiên.
            }
            else if (locThoiGian == "tuannay")
            {
                var startOfWeek = now.AddDays(-(int)now.DayOfWeek);
                query = query.Where(h => h.NgayDat.Date >= startOfWeek.Date);
            }
            else if (locThoiGian == "thangnay")
            {
                query = query.Where(h => h.NgayDat.Month == now.Month && h.NgayDat.Year == now.Year);
            }
            if (nam.HasValue)
            {
                query = query.Where(h => h.NgayDat.Year == nam.Value);
            }

            var totalItems = await query.CountAsync();

            var hoaDonList = await query
                .OrderByDescending(h => h.NgayDat)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
            // Tạo danh sách trạng thái để hiển thị trong dropdown
            ViewBag.TrangThaiList = await _context.TrangThais
                .Select(t => new SelectListItem
                {
                    Value = t.MaTrangThai.ToString(),
                    Text = t.TenTrangThai
                }).ToListAsync();

            ViewBag.PageNumber = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / PageSize);
            ViewBag.TuKhoa = tuKhoa;
            ViewBag.LocThoiGian = locThoiGian;
            ViewBag.Nam = nam;
            return View(hoaDonList);
        }

        // Chi tiết hóa đơn
        public async Task<IActionResult> Details(int id)
        {
            var hoaDonVM = await _context.HoaDons
                .Include(h => h.MaKhNavigation)
                .Include(h => h.MaTrangThaiNavigation)
                .Include(h => h.ChiTietHds)
                    .ThenInclude(ct => ct.MaHhNavigation)
                .Where(h => h.MaHd == id)
                .Select(h => new HoaDonDetailsViewModel
                {
                    MaHd = h.MaHd,
                    MaKh = h.MaKh,
                    HoTen = h.HoTen ?? h.MaKhNavigation.HoTen,
                    NgayDat = h.NgayDat,
                    TrangThai = h.MaTrangThaiNavigation.TenTrangThai,
                    DiaChi = h.DiaChi,
                    PhiVanChuyen = h.PhiVanChuyen,
                    GhiChu = h.GhiChu,
                    GiamGia = h.GiamGia,
                    TongTien = (float)h.ChiTietHds.Sum(ct => (ct.DonGia * ct.SoLuong) * (1 - ct.GiamGia / 100)) - h.GiamGia + h.PhiVanChuyen,
                    DienThoai = h.MaKhNavigation.DienThoai,
                    Email = h.MaKhNavigation.Email,
                    ChiTietHds = h.ChiTietHds.Select(ct => new ChiTietHdViewModel
                    {
                        TenHh = ct.MaHhNavigation.TenHh,
                        DonGia = ct.DonGia,
                        SoLuong = ct.SoLuong,
                        GiamGia = ct.GiamGia,
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (hoaDonVM == null)
                return NotFound();

            return View(hoaDonVM);
        }

        [HttpPost]
        public async Task<IActionResult> CapNhatTrangThai(int maHd, int maTrangThai)
        {
            var hoaDon = await _context.HoaDons.FindAsync(maHd);
            if (hoaDon == null) return NotFound();

            hoaDon.MaTrangThai = maTrangThai;
            await _context.SaveChangesAsync();

            return Ok();
        }



    }
}
