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

    // New: order info
    public class EmailOrderInfoItemVM
    {
        public string TenHh { get; set; } = "";
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien { get; set; }
        public string ImageUrl { get; set; } = "";

    }

    public class EmailOrderInfoVM
    {
        public string HoTen { get; set; } = "";
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public List<EmailOrderInfoItemVM> Items { get; set; } = new();
        public decimal Total { get; set; }
        public string Address { get; set; } = "";
        public string Province { get; set; } = "";
        public string District { get; set; } = "";
        public string Ward { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Note { get; set; } = "";
        public string OrderLink { get; set; } = "";
    }
}