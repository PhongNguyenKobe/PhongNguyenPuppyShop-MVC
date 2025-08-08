using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhongNguyenPuppy_MVC.Data;
using PhongNguyenPuppy_MVC.Helpers;
using PhongNguyenPuppy_MVC.Services;
using PhongNguyenPuppy_MVC.ViewModels;

namespace PhongNguyenPuppy_MVC.Controllers
{
    public class CartController : Controller
    {
        private readonly PhongNguyenPuppyContext db;
        private readonly PaypalClient _paypalClient;
        private readonly IVnPayService _vnPayService;

        public CartController(PhongNguyenPuppyContext Context, PaypalClient paypalClient, IVnPayService vnPayService)
        {
            // Constructor logic can be added here if needed
            db = Context;
            _paypalClient = paypalClient;
            _vnPayService = vnPayService;
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
            if (Cart.Count == 0)
            {
                return RedirectToAction("Index");
            }
            ViewBag.PaypalClientId = _paypalClient.ClientId;
            return View(Cart);
        }

        [Authorize]
        [HttpPost]
        public IActionResult Checkout(CheckoutVM model, string payment = "COD")
        {
            if (payment == "Thanh toán VNPay")
            {
                var vnpayModel = new VnPaymentRequestModel
                {
                    Amount = Cart.Sum(p => p.ThanhTien),
                    CreatedDate = DateTime.Now,
                    Description = $"{model.HoTen} {model.DienThoai}",
                    FullName = model.HoTen,
                    OrderId = new Random().Next(1000, 10000),
                };

                var paymentUrl = _vnPayService.CreatePaymentUrl(HttpContext, vnpayModel);
                return Redirect(paymentUrl); //Redirect trực tiếp đến VNPay
            }

            if (ModelState.IsValid)
            {
                var customerId = HttpContext.User.Claims.SingleOrDefault(p => p.Type == MySetting.CLAIM_CUSTOMERID).Value;

                var khachHang = new KhachHang();
                if (model.GiongKhachHang)
                {
                    khachHang = db.KhachHangs.SingleOrDefault(p => p.MaKh == customerId);
                }
                var hoadon = new HoaDon
                {
                    MaKh = customerId,
                    DiaChi = model.DiaChi ?? khachHang?.DiaChi,
                    DienThoai = model.DienThoai ?? khachHang?.DienThoai,
                    HoTen = model.HoTen ?? khachHang?.HoTen,
                    NgayDat = DateTime.Now,
                    CachThanhToan = "Thanh toán khi nhận hàng",
                    CachVanChuyen = "Giao hàng tận nơi",
                    MaTrangThai = 0,
                    GhiChu = model.GhiChu,
                };
                using var transaction = db.Database.BeginTransaction();
                try
                {
                    db.Add(hoadon);
                    db.SaveChanges();

                    var cthds = new List<ChiTietHd>();
                    foreach (var item in Cart)
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
                    transaction.Commit(); // Commit sau khi tất cả thao tác DB thành công
                    HttpContext.Session.Set<List<CartItem>>(MySetting.CART_KEY, new List<CartItem>());
                    //Truyền mã đơn hàng vào ViewBag
                    ViewBag.PaymentMethod = "COD";
                    ViewBag.MaHd = hoadon.MaHd;
                    return View("Success");
                }
                catch
                {
                    db.Database.RollbackTransaction();
                    ModelState.AddModelError("", "Đặt hàng không thành công. Vui lòng thử lại sau.");
                    ViewBag.PaypalClientId = _paypalClient.ClientId;
                    return View(Cart);
                }
            }
            return View(Cart);
        }

        [Authorize]
        public IActionResult PaymentSuccess(int mahd)
        {
            ViewBag.PaymentMethod = "PayPal";
            ViewBag.MaHd = mahd;
            return View("Success");
        }


        #region Paypal payment
        [Authorize]
        [HttpPost("/Cart/create-paypal-order")]
        public async Task<IActionResult> CreatePaypalOrder(CancellationToken cancellationToken)
        {
            var tongTienVND = Cart.Sum(p => p.ThanhTien);
            var donViTienTe = "USD";
            var maDonHangThamChieu = "DH" + DateTime.Now.Ticks.ToString();

            var tongTienUSD = await ExchangeRateHelper.ConvertVNDtoUSDAsync(tongTienVND);

            if (tongTienUSD == null)
            {
                return BadRequest(new { Message = "Không thể lấy tỷ giá USD từ Vietcombank." });
            }

            try
            {
                var response = await _paypalClient.CreateOrder(tongTienUSD.Value.ToString("F2"), donViTienTe, maDonHangThamChieu);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var error = new { ex.GetBaseException().Message };
                return BadRequest(error);
            }
        }


        [Authorize]
        [HttpPost("/Cart/capture-paypal-order")]
        public async Task<IActionResult> CapturePaypalOrder(string orderID, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _paypalClient.CaptureOrder(orderID);

                // 1. Lấy mã khách hàng từ Claims
                var customerId = HttpContext.User.Claims.SingleOrDefault(p => p.Type == MySetting.CLAIM_CUSTOMERID).Value;
                var khachHang = db.KhachHangs.SingleOrDefault(p => p.MaKh == customerId);

                // 2. Tạo hóa đơn
                var hoadon = new HoaDon
                {
                    MaKh = customerId,
                    DiaChi = khachHang?.DiaChi,
                    DienThoai = khachHang?.DienThoai,
                    HoTen = khachHang?.HoTen,
                    NgayDat = DateTime.Now,
                    CachThanhToan = "Thanh toán qua Paypal",
                    CachVanChuyen = "Giao hàng tận nơi",
                    MaTrangThai = 0,
                    GhiChu = "Thanh toán thành công qua Paypal"
                };

                using var transaction = db.Database.BeginTransaction();
                db.Add(hoadon);
                db.SaveChanges();

                var cthds = new List<ChiTietHd>();
                foreach (var item in Cart)
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
                transaction.Commit();

                // Xóa giỏ hàng
                HttpContext.Session.Set<List<CartItem>>(MySetting.CART_KEY, new List<CartItem>());

                // 3. Trả về JSON chứa mã đơn hàng để redirect phía client
                return Ok(new
                {
                    status = "success",
                    mahd = hoadon.MaHd
                });
            }
            catch (Exception ex)
            {
                db.Database.RollbackTransaction();
                return BadRequest(new { message = "Thanh toán thất bại: " + ex.GetBaseException().Message });
            }
        }

        #endregion


        [Authorize]
        public IActionResult PaymentFail()
        {
            return View();
        }

        [Authorize]
        public IActionResult PaymentCallBack()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            if (response == null || response.VnPayResponseCode != "00")
            {
                TempData["Message"] = $"Thanh toán VNPay không thành công. Vui lòng thử lại. Mã lỗi: {response?.VnPayResponseCode}";
                return RedirectToAction("PaymentFail");
            }

            // ✅ Lưu đơn hàng vào cơ sở dữ liệu
            var customerId = HttpContext.User.Claims.SingleOrDefault(p => p.Type == MySetting.CLAIM_CUSTOMERID)?.Value;
            var hoadon = new HoaDon
            {
                MaKh = customerId,
                DiaChi = "Địa chỉ mặc định", // Bạn có thể lấy từ session hoặc lưu tạm trước đó
                DienThoai = "SĐT mặc định",
                HoTen = "Tên KH mặc định",
                NgayDat = DateTime.Now,
                CachThanhToan = "Thanh toán VNPay",
                CachVanChuyen = "Giao hàng tận nơi",
                MaTrangThai = 0,
                GhiChu = "Đơn hàng thanh toán qua VNPay"
            };

            using var transaction = db.Database.BeginTransaction();
            try
            {
                db.Add(hoadon);
                db.SaveChanges();

                var cthds = new List<ChiTietHd>();
                foreach (var item in Cart)
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
                transaction.Commit();

                HttpContext.Session.Set<List<CartItem>>(MySetting.CART_KEY, new List<CartItem>());
                ViewBag.PaymentMethod = "VNPay";
                ViewBag.MaHd = hoadon.MaHd;
                ViewBag.VnPayCode = response.VnPayResponseCode;
                return View("Success");
            }
            catch
            {
                transaction.Rollback();
                TempData["Message"] = "Đặt hàng không thành công sau khi thanh toán. Vui lòng liên hệ hỗ trợ.";
                return RedirectToAction("PaymentFail");
            }
        }

    }
}

