using System.ComponentModel.DataAnnotations;

namespace PhongNguyenPuppy_MVC.ViewModels
{
    public class LoginVM
    {
        [Display(Name = "Tên đăng nhập")]
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [MaxLength(20, ErrorMessage = "Tên đăng nhập không được quá 20 ký tự")]
        public string UserName { get; set; }

        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "*")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

    }
}
