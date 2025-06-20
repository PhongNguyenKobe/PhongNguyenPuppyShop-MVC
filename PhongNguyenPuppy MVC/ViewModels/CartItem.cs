namespace PhongNguyenPuppy_MVC.ViewModels
{
    public class CartItem
    {
        public int MaHh { get; set; }
        public string TenHH { get; set; }
        public string Hinh { get; set; }
        public int SoLuong { get; set; }
        public double DonGia { get; set; }
        public double ThanhTien
        {
            get { return SoLuong * DonGia; }
        }
    }
}
