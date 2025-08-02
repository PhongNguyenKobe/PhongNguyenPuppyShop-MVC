using Microsoft.AspNetCore.Authorization;
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

        public List<CartItem> Cart => HttpContext.Session.Get<List<CartItem>>(MySetting.CART_KEY) ?? new List<CartItem>();
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
            HttpContext.Session.Set(MySetting.CART_KEY, giohang);
            return RedirectToAction("Index");
        }

        public IActionResult RemoveCart(int id)
        {
            var giohang = Cart;
            var item = giohang.SingleOrDefault(p => p.MaHh == id);
            if (item != null)
            {
                giohang.Remove(item);
                HttpContext.Session.Set(MySetting.CART_KEY, giohang);
            }
            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpGet]
        public IActionResult Checkout()
        {
            if(Cart.Count == 0)
            {
                return RedirectToAction("Index");
            }
            return View(Cart);
        }

        [Authorize]
        [HttpPost]
        public IActionResult Checkout(CheckoutVM model)
        {
            if (ModelState.IsValid)
            {
                var customerId = HttpContext.User.Claims.SingleOrDefault(p => p.Type ==  MySetting.CLAIM_CUSTOMERID).Value;

                var khachHang = new KhachHang();
                if (model.GiongKhachHang)
                {
                    khachHang = db.KhachHangs.SingleOrDefault(p => p.MaKh == customerId);
                }
                var hoadon = new HoaDon
                {
                    MaKh = customerId,
                    DiaChi = model.DiaChi?? khachHang?.DiaChi,
                    DienThoai = model.DienThoai?? khachHang?.DienThoai,
                    HoTen = model.HoTen ?? khachHang?.HoTen,
                    NgayDat = DateTime.Now,
                    CachThanhToan = "Thanh toán khi nhận hàng",
                    CachVanChuyen = "Giao hàng tận nơi",
                    MaTrangThai = 0,
                    GhiChu = model.GhiChu,
                };

                db.Database.BeginTransaction();
                try
                {
                    db.Database.CommitTransaction();
                    db.Add(hoadon);
                    db.SaveChanges();

                    var cthds = new List<ChiTietHd>();
                    foreach(var item in Cart)
                    {
                        cthds.Add(new ChiTietHd
                        {
                            MaHd = hoadon.MaHd,
                            SoLuong = item.SoLuong,
                            DonGia = item.DonGia,
                            MaHh = item.MaHh,
                            GiamGia = 0,
                        });
                    }
                    db.AddRange(cthds);
                    db.SaveChanges();

                    HttpContext.Session.Set<List<CartItem>>(MySetting.CART_KEY, new List<CartItem>());
                    //Truyền mã đơn hàng vào ViewBag
                    ViewBag.MaHd = hoadon.MaHd;
                    return View("Success");
                }
                catch
                {
                    db.Database.RollbackTransaction();
                    ModelState.AddModelError("Lỗi", "Đặt hàng không thành công. Vui lòng thử lại sau.");
                    return View(Cart);
                }
            }
            return View(Cart);
        }
    }
}
