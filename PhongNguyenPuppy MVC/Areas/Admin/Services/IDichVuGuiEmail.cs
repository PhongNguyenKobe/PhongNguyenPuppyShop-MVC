namespace PhongNguyenPuppy_MVC.Areas.Admin.Services
{
    public interface IDichVuGuiEmail
    {
        void Gui(string emailNhan, string tieuDe, string noiDung);
        Task GuiEmailAsync(List<string> danhSachEmail, string tieuDe, string noiDung);
    }
}