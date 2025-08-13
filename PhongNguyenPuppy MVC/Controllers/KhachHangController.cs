using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhongNguyenPuppy_MVC.Areas.Admin.ViewModels;
using PhongNguyenPuppy_MVC.Data;
using PhongNguyenPuppy_MVC.Helpers;
using PhongNguyenPuppy_MVC.Models; // nếu có entity KhachHang
using PhongNguyenPuppy_MVC.ViewModels;

namespace PhongNguyenPuppy_MVC.Controllers
{
    [Authorize(AuthenticationSchemes = "CustomerScheme")]
    public class KhachHangController : Controller
    {
        private readonly PhongNguyenPuppyContext db;
        private readonly IWebHostEnvironment _env;
        private readonly MyEmailHelper _emailHelper;
        private const int PageSize = 10;
        public KhachHangController(PhongNguyenPuppyContext context, IWebHostEnvironment env, MyEmailHelper emailHelper)
        {
            db = context;
            _env = env;
            _emailHelper = emailHelper;
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

            if (db.KhachHangs.Any(k => k.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng.");
                return View(model);
            }

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
        new Claim(ClaimTypes.Role, "Customer"),
        new Claim("Avatar", khachHang.Hinh ?? "default-avatar.png")
    };

            // Quan trọng: AuthenticationType phải trùng scheme
            var claimsIdentity = new ClaimsIdentity(claims, "CustomerScheme");
            var principal = new ClaimsPrincipal(claimsIdentity);

            // Đăng nhập đúng scheme khách hàng
            await HttpContext.SignInAsync("CustomerScheme", principal);

            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            return RedirectToAction("Profile", "KhachHang");
        }

        [Authorize(AuthenticationSchemes = "CustomerScheme")]
        #endregion

        #region Quên mật khẩu

        [HttpGet]
        [AllowAnonymous]
        public IActionResult QuenMatKhau()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> QuenMatKhau(ForgotPasswordVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var kh = await db.KhachHangs.SingleOrDefaultAsync(k => k.Email == model.Email);
            if (kh == null)
            {
                ModelState.AddModelError("Email", "Email không tồn tại.");
                return View(model);
            }

            // Tạo token ngẫu nhiên
            string token = Guid.NewGuid().ToString();

            // Lưu token và thời hạn
            kh.ResetToken = token;
            kh.ResetTokenExpiry = DateTime.Now.AddHours(1);
            await db.SaveChangesAsync();

            // Tạo link đặt lại mật khẩu
            string resetLink = Url.Action("DatLaiMatKhau", "KhachHang", new { token }, Request.Scheme);

            // Gửi email
            string subject = "Yêu cầu đặt lại mật khẩu";
            string body = $@"
        <p>Xin chào {kh.HoTen},</p>
        <p>Bạn đã yêu cầu đặt lại mật khẩu. Bấm vào liên kết bên dưới để thực hiện:</p>
        <p><a href='{resetLink}'>Đặt lại mật khẩu</a></p>
        <p>Liên kết sẽ hết hạn sau 1 giờ.</p>";

            await _emailHelper.SendMailAsync(kh.Email, subject, body);

            TempData["Success"] = "Liên kết đặt lại mật khẩu đã được gửi đến email.";
            return RedirectToAction("DangNhap");
        }

        #endregion


        #region dat lai mat khau
        [HttpGet]
        public IActionResult DatLaiMatKhau(string token)
        {
            var kh = db.KhachHangs.SingleOrDefault(k => k.ResetToken == token && k.ResetTokenExpiry > DateTime.Now);
            if (kh == null)
            {
                TempData["Error"] = "Liên kết không hợp lệ hoặc đã hết hạn.";
                return RedirectToAction("DangNhap");
            }

            return View(new ResetPasswordVM { Token = token });
        }

        [HttpPost]
        public async Task<IActionResult> DatLaiMatKhau(ResetPasswordVM model)
        {
            if (!ModelState.IsValid) return View(model);

            var kh = db.KhachHangs.SingleOrDefault(k => k.ResetToken == model.Token && k.ResetTokenExpiry > DateTime.Now);
            if (kh == null)
            {
                TempData["Error"] = "Liên kết không hợp lệ hoặc đã hết hạn.";
                return RedirectToAction("DangNhap");
            }

            kh.MatKhau = model.NewPassword.ToMd5Hash(kh.RandomKey);
            kh.ResetToken = null;
            kh.ResetTokenExpiry = null;
            await db.SaveChangesAsync();

            // Chuyển đến view xác nhận, truyền username
            return View("DatLaiMatKhauThanhCong", kh.MaKh); 
        }

        #endregion

        //chức năng xem thông tin cá nhân và lịch sử mua hàng
        public async Task<IActionResult> Profile(string? tuKhoa, int? nam, int page = 1)
        {
            var maKh = User.FindFirstValue("CustomerID");
            if (string.IsNullOrEmpty(maKh)) return RedirectToAction("DangNhap");

            var query = db.HoaDons
                .Include(h => h.MaTrangThaiNavigation)
                .Include(h => h.ChiTietHds)
                .Where(h => h.MaKh == maKh)
                .Select(h => new HoaDonViewModel
                {
                    MaHd = h.MaHd,
                    HoTen = h.HoTen,
                    NgayDat = h.NgayDat,
                    TrangThai = h.MaTrangThaiNavigation.TenTrangThai,
                    TongTien = h.ChiTietHds.Sum(ct => (ct.DonGia * ct.SoLuong) * (1 - ct.GiamGia / 100)) - h.GiamGia + h.PhiVanChuyen,
                    GiamGia = h.GiamGia
                });

            if (!string.IsNullOrEmpty(tuKhoa))
            {
                query = query.Where(h =>
                    h.MaHd.ToString().Contains(tuKhoa) ||
                    h.TrangThai.Contains(tuKhoa));
            }

            if (nam.HasValue)
            {
                query = query.Where(h => h.NgayDat.Year == nam.Value);
            }

            var totalItems = await query.CountAsync();
            var hoaDons = await query
                .OrderByDescending(h => h.NgayDat)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var vm = new HoaDonPagedViewModel
            {
                HoaDons = hoaDons,
                PageNumber = page,
                TotalPages = (int)Math.Ceiling((double)totalItems / PageSize),
                TuKhoa = tuKhoa
            };

            ViewBag.Nam = nam;

            return View(vm);
        }

        //chức năng xem chi tiết hóa đơn
        [Authorize(AuthenticationSchemes = "CustomerScheme")]
        public async Task<IActionResult> Details(int id)
        {
            var hoaDonVM = await db.HoaDons
                            .Include(h => h.MaKhNavigation)
                .Include(h => h.MaTrangThaiNavigation)
                .Include(h => h.ChiTietHds)
                    .ThenInclude(ct => ct.MaHhNavigation)
                .Where(h => h.MaHd == id)
                .Select(h => new HoaDonDetailsViewModel
                {
                    MaHd = h.MaHd,
                    MaKh = h.MaKh,
                    HoTen = h.HoTen ?? h.MaKhNavigation.HoTen,
                    NgayDat = h.NgayDat,
                    TrangThai = h.MaTrangThaiNavigation.TenTrangThai,
                    DiaChi = h.DiaChi,
                    PhiVanChuyen = h.PhiVanChuyen,
                    GhiChu = h.GhiChu,
                    GiamGia = h.GiamGia,
                    TongTien = (float)h.ChiTietHds.Sum(ct => (ct.DonGia * ct.SoLuong) * (1 - ct.GiamGia / 100)) - h.GiamGia + h.PhiVanChuyen,
                    DienThoai = h.MaKhNavigation.DienThoai,
                    Email = h.MaKhNavigation.Email,
                    ChiTietHds = h.ChiTietHds.Select(ct => new ChiTietHdViewModel
                    {
                        TenHh = ct.MaHhNavigation.TenHh,
                        DonGia = ct.DonGia,
                        SoLuong = ct.SoLuong,
                        GiamGia = ct.GiamGia,
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (hoaDonVM == null)
                return NotFound();

            return View(hoaDonVM);
        }



        [Authorize(AuthenticationSchemes = "CustomerScheme")]
        public async Task<IActionResult> DangXuat()
        {
            await HttpContext.SignOutAsync("CustomerScheme");
            return RedirectToAction("Index", "Home");
        }
    }
}
