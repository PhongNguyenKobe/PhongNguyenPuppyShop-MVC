using Microsoft.AspNetCore.Mvc;
using PhongNguyenPuppy_MVC.Data;
using PhongNguyenPuppy_MVC.ViewModels;
using PhongNguyenPuppy_MVC.Models; // nếu có entity KhachHang
using System.IO;
using PhongNguyenPuppy_MVC.Helpers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

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



        #region Login
        [HttpGet]
        public IActionResult DangNhap(string? ReturnUrl)
        {
            ViewBag.ReturnUrl = ReturnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DangNhap(LoginVM model, string? ReturnUrl)
        {
            ViewBag.ReturnUrl = ReturnUrl;

           
            var khachHang = db.KhachHangs.SingleOrDefault(kh => kh.MaKh == model.UserName);
            if (khachHang == null)
            {
                ModelState.AddModelError(nameof(model.UserName), "Tên đăng nhập không tồn tại.");
                return View(model);
            }

            if (!khachHang.HieuLuc)
            {
                ModelState.AddModelError("Lỗi", "Tài khoản của bạn chưa được kích hoạt. Vui lòng kiểm tra email để kích hoạt tài khoản.");
                return View(model);
            }

            string hashedPassword = model.Password.ToMd5Hash(khachHang.RandomKey);
            if (khachHang.MatKhau != hashedPassword)
            {
                ModelState.AddModelError(nameof(model.Password), "Sai thông tin đăng nhập.");
                return View(model);
            }

            // Xây dựng Claims
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Email, khachHang.Email ?? ""),
        new Claim(ClaimTypes.Name, khachHang.HoTen ?? ""),
        new Claim(MySetting.CLAIM_CUSTOMERID, khachHang.MaKh),
        new Claim(ClaimTypes.Role, "Customer")
    };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(claimsIdentity);

            // Đăng nhập
            await HttpContext.SignInAsync(principal);

            // Điều hướng theo ReturnUrl nếu hợp lệ
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            return RedirectToAction("Profile", "KhachHang");
        }
        #endregion

        [Authorize]
        public IActionResult Profile()
        {
            return View();
        }
        [Authorize]
        public async Task<IActionResult> DangXuat()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
