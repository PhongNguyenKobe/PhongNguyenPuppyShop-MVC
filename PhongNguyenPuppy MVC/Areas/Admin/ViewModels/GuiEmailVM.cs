namespace PhongNguyenPuppy_MVC.Areas.Admin.ViewModels
{
    public class GuiEmailVM
    {
        public string TieuDe { get; set; }
        public string NoiDung { get; set; }
        public List<string> DanhSachEmail { get; set; } = new List<string>();
        public string EmailBoSung { get; set; }
        public string TinyMceApiKey { get; set; }

        // Danh sách sản phẩm để chọn
        public List<ProductEmailVM> DanhSachSanPham { get; set; } = new List<ProductEmailVM>();

        // Danh sách ID sản phẩm được chọn
        public List<int> SanPhamDuocChon { get; set; } = new List<int>();
    }

    // ViewModel cho sản phẩm trong email
    public class ProductEmailVM
    {
        public int MaHh { get; set; }
        public string TenHh { get; set; }
        public double DonGia { get; set; }
        public string Hinh { get; set; }
        public string MoTaNgan { get; set; }
        public double? GiamGia { get; set; }

        // Giá sau giảm
        public double GiaSauGiam => GiamGia.HasValue && GiamGia > 0
            ? DonGia * (1 - GiamGia.Value / 100)
            : DonGia;
    }

    public class EmailTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string HtmlContent { get; set; }
        public string Category { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}