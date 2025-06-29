using System.Text;

namespace PhongNguyenPuppy_MVC.Helpers
{
    public class MyUtil
    {
        public static string UploadHinh(IFormFile Hinh, string folder)
        {
            try
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Hinh", "KhachHang", folder, Hinh.FileName);
                using (var myFile = new FileStream(fullPath, FileMode.Create))
                {
                    Hinh.CopyTo(myFile);
                }
                return Hinh.FileName;
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        public static string GetRandomKey(int length = 5)
        {
            var pattern = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var sb = new StringBuilder();
            Random random = new Random();
            for (int i = 0; i < length; i++)
            {
                sb.Append(pattern[random.Next(pattern.Length)]);
            }
            return sb.ToString();
        }
    }
}
