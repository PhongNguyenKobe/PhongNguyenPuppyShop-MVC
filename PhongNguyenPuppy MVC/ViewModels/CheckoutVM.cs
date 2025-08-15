namespace PhongNguyenPuppy_MVC.ViewModels
{
    public class CheckoutVM
    {
        public bool GiongKhachHang { get; set; }
        public string? HoTen { get; set; }
        public string? DiaChi { get; set; }
        public string? DienThoai { get; set; }
        public string? GhiChu { get; set; }
        public int PhiVanChuyen { get; set; }

        public string? MaGiamGia { get; set; } // Mã giảm giá người dùng nhập
        public double GiamGiaApDung { get; set; } = 0; // Giá trị giảm sau khi áp dụng

    }
}
