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

        [HttpPost]
        public IActionResult UpdateQuantity(int id, int quantity)
        {
            var giohang = Cart;
            var item = giohang.SingleOrDefault(p => p.MaHh == id);
            if (item != null && quantity > 0)
            {
                item.SoLuong = quantity;
                HttpContext.Session.Set(MySetting.CART_KEY, giohang);
            }
            return Ok(new { success = true });
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
                if (hanghoa == null) 
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

            var customerId = HttpContext.User.Claims.SingleOrDefault(p => p.Type == MySetting.CLAIM_CUSTOMERID)?.Value;
            var khachHang = db.KhachHangs.SingleOrDefault(p => p.MaKh == customerId);
            ViewBag.CustomerAddress = khachHang?.DiaChi ?? "";

            int tongTienHang = (int)Cart.Sum(p => p.ThanhTien);
            int phiVanChuyen = 30000;
            string finalAddress = khachHang?.DiaChi ?? "";
            if (finalAddress.ToLower().Contains("hồ chí minh") || finalAddress.ToLower().Contains("tp. hcm") || tongTienHang >= 500000)
            {
                phiVanChuyen = 0;
            }
            double giamGia = 0;
            var maGiamGia = HttpContext.Session.GetString("MaGiamGia"); // Lấy từ session
            if (!string.IsNullOrEmpty(maGiamGia))
            {
                var coupon = db.MaGiamGias
                    .SingleOrDefault(c => c.Code == maGiamGia
                                       && c.TrangThai
                                       && (c.HanSuDung == null || c.HanSuDung > DateTime.Now)
                                       && (c.SoLuongToiDa == null || c.SoLuongDaDung < c.SoLuongToiDa));
                if (coupon != null)
                {
                    giamGia = coupon.LoaiGiam ? coupon.GiaTri : (tongTienHang * (coupon.GiaTri / 100));
                    giamGia = Math.Min(giamGia, tongTienHang);
                }
            }

            // Lưu vào session
            HttpContext.Session.SetInt32("PhiVanChuyen", phiVanChuyen);
            HttpContext.Session.SetString("GiamGia", giamGia.ToString("F2")); // Sử dụng SetString

            return View(Cart);
        }

        [Authorize]
        [HttpPost]
        public IActionResult Checkout(CheckoutVM model, string payment = "COD")
        {
            var gioHang = Cart;
            int tongTienHang = (int)gioHang.Sum(p => p.ThanhTien);
            int phiVanChuyen = 30000;

            string finalAddress = model.DiaChi;
            if (model.GiongKhachHang)
            {
                var customerId = HttpContext.User.Claims.SingleOrDefault(p => p.Type == MySetting.CLAIM_CUSTOMERID).Value;
                var khachHang = db.KhachHangs.SingleOrDefault(p => p.MaKh == customerId);
                finalAddress = khachHang?.DiaChi ?? model.DiaChi;
            }

            if (finalAddress.ToLower().Contains("hồ chí minh") || finalAddress.ToLower().Contains("tp. hcm") || tongTienHang >= 500000)
            {
                phiVanChuyen = 0;
            }

            double giamGia = 0; // Giá trị giảm từ mã
            if (!string.IsNullOrEmpty(model.MaGiamGia))
            {
                var coupon = db.MaGiamGias
                    .SingleOrDefault(c => c.Code == model.MaGiamGia
                                       && c.TrangThai
                                       && (c.HanSuDung == null || c.HanSuDung > DateTime.Now)
                                       && (c.SoLuongToiDa == null || c.SoLuongDaDung < c.SoLuongToiDa));
                if (coupon != null)
                {
                    giamGia = coupon.LoaiGiam ? coupon.GiaTri : (tongTienHang * (coupon.GiaTri / 100));
                    giamGia = Math.Min(giamGia, tongTienHang); // Đảm bảo không vượt tổng tiền hàng
                }
                else
                {
                    ModelState.AddModelError("MaGiamGia", "Mã giảm giá không hợp lệ hoặc hết hạn.");
                    ViewBag.PaypalClientId = _paypalClient.ClientId;
                    return View(Cart);
                }
            }

            int tongCong = tongTienHang - (int)giamGia + phiVanChuyen;

            if (payment == "Thanh toán VNPay")
            {
                var vnpayModel = new VnPaymentRequestModel
                {
                    Amount = tongCong, // Sửa: Amount = tongCong (đã trừ giamGia)
                    CreatedDate = DateTime.Now,
                    Description = $"{model.HoTen} {model.DienThoai}",
                    FullName = model.HoTen,
                    OrderId = new Random().Next(1000, 10000),
                };

                var paymentUrl = _vnPayService.CreatePaymentUrl(HttpContext, vnpayModel);
                return Redirect(paymentUrl);
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
                    PhiVanChuyen = phiVanChuyen,
                    GiamGia = (float)giamGia // Lưu giá trị giảm giá vào hóa đơn
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
                            GiamGia = 0, // Giảm giá chi tiết để 0, vì đã tính ở hóa đơn
                        });
                    }
                    db.AddRange(cthds);
                    db.SaveChanges();

                    // Cập nhật số lượng đã dùng của mã giảm giá
                    if (giamGia > 0)
                    {
                        var coupon = db.MaGiamGias.Single(c => c.Code == model.MaGiamGia);
                        coupon.SoLuongDaDung++;
                        db.Update(coupon);
                        db.SaveChanges();
                    }

                    transaction.Commit();
                    HttpContext.Session.Set<List<CartItem>>(MySetting.CART_KEY, new List<CartItem>());
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

            ViewBag.PaypalClientId = _paypalClient.ClientId;
            return View(Cart);
        }

        [HttpPost]
        public IActionResult KiemTraMaGiamGia(string maGiamGia)
        {
            var tongTienHang = Cart.Sum(p => p.ThanhTien);
            var coupon = db.MaGiamGias
                .SingleOrDefault(c => c.Code == maGiamGia
                                   && c.TrangThai
                                   && (c.HanSuDung == null || c.HanSuDung > DateTime.Now)
                                   && (c.SoLuongToiDa == null || c.SoLuongDaDung < c.SoLuongToiDa));
            if (coupon != null)
            {
                double giamGia = coupon.LoaiGiam ? coupon.GiaTri : (tongTienHang * (coupon.GiaTri / 100));
                giamGia = Math.Min(giamGia, tongTienHang);
                HttpContext.Session.SetString("GiamGia", giamGia.ToString("F2")); // Sử dụng SetString
                HttpContext.Session.SetString("MaGiamGia", maGiamGia);
                return Ok(new { success = true, giamGia = giamGia, moTa = coupon.MoTa });
            }
            return Ok(new { success = false, message = "Mã giảm giá không hợp lệ hoặc hết hạn." });
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
            var tongTienHang = Cart.Sum(p => p.ThanhTien);
            var phiVanChuyen = HttpContext.Session.GetInt32("PhiVanChuyen") ?? 30000;
            var giamGiaStr = HttpContext.Session.GetString("GiamGia");
            var giamGia = string.IsNullOrEmpty(giamGiaStr) ? 0 : double.Parse(giamGiaStr);
            var tongTienVND = (int)(tongTienHang - giamGia + phiVanChuyen);
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

                var customerId = HttpContext.User.Claims.SingleOrDefault(p => p.Type == MySetting.CLAIM_CUSTOMERID).Value;
                var khachHang = db.KhachHangs.SingleOrDefault(p => p.MaKh == customerId);

                var gioHang = Cart;
                int tongTienHang = (int)gioHang.Sum(p => p.ThanhTien);
                var phiVanChuyen = HttpContext.Session.GetInt32("PhiVanChuyen") ?? 30000;
                var giamGiaStr = HttpContext.Session.GetString("GiamGia");
                var giamGia = string.IsNullOrEmpty(giamGiaStr) ? 0 : double.Parse(giamGiaStr);

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
                    GhiChu = "Thanh toán thành công qua Paypal",
                    PhiVanChuyen = phiVanChuyen,
                    GiamGia = (float)giamGia
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

                HttpContext.Session.Set<List<CartItem>>(MySetting.CART_KEY, new List<CartItem>());

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
        [Authorize]
        public IActionResult PaymentCallBack()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            if (response == null || response.VnPayResponseCode != "00")
            {
                TempData["Message"] = $"Thanh toán VNPay không thành công. Vui lòng thử lại. Mã lỗi: {response?.VnPayResponseCode}";
                return RedirectToAction("PaymentFail");
            }

            // Lấy thông tin từ giỏ hàng và session
            var gioHang = Cart;
            int tongTienHang = (int)gioHang.Sum(p => p.ThanhTien);
            var phiVanChuyen = HttpContext.Session.GetInt32("PhiVanChuyen") ?? 30000; // Lấy từ session
            var giamGiaStr = HttpContext.Session.GetString("GiamGia");
            var giamGia = string.IsNullOrEmpty(giamGiaStr) ? 0 : double.Parse(giamGiaStr); // Lấy từ session

            // Lấy thông tin khách hàng
            var customerId = HttpContext.User.Claims.SingleOrDefault(p => p.Type == MySetting.CLAIM_CUSTOMERID)?.Value;
            var khachHang = db.KhachHangs.SingleOrDefault(p => p.MaKh == customerId);

            // Tạo hóa đơn (không dùng TongTien)
            var hoadon = new HoaDon
            {
                MaKh = customerId,
                DiaChi = khachHang?.DiaChi ?? "Địa chỉ mặc định",
                DienThoai = khachHang?.DienThoai ?? "SĐT mặc định",
                HoTen = khachHang?.HoTen ?? "Tên KH mặc định",
                NgayDat = DateTime.Now,
                CachThanhToan = "Thanh toán VNPay",
                CachVanChuyen = "Giao hàng tận nơi",
                MaTrangThai = 0,
                GhiChu = "Đơn hàng thanh toán qua VNPay",
                PhiVanChuyen = phiVanChuyen, // Lưu phí vận chuyển từ session
                GiamGia = giamGia // Lưu giảm giá từ session
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
                        GiamGia = 0, // Giảm giá chi tiết vẫn là 0
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

