namespace PhongNguyenPuppy_MVC.Areas.Admin.ViewModels
{
    public class GuiEmailVM
    {
        public string TieuDe { get; set; }
        public string NoiDung { get; set; }
        public List<string> DanhSachEmail { get; set; } = new List<string>();
    }

}
