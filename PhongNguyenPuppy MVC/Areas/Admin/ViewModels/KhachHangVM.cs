namespace PhongNguyenPuppy_MVC.Areas.Admin.ViewModels
{
    public class KhachHangVM
    {
        public string MaKh { get; set; }
        public string HoTen { get; set; }
        public string Email { get; set; }
        public string DienThoai { get; set; }
        public bool TrangThai { get; set; } // ánh xạ hieuluc 1: hoạt động, 0: bị khóa
    }

}
