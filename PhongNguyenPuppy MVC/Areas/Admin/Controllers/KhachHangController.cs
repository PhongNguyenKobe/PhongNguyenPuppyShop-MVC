using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PhongNguyenPuppy_MVC.Areas.Admin.Helpers;
using PhongNguyenPuppy_MVC.Areas.Admin.Services;
using PhongNguyenPuppy_MVC.Areas.Admin.ViewModels;
using PhongNguyenPuppy_MVC.Models; // Model KhachHang
using PhongNguyenPuppy_MVC.Services; // Dịch vụ gửi mail, thống kê

namespace PhongNguyenPuppy_MVC.Areas.Admin.Controllers

{
    [Area("Admin")]
    public class KhachHangController : Controller
    {
        private readonly IKhachHangRepository _khachHangRepository;
        private readonly IDichVuGuiEmail _dichVuGuiEmail;
        private readonly IDichVuThongKe _dichVuThongKe;
        private readonly IOptions<TinyMceSettings> _tinyMceSettings;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public KhachHangController(
            IKhachHangRepository khachHangRepository,
            IDichVuGuiEmail dichVuGuiEmail,
            IDichVuThongKe dichVuThongKe,
            IOptions<TinyMceSettings> tinyMceSettings,
            IWebHostEnvironment webHostEnvironment)
        {
            _khachHangRepository = khachHangRepository;
            _dichVuGuiEmail = dichVuGuiEmail;
            _dichVuThongKe = dichVuThongKe;
            _tinyMceSettings = tinyMceSettings;
            _webHostEnvironment = webHostEnvironment;
        }

        // 1. Hiển thị danh sách khách hàng
        public IActionResult DanhSach(string tuKhoa = "", int trang = 1)
        {
            var ketQua = _khachHangRepository.LayTatCa(tuKhoa, trang);

            var vm = new DanhSachKhachHangVM
            {
                DanhSach = ketQua.Items.Select(kh => new KhachHangVM
                {
                    MaKh = kh.MaKh,
                    HoTen = kh.HoTen,
                    Email = kh.Email,
                    DienThoai = kh.DienThoai,
                    TrangThai = kh.HieuLuc
                }).ToList(),
                TuKhoa = tuKhoa,
                TrangHienTai = ketQua.TrangHienTai,
                TongSoTrang = ketQua.TongSoTrang
            };

            return View(vm);
        }

        // 2. Gửi email cho khách hàng
        [HttpGet]
        public IActionResult GuiEmail()
        {
            var vm = new GuiEmailVM
            {
                DanhSachEmail = _khachHangRepository.LayDanhSachEmailKhachHang(),
                TinyMceApiKey = _tinyMceSettings.Value.ApiKey
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> GuiEmail(GuiEmailVM model)
        {
            try
            {
                // Danh sách email từ khách hàng
                var danhSachEmailKhachHang = model.DanhSachEmail ?? new List<string>();

                // Xử lý email bổ sung
                var emailBoSungList = new List<string>();
                if (!string.IsNullOrEmpty(model.EmailBoSung))
                {
                    emailBoSungList = model.EmailBoSung
                        .Split(',')
                        .Select(e => e.Trim())
                        .Where(e => !string.IsNullOrEmpty(e) && e.Contains("@"))
                        .ToList();
                }

                // Gộp cả hai danh sách
                var allEmails = danhSachEmailKhachHang
                    .Union(emailBoSungList)
                    .Distinct()
                    .ToList();

                if (allEmails.Count == 0)
                {
                    TempData["ThongBao"] = "❌ Vui lòng chọn hoặc nhập email!";
                    return RedirectToAction("GuiEmail");
                }

                // Gửi email
                await _dichVuGuiEmail.GuiEmailAsync(allEmails, model.TieuDe, model.NoiDung);

                TempData["ThongBao"] = $"✅ Đã gửi email thành công tới {allEmails.Count} địa chỉ!";
                return RedirectToAction("GuiEmail");
            }
            catch (Exception ex)
            {
                TempData["ThongBao"] = $"❌ Lỗi: {ex.Message}";
                return RedirectToAction("GuiEmail");
            }
        }

        // Upload ảnh cho TinyMCE - Trả URL tuyệt đối
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { error = "Vui lòng chọn ảnh" });

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
                return Json(new { error = "Chỉ hỗ trợ ảnh JPG, PNG, GIF, WebP" });

            const long maxFileSize = 5 * 1024 * 1024;
            if (file.Length > maxFileSize)
                return Json(new { error = "Kích thước ảnh không được vượt quá 5MB" });

            try
            {
                var uploadDirectory = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "email-images");
                if (!Directory.Exists(uploadDirectory))
                    Directory.CreateDirectory(uploadDirectory);

                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadDirectory, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Trả URL tuyệt đối (không URL tương đối)
                var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
                var imageUrl = $"{baseUrl}/uploads/email-images/{fileName}";

                return Json(new { location = imageUrl });
            }
            catch (Exception ex)
            {
                return Json(new { error = $"Lỗi upload ảnh: {ex.Message}" });
            }
        }

        // 3. Chi tiết khách hàng
        [HttpGet]
        public IActionResult ChiTiet(string id)
        {
            var kh = _khachHangRepository.LayTheoId(id);
            if (kh == null) return NotFound();

            var vm = new SuaKhachHangVM
            {
                MaKh = kh.MaKh,
                HoTen = kh.HoTen,
                Email = kh.Email,
                DienThoai = kh.DienThoai,
                HieuLuc = kh.HieuLuc
            };

            return View(vm);
        }

        [HttpPost]
        public IActionResult ChiTiet(SuaKhachHangVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            _khachHangRepository.CapNhat(vm);
            TempData["ThongBao"] = "Thông tin khách hàng đã được cập nhật!";
            return RedirectToAction("ChiTiet", new { id = vm.MaKh });
        }

        // 4. Xóa khách hàng
        [HttpGet]
        public IActionResult Xoa(string id)
        {
            var kh = _khachHangRepository.LayTheoId(id);
            if (kh == null)
            {
                TempData["ThongBao"] = "❌ Không tìm thấy khách hàng để xóa.";
                return RedirectToAction("DanhSach");
            }

            _khachHangRepository.Xoa(id);
            TempData["ThongBao"] = $"🗑️ Đã xóa khách hàng \"{kh.HoTen}\" thành công!";
            return RedirectToAction("DanhSach");
        }

        // 5. Gửi email cá nhân
        [HttpPost]
        public IActionResult GuiEmailCaNhan(string MaKh, string Email, string TieuDeEmail, string NoiDungEmail)
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(TieuDeEmail) || string.IsNullOrEmpty(NoiDungEmail))
            {
                TempData["ThongBao"] = "❌ Vui lòng nhập đầy đủ thông tin email.";
                return RedirectToAction("ChiTiet", new { id = MaKh });
            }

            _dichVuGuiEmail.Gui(Email, TieuDeEmail, NoiDungEmail);
            TempData["ThongBao"] = $"✅ Đã gửi email đến khách hàng \"{Email}\".";
            return RedirectToAction("ChiTiet", new { id = MaKh });
        }
    }
}