namespace PhongNguyenPuppy_MVC.Areas.Admin.Services
{
    public class ThongKeKhachHangDto
    {
        public int TongSoKhachHang { get; set; }
        public int SoKhachHoatDong { get; set; }
        public int SoKhachBiKhoa { get; set; } 
        public decimal TongDoanhThu { get; set; }
    }

    public interface IDichVuThongKe
    {
        ThongKeKhachHangDto LayThongKeKhachHang();
    }

}
