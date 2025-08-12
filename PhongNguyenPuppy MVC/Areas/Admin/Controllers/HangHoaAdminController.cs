using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhongNguyenPuppy_MVC.Data;
using PhongNguyenPuppy_MVC.ViewModels;
using System.IO;

namespace PhongNguyenPuppy_MVC.Controllers.Admin
{
    [Authorize(Roles = "Admin")] // Chỉ admin truy cập
    public class HangHoaAdminController : Controller
    {
        private readonly PhongNguyenPuppyContext _db;
        private readonly IWebHostEnvironment _environment; // Để lưu hình ảnh

        public HangHoaAdminController(PhongNguyenPuppyContext db, IWebHostEnvironment environment)
        {
            _db = db;
            _environment = environment;
        }

        // Index: Liệt kê tất cả HangHoa
        public async Task<IActionResult> Index()
        {
            var hangHoas = await _db.HangHoas
                .Include(h => h.MaLoaiNavigation) // Load loại sản phẩm
                .Include(h => h.MaNccNavigation) // Load nhà cung cấp
                .ToListAsync();
            return View(hangHoas);
        }

        // Create (GET): Form tạo mới
        public IActionResult Create()
        {
            ViewBag.LoaiList = _db.Loais.ToList(); // Load danh sách loại để select
            ViewBag.NccList = _db.NhaCungCaps.ToList(); // Load danh sách NCC
            return View(new HangHoaAdmin());
        }

        // Create (POST): Lưu sản phẩm mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HangHoaAdmin model)
        {
            if (ModelState.IsValid)
            {
                var hangHoa = new HangHoa
                {
                    TenHh = model.TenHh,
                    TenAlias = model.TenAlias,
                    MaLoai = model.MaLoai,
                    MoTaDonVi = model.MoTaDonVi,
                    DonGia = model.DonGia,
                    NgaySx = model.NgaySx,
                    GiamGia = model.GiamGia,
                    SoLanXem = model.SoLanXem,
                    MoTa = model.MoTa,
                    MaNcc = model.MaNcc
                };

                // Xử lý upload hình ảnh
                if (model.Hinh != null)
                {
                    var filePath = Path.Combine(_environment.WebRootPath, "Hinh/HangHoa", model.Hinh.FileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Hinh.CopyToAsync(stream);
                    }
                    hangHoa.Hinh = model.Hinh.FileName;
                }

                _db.Add(hangHoa);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.LoaiList = _db.Loais.ToList();
            ViewBag.NccList = _db.NhaCungCaps.ToList();
            return View(model);
        }

        // Edit (GET): Form sửa
        public async Task<IActionResult> Edit(int id)
        {
            var hangHoa = await _db.HangHoas.FindAsync(id);
            if (hangHoa == null)
            {
                return NotFound();
            }

            var model = new HangHoaAdmin
            {
                MaHh = hangHoa.MaHh,
                TenHh = hangHoa.TenHh,
                TenAlias = hangHoa.TenAlias,
                MaLoai = hangHoa.MaLoai,
                MoTaDonVi = hangHoa.MoTaDonVi,
                DonGia = hangHoa.DonGia,
                ExistingHinh = hangHoa.Hinh,
                NgaySx = hangHoa.NgaySx,
                GiamGia = hangHoa.GiamGia,
                SoLanXem = hangHoa.SoLanXem,
                MoTa = hangHoa.MoTa,
                MaNcc = hangHoa.MaNcc
            };

            ViewBag.LoaiList = _db.Loais.ToList();
            ViewBag.NccList = _db.NhaCungCaps.ToList();
            return View(model);
        }

        // Edit (POST): Lưu sửa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(HangHoaAdmin model)
        {
            if (ModelState.IsValid)
            {
                var hangHoa = await _db.HangHoas.FindAsync(model.MaHh);
                if (hangHoa == null)
                {
                    return NotFound();
                }

                hangHoa.TenHh = model.TenHh;
                hangHoa.TenAlias = model.TenAlias;
                hangHoa.MaLoai = model.MaLoai;
                hangHoa.MoTaDonVi = model.MoTaDonVi;
                hangHoa.DonGia = model.DonGia;
                hangHoa.NgaySx = model.NgaySx;
                hangHoa.GiamGia = model.GiamGia;
                hangHoa.SoLanXem = model.SoLanXem;
                hangHoa.MoTa = model.MoTa;
                hangHoa.MaNcc = model.MaNcc;

                // Xử lý upload hình ảnh mới
                if (model.Hinh != null)
                {
                    var filePath = Path.Combine(_environment.WebRootPath, "Hinh/HangHoa", model.Hinh.FileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Hinh.CopyToAsync(stream);
                    }
                    hangHoa.Hinh = model.Hinh.FileName;
                }

                _db.Update(hangHoa);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.LoaiList = _db.Loais.ToList();
            ViewBag.NccList = _db.NhaCungCaps.ToList();
            return View(model);
        }

        // Delete (GET): Xác nhận xóa
        public async Task<IActionResult> Delete(int id)
        {
            var hangHoa = await _db.HangHoas.FindAsync(id);
            if (hangHoa == null)
            {
                return NotFound();
            }
            return View(hangHoa);
        }

        // Delete (POST): Thực hiện xóa
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hangHoa = await _db.HangHoas.FindAsync(id);
            if (hangHoa != null)
            {
                _db.HangHoas.Remove(hangHoa);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}