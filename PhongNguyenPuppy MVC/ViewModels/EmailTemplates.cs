namespace PhongNguyenPuppy_MVC.ViewModels.EmailTemplates
{
    public class EmailVerifyVM
    {
        public string HoTen { get; set; } = "";
        public string VerifyLink { get; set; } = "";
    }
    public class EmailPasswordResetVM
    {
        public string HoTen { get; set; } = "";
        public string ResetLink { get; set; } = "";
    }
}