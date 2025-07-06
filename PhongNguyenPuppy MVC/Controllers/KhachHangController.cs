using Microsoft.AspNetCore.Mvc;
using PhongNguyenPuppy_MVC.Data;
using PhongNguyenPuppy_MVC.ViewModels;
using PhongNguyenPuppy_MVC.Models; // nếu có entity KhachHang
using System.IO;
using PhongNguyenPuppy_MVC.Helpers;

namespace PhongNguyenPuppy_MVC.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly PhongNguyenPuppyContext db;
        private readonly IWebHostEnvironment _env;

        public KhachHangController(PhongNguyenPuppyContext context, IWebHostEnvironment env)
        {
            db = context;
            _env = env;
        }

        #region Register in
        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DangKy(RegisterVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? imgPath = null;

            // Lưu ảnh nếu có upload  
            if (model.Hinh != null)
            {
                string? fileName = MyUtil.UploadHinh(model.Hinh, "");
                imgPath = fileName != null ? $"/Hinh/KhachHang/{fileName}" : null;
            }

            // Mapping từ RegisterVM sang entity KhachHang  
            var kh = new KhachHang
            {
                MaKh = model.MaKh,
                MatKhau = model.MatKhau ?? string.Empty, // Ensure MatKhau is not null  
                HoTen = model.HoTen,
                GioiTinh = model.GioiTinh,
                NgaySinh = model.NgaySinh.HasValue ? model.NgaySinh.Value : DateTime.MinValue, // Explicit conversion and default value  
                DiaChi = model.DiaChi,
                DienThoai = model.DienThoai,
                Email = model.Email,
                Hinh = imgPath
            };

            kh.RandomKey = MyUtil.GetRandomKey();
            kh.MatKhau = kh.MatKhau.ToMd5Hash(kh.RandomKey); // MatKhau is guaranteed to be non-null  
            kh.HieuLuc = true; // Mặc định là hiệu lực, mặc định xử lý khi dùng Mail để Active  

            db.KhachHangs.Add(kh);
            await db.SaveChangesAsync();

            TempData["Success"] = "Đăng ký thành công!";
            return RedirectToAction("DangNhap");
        }
        #endregion



        #region Login in
        [HttpGet]
        public IActionResult DangNhap(string? ReturnUrl)
        {
            ViewBag.ReturnUrl = ReturnUrl;
            return View();
        }

        #endregion
    }
}

