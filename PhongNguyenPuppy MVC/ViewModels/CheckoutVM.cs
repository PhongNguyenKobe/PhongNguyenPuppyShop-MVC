namespace PhongNguyenPuppy_MVC.ViewModels
{
    public class CheckoutVM
    {
        public bool GiongKhachHang { get; set; }
        public string? HoTen { get; set; }
        public string? DiaChi { get; set; }
        public string? DienThoai { get; set; }
        public string? GhiChu { get; set; }

        // Thêm các trường cho GHN
        public int ProvinceId { get; set; }
        public int DistrictId { get; set; }
        public string? WardCode { get; set; }

        public string? MaGiamGia { get; set; } 
        public double GiamGiaApDung { get; set; } = 0; 

    }
}
