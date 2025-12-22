using System.ComponentModel.DataAnnotations;

namespace PhongNguyenPuppy_MVC.ViewModels
{
    public class CheckoutVM
    {
        public bool GiongKhachHang { get; set; }
        public string? HoTen { get; set; }
        public string? DiaChi { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^(03|05|07|08|09)\d{8}$", ErrorMessage = "Số điện thoại không hợp lệ (phải bắt đầu bằng 03/05/07/08/09 và có 10 số)")]
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
