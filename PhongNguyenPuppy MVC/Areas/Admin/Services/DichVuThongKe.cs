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

    }
}
