using PhongNguyenPuppy_MVC.Areas.Admin.ViewModels;
using PhongNguyenPuppy_MVC.Data;

namespace PhongNguyenPuppy_MVC.Areas.Admin.Services
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TongSoTrang { get; set; }
        public int TrangHienTai { get; set; }
        public int TongSoLuong { get; set; }
        public int SoLuongMoiTrang { get; set; }
    }
    public interface IKhachHangRepository
    {
        List<string> LayDanhSachEmailKhachHang();
        PagedResult<KhachHang> LayTatCa(string tuKhoa, int trang);
        KhachHang LayTheoId(string id);
        void CapNhat(SuaKhachHangVM vm);
        void Xoa(string id);


    }

}
