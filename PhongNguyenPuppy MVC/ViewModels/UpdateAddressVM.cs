namespace PhongNguyenPuppy_MVC.ViewModels
{
    public class UpdateAddressVM
    {
        public string HoTen { get; set; } = string.Empty;
        public string? DiaChi { get; set; }
        public string? DienThoai { get; set; }
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public string? WardCode { get; set; }
    }
}