using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhongNguyenPuppy_MVC.Areas.Admin.Helpers;
using PhongNguyenPuppy_MVC.Areas.Admin.Services;
using PhongNguyenPuppy_MVC.Areas.Admin.ViewModels;
using PhongNguyenPuppy_MVC.Models; // Model KhachHang
using PhongNguyenPuppy_MVC.Services; // Dịch vụ gửi mail, thống kê
using PhongNguyenPuppy_MVC.Data;
using System.IO;

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

        private readonly PhongNguyenPuppyContext _context;
        private readonly IConfiguration _configuration;
        private readonly ICloudinaryService _cloudinaryService; 



        public KhachHangController(
            IKhachHangRepository khachHangRepository,
            IDichVuGuiEmail dichVuGuiEmail,
            IDichVuThongKe dichVuThongKe,
            IOptions<TinyMceSettings> tinyMceSettings,
            IWebHostEnvironment webHostEnvironment,
             PhongNguyenPuppyContext context,         
            IConfiguration configuration,
            ICloudinaryService cloudinaryService)
        {
            _khachHangRepository = khachHangRepository;
            _dichVuGuiEmail = dichVuGuiEmail;
            _dichVuThongKe = dichVuThongKe;
            _tinyMceSettings = tinyMceSettings;
            _webHostEnvironment = webHostEnvironment;
            _context = context;                       
            _configuration = configuration;
            _cloudinaryService = cloudinaryService;
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
        public async Task<IActionResult> GuiEmail()
        {
            var emails = _context.KhachHangs
                .Where(kh => !string.IsNullOrEmpty(kh.Email))
                .Select(kh => kh.Email)
                .Distinct()
                .ToList();

            var sanPhamsLocal = _context.HangHoas
                .OrderByDescending(h => h.NgaySx)
                .Take(10)
                .Select(h => new
                {
                    h.MaHh,
                    h.TenHh,
                    DonGia = h.DonGia ?? 0,
                    Hinh = h.Hinh ?? "default.png",
                    MoTaNgan = h.MoTaDonVi ?? "",
                    GiamGia = h.GiamGia
                })
                .ToList();

            // ✅ UPLOAD ẢNH LÊN CLOUDINARY
            var sanPhams = new List<ProductEmailVM>();
            foreach (var sp in sanPhamsLocal)
            {
                string cloudinaryUrl;

                try
                {
                    var localPath = Path.Combine(_webHostEnvironment.WebRootPath, "Hinh", "HangHoa", sp.Hinh);

                    if (System.IO.File.Exists(localPath))
                    {
                        cloudinaryUrl = await _cloudinaryService.UploadImageFromPathAsync(localPath);
                    }
                    else
                    {
                        cloudinaryUrl = "https://via.placeholder.com/150?text=No+Image";
                    }
                }
                catch (Exception ex)
                {
                    // Log lỗi và dùng fallback
                    Console.WriteLine($"Cloudinary upload failed for {sp.Hinh}: {ex.Message}");
                    cloudinaryUrl = "https://via.placeholder.com/150?text=Error";
                }

                sanPhams.Add(new ProductEmailVM
                {
                    MaHh = sp.MaHh,
                    TenHh = sp.TenHh,
                    DonGia = sp.DonGia,
                    Hinh = cloudinaryUrl, // URL Cloudinary
                    MoTaNgan = sp.MoTaNgan,
                    GiamGia = sp.GiamGia
                });
            }

            var vm = new GuiEmailVM
            {
                DanhSachEmail = emails,
                DanhSachSanPham = sanPhams,
                TinyMceApiKey = _configuration["TinyMceSettings:ApiKey"]
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
                // ✅ UPLOAD TRỰC TIẾP LÊN CLOUDINARY
                var cloudinaryUrl = await _cloudinaryService.UploadImageAsync(file);
                return Json(new { location = cloudinaryUrl });
            }
            catch (Exception ex)
            {
                return Json(new { error = $"Lỗi upload: {ex.Message}" });
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
        // SearchProducts API - Upload lên Cloudinary
        [HttpGet]
        public async Task<IActionResult> SearchProducts(string query = "", int page = 1, int pageSize = 10)
        {
            var productsQuery = _context.HangHoas.AsQueryable();

            if (!string.IsNullOrEmpty(query))
            {
                productsQuery = productsQuery.Where(h => h.TenHh.Contains(query));
            }

            var totalCount = await productsQuery.CountAsync();

            var productsLocal = await productsQuery
                .OrderByDescending(h => h.NgaySx)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(h => new
                {
                    h.MaHh,
                    h.TenHh,
                    DonGia = h.DonGia ?? 0,
                    Hinh = h.Hinh ?? "default.png",
                    MoTaNgan = h.MoTaDonVi ?? "",
                    GiamGia = h.GiamGia
                })
                .ToListAsync();

            // UPLOAD LÊN CLOUDINARY
            var products = new List<object>();
            foreach (var p in productsLocal)
            {
                string cloudinaryUrl;

                try
                {
                    var localPath = Path.Combine(_webHostEnvironment.WebRootPath, "Hinh", "HangHoa", p.Hinh);

                    if (System.IO.File.Exists(localPath))
                    {
                        cloudinaryUrl = await _cloudinaryService.UploadImageFromPathAsync(localPath);
                    }
                    else
                    {
                        cloudinaryUrl = "https://via.placeholder.com/150?text=No+Image";
                    }
                }
                catch
                {
                    cloudinaryUrl = "https://via.placeholder.com/150?text=Error";
                }

                products.Add(new
                {
                    maHh = p.MaHh,
                    tenHh = p.TenHh,
                    donGia = p.DonGia,
                    hinh = cloudinaryUrl, // ✅ URL Cloudinary
                    moTaNgan = p.MoTaNgan,
                    giamGia = p.GiamGia
                });
            }

            return Json(new
            {
                success = true,
                products = products,
                totalCount = totalCount,
                currentPage = page,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
    }
}