using Microsoft.AspNetCore.Mvc;
using PhongNguyenPuppy_MVC.Data;
using Microsoft.EntityFrameworkCore;

namespace PhongNguyenPuppy_MVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    public class MaGiamGiaController : Controller
    {
        private readonly PhongNguyenPuppyContext db;

        public MaGiamGiaController(PhongNguyenPuppyContext context)
        {
            db = context;
        }

        public async Task<IActionResult> Index()
        {
            var danhSach = await db.MaGiamGias.Include(m => m.MaNvNavigation).ToListAsync();
            return View(danhSach);
        }

        public IActionResult Create()
        {
            return View();
        }

        // tạo mã giảm giá mới
        [HttpPost]
        public async Task<IActionResult> Create(MaGiamGia model)
        {
            if (ModelState.IsValid)
            {
                db.MaGiamGias.Add(model);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // xóa mã giảm giá
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var ma = await db.MaGiamGias.FindAsync(id);
            if (ma != null)
            {
                db.MaGiamGias.Remove(ma);
                await db.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}
