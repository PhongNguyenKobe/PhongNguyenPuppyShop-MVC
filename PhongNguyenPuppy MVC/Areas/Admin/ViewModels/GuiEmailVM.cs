namespace PhongNguyenPuppy_MVC.Areas.Admin.ViewModels
{
    public class GuiEmailVM
    {
        public string TieuDe { get; set; }
        public string NoiDung { get; set; }
        public List<string> DanhSachEmail { get; set; } = new List<string>();
        public string EmailBoSung { get; set; }

        public string TinyMceApiKey { get; set; } // API Key từ controller
    }
    public class EmailTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; } // "Khuyến mãi", "Thông báo sản phẩm mới", etc.
        public string Description { get; set; }
        public string HtmlContent { get; set; }
        public string Category { get; set; } // "Promotion", "Product", "Welcome", etc.
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}