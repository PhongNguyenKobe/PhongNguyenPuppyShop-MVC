using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhongNguyenPuppy_MVC.Data;
using PhongNguyenPuppy_MVC.Areas.Admin.Services;

namespace PhongNguyenPuppy_MVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = "AdminScheme", Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly PhongNguyenPuppyContext _db;
        private readonly IDichVuThongKe _thongKeService;

        public DashboardController(PhongNguyenPuppyContext db, IDichVuThongKe thongKeService)
        {
            _db = db;
            _thongKeService = thongKeService;
        }

        public IActionResult Index()
        {
            // Lấy mã nhân viên từ tài khoản đăng nhập
            var username = User.Identity.Name;
            var nhanVien = _db.NhanViens.FirstOrDefault(nv => nv.MaNv == username);
            ViewBag.HoTen = nhanVien?.HoTen ?? "";

            // Ngày và giờ hiện tại
            ViewBag.Today = DateTime.Now.ToString("dd/MM/yyyy");
            ViewBag.TimeNow = DateTime.Now.ToString("HH:mm:ss");
            // Tên đăng nhập
            ViewBag.HoTen = User.Identity.Name;

            // Chào theo giờ
            var hour = DateTime.Now.Hour;
            ViewBag.Greeting = hour switch
            {
                <= 6 => "Chúc buổi sáng tốt lành ☀️",
                <= 12 => "Chúc một ngày hiệu quả 💼",
                <= 18 => "Chúc buổi tối vui vẻ 🌙",
                _ => "Chúc ngủ ngon 😴"
            };

            // Năm hiện tại
            int currentYear = DateTime.Now.Year;
            ViewBag.CurrentYear = currentYear;

            // Gọi các thống kê
            ViewBag.TotalProducts = _thongKeService.LayTongSanPham();
            ViewBag.TotalOrders = _thongKeService.LayTongDonHang(currentYear);
            ViewBag.TotalRevenue = _thongKeService.LayTongDoanhThu(currentYear);
            ViewBag.TotalSoldProducts = _thongKeService.LayTongSanPhamDaBan(currentYear);
            ViewBag.MonthlyRevenue = _thongKeService.LayDoanhThuTheoThang(currentYear);
            ViewBag.BestSelling = _thongKeService.LaySanPhamBanChay(currentYear);
            ViewBag.OrderStatus = _thongKeService.LayTrangThaiDonHang(currentYear);
            ViewBag.TopCustomers = _thongKeService.LayTopKhachHang(currentYear);

            return View();
        }
    }
}
