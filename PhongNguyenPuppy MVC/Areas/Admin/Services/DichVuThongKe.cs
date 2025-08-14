using Microsoft.EntityFrameworkCore;
using PhongNguyenPuppy_MVC.Data;
using PhongNguyenPuppy_MVC.Models;

namespace PhongNguyenPuppy_MVC.Areas.Admin.Services
{
    public class DichVuThongKe : IDichVuThongKe
    {
        private readonly PhongNguyenPuppyContext _context;

        public DichVuThongKe(PhongNguyenPuppyContext context)
        {
            _context = context;
        }

        public ThongKeKhachHangDto LayThongKeKhachHang()
        {
            var tong = _context.KhachHangs.Count();
            var hoatDong = _context.KhachHangs.Count(kh => kh.HieuLuc);
            var biKhoa = tong - hoatDong;

            return new ThongKeKhachHangDto
            {
                TongSoKhachHang = tong,
                SoKhachHoatDong = hoatDong,
                SoKhachBiKhoa = biKhoa,
                TongDoanhThu = 0 // tạm thời nếu chưa có bảng đơn hàng
            };
        }
        public int LayTongSanPham()
        {
            return _context.HangHoas.Count();
        }

        public int LayTongDonHang(int year)
        {
            return _context.HoaDons.Count(hd => hd.NgayDat.Year == year);
        }

        public decimal LayTongDoanhThu(int year)
        {
            return _context.HoaDons
                .Include(hd => hd.ChiTietHds)
                .Where(hd => hd.NgayDat.Year == year)
                .Sum(hd =>
                    hd.ChiTietHds.Sum(ct =>
                        (decimal)ct.SoLuong * ((decimal)ct.DonGia - (decimal)ct.GiamGia)
                    ) + (decimal)hd.PhiVanChuyen
                );
        }

        public IEnumerable<DoanhThuThangDto> LayDoanhThuTheoThang(int year)
        {
            var allMonths = Enumerable.Range(1, 12).ToList();

            var monthlyRaw = _context.HoaDons
                .Include(hd => hd.ChiTietHds)
                .Where(hd => hd.NgayDat.Year == year)
                .GroupBy(hd => hd.NgayDat.Month)
                .Select(g => new DoanhThuThangDto
                {
                    Thang = g.Key,
                    TongTien = g.Sum(hd =>
                        hd.ChiTietHds.Sum(ct => (decimal)ct.SoLuong * ((decimal)ct.DonGia - (decimal)ct.GiamGia)) + (decimal)hd.PhiVanChuyen
)
                }).ToList();

            return allMonths.Select(m => new DoanhThuThangDto
            {
                Thang = m,
                TongTien = monthlyRaw.FirstOrDefault(x => x.Thang == m)?.TongTien ?? 0
            }).ToList();
        }

        public IEnumerable<SanPhamBanChayDto> LaySanPhamBanChay(int year)
        {
            return _context.HoaDons
                .Where(hd => hd.NgayDat.Year == year)
                .Include(hd => hd.ChiTietHds)
                .SelectMany(hd => hd.ChiTietHds)
                .Include(ct => ct.MaHhNavigation)
                .GroupBy(ct => ct.MaHh)
                .Select(g => new SanPhamBanChayDto
                {
                    TenHh = g.First().MaHhNavigation.TenHh,
                    SoLuongBan = g.Sum(x => x.SoLuong)
                }).OrderByDescending(x => x.SoLuongBan).Take(5).ToList();
        }


        public IEnumerable<TrangThaiDonHangDto> LayTrangThaiDonHang(int year)
        {
            return _context.HoaDons
                .Include(hd => hd.MaTrangThaiNavigation)
                .Where(hd => hd.NgayDat.Year == year)
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
                .Select(g => new TrangThaiDonHangDto
                {
                    TrangThai = g.Key,
                    SoLuong = g.Sum(x => x.SoLuong)
                })
                .ToList();
        }


        public IEnumerable<KhachHangTopDto> LayTopKhachHang(int year)
        {
            return _context.HoaDons
                .Include(hd => hd.MaKhNavigation)
                .Include(hd => hd.ChiTietHds)
                .Where(hd => hd.NgayDat.Year == year)
                .GroupBy(hd => hd.MaKh)
                .Select(g => new KhachHangTopDto
                {
                    TenKhachHang = g.First().MaKhNavigation.HoTen,
                    TongTien = g.Sum(hd => hd.ChiTietHds.Sum(ct => (decimal)ct.SoLuong * ((decimal)ct.DonGia - (decimal)ct.GiamGia)) + (decimal)hd.PhiVanChuyen
)

                }).OrderByDescending(x => x.TongTien).Take(5).ToList();
        }
        public int LayTongSanPhamDaBan(int year)
        {
            return _context.HoaDons
                .Where(hd => hd.NgayDat.Year == year)
                .SelectMany(hd => hd.ChiTietHds)
                .Sum(ct => ct.SoLuong);
        }


    }
}
