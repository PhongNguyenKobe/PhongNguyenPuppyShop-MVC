using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // Để upload hình ảnh

namespace PhongNguyenPuppy_MVC.ViewModels
{
    public class HangHoaAdmin
    {
        public int MaHh { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [StringLength(50)]
        public string TenHh { get; set; }

        [StringLength(50)]
        public string TenAlias { get; set; }

        [Required(ErrorMessage = "Loại sản phẩm không được để trống")]
        public int MaLoai { get; set; }

        [StringLength(50)]
        public string MoTaDonVi { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá phải lớn hơn 0")]
        public double? DonGia { get; set; }

        public IFormFile Hinh { get; set; } // Để upload hình ảnh mới
        public string ExistingHinh { get; set; } // Hình hiện tại khi sửa

        [Required(ErrorMessage = "Ngày sản xuất không được để trống")]
        public DateTime NgaySx { get; set; }

        [Range(0, 100, ErrorMessage = "Giảm giá từ 0-100%")]
        public double GiamGia { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lần xem phải lớn hơn 0")]
        public int SoLanXem { get; set; }

        public string MoTa { get; set; }

        [Required(ErrorMessage = "Nhà cung cấp không được để trống")]
        [StringLength(50)]
        public string MaNcc { get; set; }
    }
}