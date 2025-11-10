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
using PhongNguyenPuppy_MVC.Services;
using PhongNguyenPuppy_MVC.ViewModels;

namespace PhongNguyenPuppy_MVC.Controllers
{
    //[Authorize(AuthenticationSchemes = "CustomerScheme")] bị lỗi vòng lặp URL
    public class KhachHangController : Controller
    {
        private readonly PhongNguyenPuppyContext db;
        private readonly IWebHostEnvironment _env;
        private readonly MyEmailHelper _emailHelper;
        private readonly IGHNService _ghnService;
        private const int PageSize = 10;
        public KhachHangController(PhongNguyenPuppyContext context, IWebHostEnvironment env, MyEmailHelper emailHelper, IGHNService ghnService)
        {
            db = context;
            _env = env;
            _emailHelper = emailHelper;
            _ghnService = ghnService;
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
                    TongTien = h.ChiTietHds.Sum(ct => ct.DonGia * ct.SoLuong) - (h.GiamGia) + (h.PhiVanChuyen), // Tính lại chính xác
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
                    TongTien = (float)(h.ChiTietHds.Sum(ct => ct.DonGia * ct.SoLuong) - (h.GiamGia) + (h.PhiVanChuyen)),
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

            //LẤY CẢ HOADON VÀ KHACHHANG
            var hoaDon = await db.HoaDons
                .Include(h => h.MaKhNavigation)
                .FirstOrDefaultAsync(h => h.MaHd == id);

            if (hoaDon != null)
            {
                // FALLBACK: Ưu tiên HoaDon, nếu không có thì lấy từ KhachHang
                // (Phòng trường hợp SQL UPDATE bị lỗi hoặc khách hàng chưa có địa chỉ)
                int? provinceId = hoaDon.ProvinceId ?? hoaDon.MaKhNavigation?.ProvinceId;
                int? districtId = hoaDon.DistrictId ?? hoaDon.MaKhNavigation?.DistrictId;
                string? wardCode = hoaDon.WardCode ?? hoaDon.MaKhNavigation?.WardCode;

                // Chỉ gọi API nếu có dữ liệu hợp lệ
                if (provinceId.HasValue && provinceId.Value > 0)
                {
                    try
                    {
                        var provinces = await _ghnService.GetProvincesAsync();
                        hoaDonVM.ProvinceName = provinces?.FirstOrDefault(p => p.ProvinceID == provinceId)?.ProvinceName;
                    }
                    catch (Exception ex)
                    {
                        // Log lỗi nhưng không crash ứng dụng
                        Console.WriteLine($"Lỗi lấy tên tỉnh: {ex.Message}");
                    }
                }

                if (districtId.HasValue && districtId.Value > 0 && provinceId.HasValue)
                {
                    try
                    {
                        var districts = await _ghnService.GetDistrictsAsync(provinceId.Value);
                        hoaDonVM.DistrictName = districts?.FirstOrDefault(d => d.DistrictID == districtId)?.DistrictName;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi lấy tên quận: {ex.Message}");
                    }
                }

                if (!string.IsNullOrEmpty(wardCode) && districtId.HasValue)
                {
                    try
                    {
                        var wards = await _ghnService.GetWardsAsync(districtId.Value);
                        hoaDonVM.WardName = wards?.FirstOrDefault(w => w.WardCode == wardCode)?.WardName;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi lấy tên phường: {ex.Message}");
                    }
                }
            }

            return View(hoaDonVM);
        }

        #region Cập nhật địa chỉ GHN
        [Authorize(AuthenticationSchemes = "CustomerScheme")]
        [HttpGet]
        public async Task<IActionResult> UpdateAddress()
        {
            var maKh = User.FindFirstValue("CustomerID");
            if (string.IsNullOrEmpty(maKh)) return RedirectToAction("DangNhap");

            var kh = await db.KhachHangs.FindAsync(maKh);
            if (kh == null) return NotFound();

            // Lấy danh sách tỉnh từ GHN
            var provinces = await _ghnService.GetProvincesAsync();
            ViewBag.Provinces = provinces;

            var model = new UpdateAddressVM
            {
                HoTen = kh.HoTen,
                DiaChi = kh.DiaChi,
                DienThoai = kh.DienThoai,
                ProvinceId = kh.ProvinceId,
                DistrictId = kh.DistrictId,
                WardCode = kh.WardCode
            };

            return View(model);
        }

        [Authorize(AuthenticationSchemes = "CustomerScheme")]
        [HttpPost]
        public async Task<IActionResult> UpdateAddress(UpdateAddressVM model)
        {
            if (!ModelState.IsValid)
            {
                var provinces = await _ghnService.GetProvincesAsync();
                ViewBag.Provinces = provinces;
                return View(model);
            }

            var maKh = User.FindFirstValue("CustomerID");
            if (string.IsNullOrEmpty(maKh)) return RedirectToAction("DangNhap");

            var kh = await db.KhachHangs.FindAsync(maKh);
            if (kh == null) return NotFound();

            // Cập nhật thông tin
            kh.HoTen = model.HoTen;
            kh.DiaChi = model.DiaChi;
            kh.DienThoai = model.DienThoai;
            kh.ProvinceId = model.ProvinceId;
            kh.DistrictId = model.DistrictId;
            kh.WardCode = model.WardCode;

            await db.SaveChangesAsync();

            TempData["Success"] = "Cập nhật địa chỉ thành công!";
            return RedirectToAction("Profile");
        }

        // API để lấy danh sách quận/huyện
        [HttpGet]
        public async Task<IActionResult> GetDistricts(int provinceId)
        {
            var districts = await _ghnService.GetDistrictsAsync(provinceId);
            return Json(districts);
        }

        // API để lấy danh sách phường/xã
        [HttpGet]
        public async Task<IActionResult> GetWards(int districtId)
        {
            var wards = await _ghnService.GetWardsAsync(districtId);
            return Json(wards);
        }
        #endregion

        [Authorize(AuthenticationSchemes = "CustomerScheme")]
        public async Task<IActionResult> DangXuat()
        {
            await HttpContext.SignOutAsync("CustomerScheme");
            return RedirectToAction("Index", "Home");
        }
    }
}
