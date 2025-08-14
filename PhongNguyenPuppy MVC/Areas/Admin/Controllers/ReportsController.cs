using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhongNguyenPuppy_MVC.Areas.Admin.Services;
using PhongNguyenPuppy_MVC.Data;

namespace PhongNguyenPuppy_MVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReportsController : Controller
    {
        private readonly PhongNguyenPuppyContext _db;
        private readonly IDichVuThongKe _thongKeService;
        public ReportsController(PhongNguyenPuppyContext db, IDichVuThongKe dichVuThongKe)
        {
            _db = db;
            _thongKeService = dichVuThongKe;
        }

        public IActionResult Index(int? year)
        {
            int currentYear = year ?? DateTime.Now.Year;
            ViewBag.CurrentYear = currentYear;
            // Tổng số sản phẩm
            var totalProducts = _db.HangHoas.Count();

            // Tổng số hóa đơn
            var totalOrders = _db.HoaDons.Count();

            // Tổng doanh thu (tiền hàng + phí vận chuyển)
            var totalRevenue = _db.HoaDons
                .Include(hd => hd.ChiTietHds)
                .Sum(hd =>
                    hd.ChiTietHds.Sum(ct => ct.SoLuong * (ct.DonGia - ct.GiamGia)) +
                    (hd.PhiVanChuyen)
                );

            // Sản phẩm bán chạy nhất (Top 5)
            var bestSelling = _db.ChiTietHds
                .Include(ct => ct.MaHhNavigation)
                .GroupBy(ct => ct.MaHh)
                .Select(g => new
                {
                    TenHh = g.First().MaHhNavigation.TenHh,
                    SoLuongBan = g.Sum(x => x.SoLuong)
                })
                .OrderByDescending(x => x.SoLuongBan)
                .Take(5)
                .ToList();

            // Doanh thu theo tháng trong năm hiện tại
            var allMonths = Enumerable.Range(1, 12).ToList();

            var monthlyRevenueRaw = _db.HoaDons
                .Where(hd => hd.NgayDat.Year == currentYear && hd.NgayDat <= DateTime.Now)
                .GroupBy(hd => hd.NgayDat.Month)
                .Select(g => new
                {
                    Thang = g.Key,
                    TongTien = g.Sum(hd => hd.ChiTietHds.Sum(ct => ct.SoLuong * ct.DonGia))
                })
                .ToList();

            // Ghép dữ liệu với danh sách tháng cố định
            var monthlyRevenue = allMonths
                .Select(m => new {
                    Thang = m,
                    TongTien = monthlyRevenueRaw.FirstOrDefault(x => x.Thang == m)?.TongTien ?? 0
                })
                .ToList();


            // Đơn hàng theo trạng thái
            ViewBag.OrderStatus = _db.HoaDons
             .Include(hd => hd.MaTrangThaiNavigation)
             .AsEnumerable() 
             .GroupBy(hd => hd.MaTrangThaiNavigation.TenTrangThai)
             .Select(g => new {
                 TrangThai = g.Key switch
                 {
                     "Mới đặt hàng" => "Chờ xác nhận",
                     "Chờ giao hàng" => "Đang giao",
                     "Đã giao hàng" => "Hoàn tất",
                     "Đã thanh toán" => "Hoàn tất",
                     "Khách hàng hủy đơn hàng" => "Hủy",
                     _ => "Không xác định"
                 },
                 SoLuong = g.Count()
             })
             .GroupBy(x => x.TrangThai)
             .Select(g => new {
                 TrangThai = g.Key,
                 SoLuong = g.Sum(x => x.SoLuong)
             })
             .ToList();

            // Top khách hàng chi tiêu nhiều nhất
            var topCustomers = _db.HoaDons
                .Include(hd => hd.MaKhNavigation) // tên khách hàng
                .Include(hd => hd.ChiTietHds)
                .GroupBy(hd => hd.MaKh)
                .Select(g => new
                {
                    TenKhachHang = g.First().MaKhNavigation.HoTen,
                    TongTien = g.Sum(hd =>
                        hd.ChiTietHds.Sum(ct => ct.SoLuong * (ct.DonGia - ct.GiamGia)) + (hd.PhiVanChuyen)
                    )
                })
                .OrderByDescending(x => x.TongTien)
                .Take(3)
                .ToList();


            // Truyền dữ liệu sang View
            ViewBag.TotalProducts = _thongKeService.LayTongSanPham();
            ViewBag.TotalOrders = _thongKeService.LayTongDonHang(currentYear);
            ViewBag.TotalRevenue = _thongKeService.LayTongDoanhThu(currentYear);
            ViewBag.MonthlyRevenue = _thongKeService.LayDoanhThuTheoThang(currentYear);
            ViewBag.BestSelling = _thongKeService.LaySanPhamBanChay(currentYear);
            ViewBag.OrderStatus = _thongKeService.LayTrangThaiDonHang(currentYear);
            ViewBag.TopCustomers = _thongKeService.LayTopKhachHang(currentYear);
            ViewBag.TotalSoldProducts = _thongKeService.LayTongSanPhamDaBan(currentYear);


            return View();
        }
    }
}
