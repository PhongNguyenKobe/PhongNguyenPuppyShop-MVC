namespace PhongNguyenPuppy_MVC.Areas.Admin.ViewModels
{
    public class SuaKhachHangVM
    {
        public string MaKh { get; set; } 
        public string HoTen { get; set; }
        public string Email { get; set; }
        public string DienThoai { get; set; }
        public bool HieuLuc { get; set; }

        // Thêm phần gửi email
        public string TieuDeEmail { get; set; }
        public string NoiDungEmail { get; set; }
    }

}
