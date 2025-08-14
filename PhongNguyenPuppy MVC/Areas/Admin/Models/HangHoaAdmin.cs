using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PhongNguyenPuppy_MVC.Areas.Admin.ViewModels
{
    public class HangHoaCreateViewModel
    {
        [Display(Name = "Tên sản phẩm")]
        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        public string TenHh { get; set; }

        [Display(Name = "Loại")]
        [Required(ErrorMessage = "Loại sản phẩm không được để trống")]
        public int? MaLoai { get; set; }

        [Display(Name = "Nhà cung cấp")]
        [Required(ErrorMessage = "Nhà cung cấp không được để trống")]
        public string MaNCC { get; set; } //  dùng mã NCC để binding

        [Display(Name = "Tên Công Ty")]
        public string? TenCongTy { get; set; } // nullable, không bị validate

        [Display(Name = "Mô tả")]
        public string? MoTa { get; set; } //  optional

        [Display(Name = "Đơn giá")]
        [Required(ErrorMessage = "Đơn giá không được để trống")]
        [Range(1000, double.MaxValue, ErrorMessage = "Đơn giá phải lớn hơn 1000")]
        public decimal? DonGia { get; set; }

        [Display(Name = "Hình ảnh")]
        public IFormFile? Hinh { get; set; } // optional, tránh lỗi nếu không chọn file
    }

    public class HangHoaEditViewModel : HangHoaCreateViewModel
    {
        public int MaHh { get; set; }
        public string? ExistingHinh { get; set; } // nullable để tránh lỗi nếu chưa có hình
    }
}
