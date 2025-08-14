namespace PhongNguyenPuppy_MVC.Areas.Admin.Services
{
    public class ThongKeKhachHangDto
    {
        public int TongSoKhachHang { get; set; }
        public int SoKhachHoatDong { get; set; }
        public int SoKhachBiKhoa { get; set; }
        public decimal TongDoanhThu { get; set; }
    }

    public class DoanhThuThangDto
    {
        public int Thang { get; set; }
        public decimal TongTien { get; set; }
    }

    public class SanPhamBanChayDto
    {
        public string TenHh { get; set; }
        public int SoLuongBan { get; set; }
    }

    public class TrangThaiDonHangDto
    {
        public string TrangThai { get; set; }
        public int SoLuong { get; set; }
    }

    public class KhachHangTopDto
    {
        public string TenKhachHang { get; set; }
        public decimal TongTien { get; set; }
    }

    public interface IDichVuThongKe
    {
        ThongKeKhachHangDto LayThongKeKhachHang();
        int LayTongSanPham();
        int LayTongDonHang(int year);
        decimal LayTongDoanhThu(int year);
        IEnumerable<DoanhThuThangDto> LayDoanhThuTheoThang(int year);
        IEnumerable<SanPhamBanChayDto> LaySanPhamBanChay(int year);
        IEnumerable<TrangThaiDonHangDto> LayTrangThaiDonHang(int year);
        IEnumerable<KhachHangTopDto> LayTopKhachHang(int year);
        int LayTongSanPhamDaBan(int year);

    }
}
