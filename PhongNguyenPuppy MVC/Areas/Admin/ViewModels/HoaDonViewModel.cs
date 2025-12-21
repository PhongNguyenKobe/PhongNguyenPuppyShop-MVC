namespace PhongNguyenPuppy_MVC.Areas.Admin.ViewModels
{
    public class HoaDonViewModel
    {
        public int MaHd { get; set; }
        public string HoTen { get; set; }
        public DateTime NgayDat { get; set; }
        public string TrangThai { get; set; }
        public double TongTien { get; set; }
        public double GiamGia { get; set; }
    }

    public class HoaDonDetailsViewModel
    {
        public int MaHd { get; set; }
        public string MaKh { get; set; }
        public string HoTen { get; set; }
        public DateTime NgayDat { get; set; }
        public string TrangThai { get; set; }
        public string DiaChi { get; set; }
        public string? ProvinceName { get; set; }
        public string? DistrictName { get; set; }
        public string? WardName { get; set; }
        public double PhiVanChuyen { get; set; }
        public string? GhiChu { get; set; }
        public string DienThoai { get; set; }
        public string Email { get; set; }
        public double TongTien { get; set; }
        public string MaGiamGia { get; set; }
        public double GiamGia { get; set; }

        // THÊM MỚI: Transaction ID và Order ID từ cổng thanh toán
        public string? TransactionId { get; set; }
        public string? PaymentGatewayOrderId { get; set; }
        public string? CachThanhToan { get; set; } // Để biết thanh toán qua COD/VNPay/PayPal

        public List<ChiTietHdViewModel> ChiTietHds { get; set; } = new();
    }

    public class ChiTietHdViewModel
    {
        public int MaHh { get; set; } 
        public string TenHh { get; set; }
        public double DonGia { get; set; }
        public int SoLuong { get; set; }
        public double GiamGia { get; set; }
        public string MaGiamGia { get; set; }
    }

    public class HoaDonPagedViewModel
    {
        public List<HoaDonViewModel> HoaDons { get; set; }
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public string TuKhoa { get; set; }
    }
}