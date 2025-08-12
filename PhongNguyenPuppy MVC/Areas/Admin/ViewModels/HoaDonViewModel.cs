namespace PhongNguyenPuppy_MVC.Areas.Admin.ViewModels
{
    public class HoaDonViewModel
    {
        public int MaHd { get; set; }
        public string HoTen { get; set; }
        public DateTime NgayDat { get; set; }
        public string TrangThai { get; set; }
        public double TongTien { get; set; }
        public double GiamGia { get; set; }
    }

    public class HoaDonDetailsViewModel
    {
        public int MaHd { get; set; }
        public string MaKh { get; set; }
        public string HoTen { get; set; }
        public DateTime NgayDat { get; set; }
        public string TrangThai { get; set; }
        public string DiaChi { get; set; }
        public double PhiVanChuyen { get; set; }
        public string? GhiChu { get; set; }
        public string DienThoai { get; set; }
        public string Email { get; set; }
        public double TongTien { get; set; }
        public string MaGiamGia { get; set; }
        public double GiamGia { get; set; }
        public List<ChiTietHdViewModel> ChiTietHds { get; set; } = new();
    }

    public class ChiTietHdViewModel
    {
        public string TenHh { get; set; }
        public double DonGia { get; set; }
        public int SoLuong { get; set; }
        public double GiamGia { get; set; } // Giữ lại nếu muốn hiển thị cả % giảm
        public string MaGiamGia { get; set; } // THÊM: Lưu mã giảm giá (Code từ MaGiamGia)
    }

    public class HoaDonPagedViewModel
    {
        public List<HoaDonViewModel> HoaDons { get; set; }
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public string TuKhoa { get; set; }
    }


}