using System.ComponentModel.DataAnnotations;

namespace PhongNguyenPuppy_MVC.ViewModels
{
    public class RegisterVM
    {
        [Key]
        [Display(Name = "Tên đăng nhập")]
        [Required(ErrorMessage = "*")]
        [MaxLength(20, ErrorMessage = "Tên đăng nhập không được quá 20 ký tự")]
        public string MaKh { get; set; }

        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "*")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên")]
        [DataType(DataType.Password)]
        public string? MatKhau { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu")]
        [DataType(DataType.Password)]
        [Compare("MatKhau", ErrorMessage = "Mật khẩu nhập lại không khớp")]
        public string? MatKhauNhapLai { get; set; }


        [Display(Name = "Họ tên")]
        [Required(ErrorMessage = "*")]
        [MaxLength(50, ErrorMessage = "Họ tên không được quá 50 ký tự")]
        public string HoTen { get; set; }

        [Display(Name = "Giới tính")]
        public bool GioiTinh { get; set; } = true;

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? NgaySinh { get; set; }

        [Display(Name = "Địa chỉ")]
        [MaxLength(60, ErrorMessage = "Địa chỉ không được quá 60 ký tự")]
        public string? DiaChi { get; set; }

        [Display(Name = "Điện thoại")]
        [MaxLength(12, ErrorMessage = "Điện thoại không được quá 12 ký tự")]
        [RegularExpression(@"^(\+84|0)[0-9]{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? DienThoai { get; set; }

        [StringLength(256)]
        [EmailAddress]
        public string Email { get; set; }


        [Display(Name = "Hình ảnh")]
        public IFormFile Hinh { get; set; }
    }
}
