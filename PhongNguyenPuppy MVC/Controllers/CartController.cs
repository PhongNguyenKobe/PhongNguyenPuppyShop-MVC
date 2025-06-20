using Microsoft.AspNetCore.Mvc;
using PhongNguyenPuppy_MVC.Data;
using PhongNguyenPuppy_MVC.Helpers;
using PhongNguyenPuppy_MVC.ViewModels;

namespace PhongNguyenPuppy_MVC.Controllers
{
    public class CartController : Controller
    {
        private readonly PhongNguyenPuppyContext db;

        public CartController(PhongNguyenPuppyContext Context)
        {
            // Constructor logic can be added here if needed
            db = Context;
        }

        const string CART_KEY = "MYCART";
        public List<CartItem> Cart => HttpContext.Session.Get<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
        public IActionResult Index()
        {
            return View(Cart);
        }

        public IActionResult AddToCart(int id, int quantity = 1)
        {
            var giohang = Cart;
            var item = giohang.SingleOrDefault(p => p.MaHh == id);
            if (item == null)
            {
                var hanghoa = db.HangHoas.SingleOrDefault(p => p.MaHh == id);
                if (hanghoa == null) // Fix: Check if hanghoa is null before accessing its properties  
                {
                    TempData["Message"] = "Không tìm thấy hàng hóa mà bạn tìm";
                    return RedirectToAction("/404");
                }
                item = new CartItem
                {
                    MaHh = hanghoa.MaHh,
                    TenHH = hanghoa.TenHh,
                    DonGia = hanghoa.DonGia ?? 0,
                    Hinh = hanghoa.Hinh ?? string.Empty,
                    SoLuong = quantity
                };
                giohang.Add(item);
            }
            else
            {
                item.SoLuong += quantity;
            }
            HttpContext.Session.Set(CART_KEY, giohang);
            return RedirectToAction("Index");
        }

        public IActionResult RemoveCart(int id)
        {
            var giohang = Cart;
            var item = giohang.SingleOrDefault(p => p.MaHh == id);
            if (item != null)
            {
                giohang.Remove(item);
                HttpContext.Session.Set(CART_KEY, giohang);
            }
            return RedirectToAction("Index");
        }
    }
}
