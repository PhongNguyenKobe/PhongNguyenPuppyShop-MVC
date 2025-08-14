namespace PhongNguyenPuppy_MVC.Areas.Admin.ViewModels
{
    public class DanhSachKhachHangVM
    {
        public List<KhachHangVM> DanhSach { get; set; }
        public string TuKhoa { get; set; }
        public int TrangHienTai { get; set; }
        public int TongSoTrang { get; set; }
    }

}
