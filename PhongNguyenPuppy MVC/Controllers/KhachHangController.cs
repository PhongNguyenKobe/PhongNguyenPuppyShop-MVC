using System;
using System.Security.Claims;
using Azure.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhongNguyenPuppy_MVC.Areas.Admin.ViewModels;
using PhongNguyenPuppy_MVC.Data;
using PhongNguyenPuppy_MVC.Helpers;
using PhongNguyenPuppy_MVC.Models;
using PhongNguyenPuppy_MVC.Services;
using PhongNguyenPuppy_MVC.ViewModels;
using PhongNguyenPuppy_MVC.ViewModels.EmailTemplates;

namespace PhongNguyenPuppy_MVC.Controllers
{
    //[Authorize(AuthenticationSchemes = "CustomerScheme")] bị lỗi vòng lặp URL
    public class KhachHangController : Controller
    {
        private readonly PhongNguyenPuppyContext db;
        private readonly IWebHostEnvironment _env;
        private readonly MyEmailHelper _emailHelper;
        private readonly IGHNService _ghnService;
        private readonly IConfiguration _configuration;
        private readonly IViewRenderService _viewRenderService;
        private const int PageSize = 10;
        public KhachHangController(PhongNguyenPuppyContext context, IWebHostEnvironment env, MyEmailHelper emailHelper, IGHNService ghnService, IConfiguration configuration, IViewRenderService viewRenderService)
        {
            db = context;
            _env = env;
            _emailHelper = emailHelper;
            _ghnService = ghnService;
            _configuration = configuration;
            _viewRenderService = viewRenderService;
        }

        private string GetAbsoluteUrl(string actionName, string controllerName, object routeValues = null)
        {
            string baseUrl = _configuration["AppSettings:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
            baseUrl = baseUrl.TrimEnd('/');
            string relativePath = Url.Action(actionName, controllerName, routeValues)!;
            return $"{baseUrl}{relativePath}";
        }

        #region Register in
        [HttpGet]
        public async Task<IActionResult> DangKy()
        {
            // Load danh sách tỉnh từ GHN service
            try
            {
                var provinces = await _ghnService.GetProvincesAsync();
                ViewBag.Provinces = provinces;
            }
            catch (Exception ex)
            {
                ViewBag.Provinces = new List<GHNProvince>();
                ViewBag.ErrorMessage = "Không thể tải danh sách tỉnh/thành phố. Vui lòng thử lại sau.";
                Console.WriteLine($"Lỗi load provinces: {ex.Message}");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DangKy(RegisterVM model)
        {
            if (!ModelState.IsValid)
            {
                //Reload lại danh sách tỉnh khi có lỗi validation
                try
                {
                    var provinces = await _ghnService.GetProvincesAsync();
                    ViewBag.Provinces = provinces;
                }
                catch
                {
                    ViewBag.Provinces = new List<GHNProvince>();
                }
                return View(model);
            }

            string? imgPath = null;

            if (db.KhachHangs.Any(k => k.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng.");
                // Reload provinces
                try
                {
                    var provinces = await _ghnService.GetProvincesAsync();
                    ViewBag.Provinces = provinces;
                }
                catch
                {
                    ViewBag.Provinces = new List<GHNProvince>();
                }
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
                MatKhau = model.MatKhau ?? string.Empty,
                HoTen = model.HoTen,
                GioiTinh = model.GioiTinh,
                NgaySinh = model.NgaySinh.HasValue ? model.NgaySinh.Value : DateTime.MinValue,
                DiaChi = model.DiaChi,
                DienThoai = model.DienThoai,
                Email = model.Email,
                Hinh = imgPath
            };

            kh.RandomKey = MyUtil.GetRandomKey();
            kh.MatKhau = kh.MatKhau.ToMd5Hash(kh.RandomKey);

            // THAY ĐỔI: Chưa kích hoạt tài khoản, cần xác thực email
            kh.HieuLuc = false;
            kh.ResetToken = Guid.NewGuid().ToString(); // Token xác thực
            kh.ResetTokenExpiry = DateTime.Now.AddHours(24); // Hết hạn sau 24 giờ

            db.KhachHangs.Add(kh);
            await db.SaveChangesAsync();

            // Gửi email xác thực
            try
            {
                string verifyLink = GetAbsoluteUrl("XacThucEmail", "KhachHang", new { token = kh.ResetToken });
                string subject = "Xác thực tài khoản - Phong Nguyen Puppy Shop";

                var emailModel = new EmailVerifyVM { HoTen = kh.HoTen, VerifyLink = verifyLink };

                // Note: use absolute view path so rendering works from controllers/background tasks
                string body = await _viewRenderService.RenderToStringAsync("/Views/EmailTemplates/VerifyAccount.cshtml", emailModel);

                await _emailHelper.SendMailAsync(kh.Email, subject, body);

                TempData["Success"] = "Đăng ký thành công! Vui lòng kiểm tra email để kích hoạt tài khoản.";
            }
            catch (Exception ex)
            {
                // Nếu gửi email thất bại, xóa user vừa tạo
                db.KhachHangs.Remove(kh);
                await db.SaveChangesAsync();

                ModelState.AddModelError("", $"Đăng ký thất bại. Không thể gửi email xác thực. Lỗi: {ex.Message}");
                return View(model);
            }

            return RedirectToAction("DangNhap");
        }

        // THÊM API endpoint để lấy danh sách tỉnh
        [HttpGet]
        public async Task<IActionResult> GetProvinces()
        {
            try
            {
                var provinces = await _ghnService.GetProvincesAsync();
                return Json(provinces);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message });
            }
        }
        #endregion

        #region Xác thực Email
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> XacThucEmail(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Link xác thực không hợp lệ.";
                return RedirectToAction("DangNhap");
            }

            var kh = await db.KhachHangs.SingleOrDefaultAsync(k =>
                k.ResetToken == token &&
                k.ResetTokenExpiry > DateTime.Now &&
                !k.HieuLuc); // Chỉ lấy tài khoản chưa kích hoạt

            if (kh == null)
            {
                TempData["Error"] = "Link xác thực không hợp lệ hoặc đã hết hạn. Vui lòng đăng ký lại hoặc yêu cầu gửi lại email.";
                return RedirectToAction("DangNhap");
            }

            // Kích hoạt tài khoản
            kh.HieuLuc = true;
            kh.ResetToken = null;
            kh.ResetTokenExpiry = null;
            await db.SaveChangesAsync();

            TempData["Success"] = "✅ Xác thực email thành công! Bạn có thể đăng nhập ngay bây giờ.";
            return RedirectToAction("DangNhap");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GuiLaiEmailXacThuc()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> GuiLaiEmailXacThuc(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Vui lòng nhập email.");
                return View();
            }

            var kh = await db.KhachHangs.SingleOrDefaultAsync(k => k.Email == email && !k.HieuLuc);

            if (kh == null)
            {
                ModelState.AddModelError("", "Email không tồn tại hoặc tài khoản đã được kích hoạt.");
                return View();
            }

            // Tạo token mới
            kh.ResetToken = Guid.NewGuid().ToString();
            kh.ResetTokenExpiry = DateTime.Now.AddHours(24);
            await db.SaveChangesAsync();

            try
            {
                string verifyLink = GetAbsoluteUrl("XacThucEmail", "KhachHang", new { token = kh.ResetToken });
                string subject = "Gửi lại link xác thực tài khoản - Phong Nguyen Puppy Shop";

                var emailModel = new EmailVerifyVM { HoTen = kh.HoTen, VerifyLink = verifyLink };
                string body = await _viewRenderService.RenderToStringAsync("/Views/EmailTemplates/VerifyAccount.cshtml", emailModel);

                await _emailHelper.SendMailAsync(kh.Email, subject, body);

                TempData["Success"] = "Email xác thực đã được gửi lại. Vui lòng kiểm tra hộp thư (bao gồm cả thư mục spam).";
                return RedirectToAction("DangNhap");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Không thể gửi email. Lỗi: {ex.Message}");
                return View();
            }
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
                ViewBag.ShowResendLink = true; // ✅ Hiển thị link gửi lại email
                ViewBag.UserEmail = khachHang.Email;
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
            string resetLink = GetAbsoluteUrl("DatLaiMatKhau", "KhachHang", new { token });
            string subject = "Yêu cầu đặt lại mật khẩu";

            var emailModel = new EmailPasswordResetVM { HoTen = kh.HoTen, ResetLink = resetLink };
            string body = await _viewRenderService.RenderToStringAsync("/Views/EmailTemplates/PasswordReset.cshtml", emailModel);

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