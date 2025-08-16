using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhongNguyenPuppy_MVC.Areas.Admin.ViewModels;
using PhongNguyenPuppy_MVC.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PhongNguyenPuppy_MVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin", AuthenticationSchemes = "AdminScheme")]
    public class HangHoaAdminController : Controller
    {
        private readonly PhongNguyenPuppyContext _db;
        private readonly IWebHostEnvironment _environment;

        public HangHoaAdminController(PhongNguyenPuppyContext db, IWebHostEnvironment environment)
        {
            _db = db;
            _environment = environment;
        }

        // Hiển thị danh sách sản phẩm, tìm kiếm, lọc và phân trang
        public async Task<IActionResult> Index(string search, int? maLoai, string tenCongTy, string sortOrder, int page = 1)
        {
            int pageSize = 10;

            var query = _db.HangHoas
                .Include(h => h.MaLoaiNavigation)
                .Include(h => h.MaNccNavigation)
                .AsQueryable();

            // Tìm kiếm theo tên sản phẩm
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(h => h.TenHh.Contains(search));
                ViewBag.Search = search;
            }

            // Lọc theo loại
            if (maLoai.HasValue)
            {
                query = query.Where(h => h.MaLoai == maLoai.Value);
                ViewBag.SelectedLoai = maLoai.Value;
            }

            // Lọc theo tên công ty nhà cung cấp
            if (!string.IsNullOrEmpty(tenCongTy))
            {
                query = query.Where(h => h.MaNccNavigation.TenCongTy == tenCongTy);
                ViewBag.SelectedTenCongTy = tenCongTy;
            }

            // Sắp xếp theo giá
            ViewBag.SortOrder = sortOrder;
            query = sortOrder switch
            {
                "price_desc" => query.OrderByDescending(h => h.DonGia),
                "price_asc" => query.OrderBy(h => h.DonGia),
                "newest" => query.OrderByDescending(h => h.MaHh), 
                _ => query.OrderBy(h => h.MaHh)
            };

            // Phân trang
            var totalItems = await query.CountAsync();
            var hangHoas = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.LoaiList = await _db.Loais.ToListAsync();
            ViewBag.TenCongTyList = await _db.NhaCungCaps
                .Where(n => !string.IsNullOrEmpty(n.TenCongTy))
                .Select(n => n.TenCongTy)
                .Distinct()
                .ToListAsync();

            return View(hangHoas);
        }




        // Hiển thị form thêm sản phẩm
        public IActionResult Create()
        {
            ViewBag.LoaiList = _db.Loais.ToList();
            ViewBag.NccList = _db.NhaCungCaps
    .Where(n => n.MaNcc != null && n.TenCongTy != null)
    .ToList();

            return View();
        }



        // Xử lý thêm sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HangHoaCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                Console.WriteLine("ModelState is valid. Processing...");
                var hangHoa = new HangHoa
                {
                    TenHh = model.TenHh,
                    MaLoai = model.MaLoai ?? 0,
                    MaNcc = model.MaNCC ?? string.Empty,
                    DonGia = (double)(model.DonGia ?? 0),
                    MoTa = model.MoTa
                };

                if (model.Hinh != null && model.Hinh.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.Hinh.FileName);
                    var folderPath = Path.Combine(_environment.WebRootPath, "Hinh", "HangHoa");

                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    var filePath = Path.Combine(folderPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Hinh.CopyToAsync(stream);
                    }
                    hangHoa.Hinh = fileName;
                }
                else
                {
                    ModelState.AddModelError("Hinh", "Vui lòng chọn hình ảnh.");
                }

                if (!_db.Loais.Any(l => l.MaLoai == hangHoa.MaLoai))
                {
                    ModelState.AddModelError("MaLoai", "Loại sản phẩm không tồn tại.");
                }
                if (!_db.NhaCungCaps.Any(n => n.MaNcc == hangHoa.MaNcc))
                {
                    ModelState.AddModelError("MaNCC", "Nhà cung cấp không tồn tại.");
                }

                if (ModelState.IsValid)
                {
                    _db.HangHoas.Add(hangHoa);
                    await _db.SaveChangesAsync();
                    TempData["Success"] = "Thêm sản phẩm thành công!";
                    return RedirectToAction("Index");
                }
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            TempData["Error"] = string.Join("; ", errors);
            ViewBag.LoaiList = await _db.Loais.ToListAsync();
            ViewBag.NccList = await _db.NhaCungCaps
                .Where(n => n.MaNcc != null && n.TenCongTy != null)
                .ToListAsync();
            return View(model);
        }

        // Hiển thị form chỉnh sửa
        // Hiển thị form sửa
        public async Task<IActionResult> Edit(int id)
        {
            var hangHoa = await _db.HangHoas.FindAsync(id);
            if (hangHoa == null) return NotFound();

            var model = new HangHoaEditViewModel
            {
                MaHh = hangHoa.MaHh,
                TenHh = hangHoa.TenHh,
                MaLoai = hangHoa.MaLoai,
                MaNCC = hangHoa.MaNcc, // Gán trực tiếp vì cả hai đều là string
                DonGia = (decimal?)hangHoa.DonGia,
                ExistingHinh = hangHoa.Hinh,
                MoTa = hangHoa.MoTa
            };

            ViewBag.LoaiList = _db.Loais.ToList();
            ViewBag.NccList = _db.NhaCungCaps.ToList();

            return View(model);
        }

        // Xử lý chỉnh sửa sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(HangHoaEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                var hangHoa = await _db.HangHoas.FindAsync(model.MaHh);
                if (hangHoa == null) return NotFound();

                hangHoa.TenHh = model.TenHh;
                hangHoa.MaLoai = model.MaLoai ?? 0;
                hangHoa.MaNcc = model.MaNCC ?? string.Empty; // Sửa thành string
                hangHoa.DonGia = (double)(model.DonGia ?? 0);
                hangHoa.MoTa = model.MoTa;

                // Upload hình mới nếu có
                if (model.Hinh != null && model.Hinh.Length > 0)
                {
                    // Xóa hình cũ nếu tồn tại
                    if (!string.IsNullOrEmpty(model.ExistingHinh))
                    {
                        var oldPath = Path.Combine(_environment.WebRootPath, "Hinh", "HangHoa", model.ExistingHinh);
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.Hinh.FileName);
                    var folderPath = Path.Combine(_environment.WebRootPath, "Hinh", "HangHoa");
                    var filePath = Path.Combine(folderPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Hinh.CopyToAsync(stream);
                    }

                    hangHoa.Hinh = fileName;
                }

                _db.Update(hangHoa);
                await _db.SaveChangesAsync();

                TempData["Success"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction("Index");
            }

            // Nếu ModelState không hợp lệ
            ViewBag.LoaiList = _db.Loais.ToList();
            ViewBag.NccList = _db.NhaCungCaps.ToList();

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            TempData["Error"] = string.Join("; ", errors);

            return View(model);
        }

        // Hiển thị xác nhận xóa sản phẩm
        public async Task<IActionResult> Delete(int id)
        {
            var hangHoa = await _db.HangHoas.FindAsync(id);
            if (hangHoa == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction("Index");
            }

            return View(hangHoa); // Trả về View Delete.cshtml với model là sản phẩm
        }

        // Xử lý xóa sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hangHoa = await _db.HangHoas.FindAsync(id);
            if (hangHoa == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm để xóa.";
                return RedirectToAction("Index");
            }

            // Xóa hình nếu có
            if (!string.IsNullOrEmpty(hangHoa.Hinh))
            {
                var filePath = Path.Combine(_environment.WebRootPath, "Hinh", "HangHoa", hangHoa.Hinh);
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        System.IO.File.Delete(filePath);
                    }
                    catch
                    {
                        TempData["Error"] = "Không thể xóa hình ảnh sản phẩm.";
                        return RedirectToAction("Index");
                    }
                }
            }

            _db.HangHoas.Remove(hangHoa);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa sản phẩm: {hangHoa.TenHh}";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult CreateAjax([FromBody] LoaiViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.TenLoai))
                return Json(new { success = false, message = "Tên loại không được để trống." });

            try
            {
                var loai = new Loai
                {
                    TenLoai = model.TenLoai.Trim(),
                    TenLoaiAlias = model.TenLoaiAlias?.Trim(),
                    MoTa = model.MoTa?.Trim()
                };

                _db.Loais.Add(loai);
                _db.SaveChanges();

                return Json(new { success = true, maLoai = loai.MaLoai, tenLoai = loai.TenLoai });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi thêm loại: " + ex.Message });
            }
        }

        [HttpDelete]
        public JsonResult DeleteAjax(int id)
        {
            var loai = _db.Loais.Find(id);
            if (loai == null)
                return Json(new { success = false, message = "Không tìm thấy loại." });

            // Kiểm tra nếu loại đang được dùng
            bool isUsed = _db.HangHoas.Any(h => h.MaLoai == id);
            if (isUsed)
                return Json(new { success = false, message = "Loại đang được sử dụng, không thể xóa." });

            _db.Loais.Remove(loai);
            _db.SaveChanges();

            return Json(new { success = true });
        }



    }
}