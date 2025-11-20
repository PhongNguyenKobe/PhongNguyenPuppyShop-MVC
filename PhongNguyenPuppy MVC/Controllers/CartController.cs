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
        private readonly IGHNService _ghnService;

        public CartController(PhongNguyenPuppyContext Context, PaypalClient paypalClient, IVnPayService vnPayService, IGHNService ghnService)
        {
            db = Context;
            _paypalClient = paypalClient;
            _vnPayService = vnPayService;
            _ghnService = ghnService;
        }

        // Thêm API endpoint để tính phí vận chuyển động
        [HttpPost]
        public async Task<IActionResult> CalculateShippingFee(int districtId, string wardCode)
        {
            try
            {
                var tongTienHang = Cart.Sum(p => p.ThanhTien);

                // Nếu đơn hàng >= 500K → FREE SHIP (bất kể địa chỉ)
                if (tongTienHang >= 500000)
                {
                    HttpContext.Session.SetInt32("PhiVanChuyen", 0);
                    return Ok(new { success = true, shippingFee = 0, freeShip = true });
                }

                // Danh sách District ID của các quận nội thành TP.HCM
                var noiThanhHCM = new List<int>
        {
            1442, // Quận 1
            1443, // Quận 2
            1444, // Quận 3
            1445, // Quận 4
            1446, // Quận 5
            1447, // Quận 6
            1448, // Quận 8
            1449, // Quận 7
            1450, // Quận 9
            1451, // Quận 10
            1452, // Quận 11
            1453, // Quận 12
            1454, // Quận Bình Thạnh
            1455, // Quận Gò Vấp
            1456, // Quận Phú Nhuận
            1457, // Quận Tân Bình
            1458, // Quận Tân Phú
            1459, // Quận Bình Tân
            1460, // Thủ Đức (cũ)
            1463  // TP Thủ Đức (mới)
        };

                int shippingFee;

                if (noiThanhHCM.Contains(districtId))
                {
                    // ===== NỘI THÀNH TP.HCM: PHÍ CỐ ĐỊNH 20K =====
                    shippingFee = 20000;
                    Console.WriteLine($"[SHIPPING] Nội thành TP.HCM → Phí cố định: 20,000 VNĐ");
                }
                else
                {
                    // ===== NGOẠI THÀNH / TỈNH KHÁC: TÍNH THEO GHN =====
                    var totalWeight = Cart.Sum(p => p.SoLuong * 500); // gram
                    shippingFee = await _ghnService.CalculateShippingFeeAsync(districtId, wardCode, totalWeight);
                    Console.WriteLine($"[SHIPPING] Ngoại thành/tỉnh khác → Phí GHN: {shippingFee:N0} VNĐ");
                }

                // Lưu phí vận chuyển vào Session
                HttpContext.Session.SetInt32("PhiVanChuyen", shippingFee);

                return Ok(new { success = true, shippingFee = shippingFee, freeShip = false });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] CalculateShippingFee: {ex.Message}");
                return Ok(new { success = false, message = ex.Message });
            }
        }

        // API để lấy danh sách tỉnh/thành phố
        [HttpGet]
        public async Task<IActionResult> GetProvinces()
        {
            var provinces = await _ghnService.GetProvincesAsync();
            return Ok(provinces);
        }

        // API để lấy danh sách quận/huyện
        [HttpGet]
        public async Task<IActionResult> GetDistricts(int provinceId)
        {
            var districts = await _ghnService.GetDistrictsAsync(provinceId);
            return Ok(districts);
        }

        // API để lấy danh sách phường/xã
        [HttpGet]
        public async Task<IActionResult> GetWards(int districtId)
        {
            var wards = await _ghnService.GetWardsAsync(districtId);
            return Ok(wards);
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
        public async Task<IActionResult> Checkout()
        {
            if (Cart.Count == 0)
            {
                return RedirectToAction("Index");
            }
            ViewBag.PaypalClientId = _paypalClient.ClientId;

            var customerId = HttpContext.User.Claims.SingleOrDefault(p => p.Type == MySetting.CLAIM_CUSTOMERID)?.Value;
            var khachHang = db.KhachHangs.SingleOrDefault(p => p.MaKh == customerId);

            // Truyền thông tin khách hàng vào ViewBag
            ViewBag.CustomerName = khachHang?.HoTen ?? "";
            ViewBag.CustomerPhone = khachHang?.DienThoai ?? "";
            ViewBag.CustomerAddress = khachHang?.DiaChi ?? "";

            // **THÊM MỚI: Truyền thông tin GHN**
            ViewBag.CustomerProvinceId = khachHang?.ProvinceId ?? 0;
            ViewBag.CustomerDistrictId = khachHang?.DistrictId ?? 0;
            ViewBag.CustomerWardCode = khachHang?.WardCode ?? "";

            int tongTienHang = (int)Cart.Sum(p => p.ThanhTien);
            int phiVanChuyen = 0;
            bool freeShip = tongTienHang >= 500000;
            ViewBag.FreeShip = freeShip;

            double giamGia = 0;
            var maGiamGia = HttpContext.Session.GetString("MaGiamGia");
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

            HttpContext.Session.SetInt32("PhiVanChuyen", phiVanChuyen);
            HttpContext.Session.SetString("GiamGia", giamGia.ToString("F2"));

            ViewBag.Provinces = await _ghnService.GetProvincesAsync();

            return View(Cart);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Checkout(CheckoutVM model, string payment = "COD")
        {
            var gioHang = Cart;
            int tongTienHang = (int)gioHang.Sum(p => p.ThanhTien);

            // Lấy phí vận chuyển từ Session (đã được tính bởi JavaScript)
            int phiVanChuyen = HttpContext.Session.GetInt32("PhiVanChuyen") ?? 0;

            // Nếu đơn hàng >= 500K thì miễn phí ship
            if (tongTienHang >= 500000)
            {
                phiVanChuyen = 0;
            }
            // Nếu không có phí trong session và có thông tin địa chỉ, tính lại
            else if (phiVanChuyen == 0 && model.DistrictId > 0 && !string.IsNullOrEmpty(model.WardCode))
            {
                try
                {
                    // Danh sách nội thành TP.HCM
                    var noiThanhHCM = new List<int>
            {
                1442, 1443, 1444, 1445, 1446, 1447, 1448, 1449, 1450, 1451,
                1452, 1453, 1454, 1455, 1456, 1457, 1458, 1459, 1460, 1463
            };

                    if (noiThanhHCM.Contains(model.DistrictId))
                    {
                        // Nội thành: 20K cố định
                        phiVanChuyen = 20000;
                    }
                    else
                    {
                        // Ngoại thành: tính theo GHN
                        var totalWeight = gioHang.Sum(p => p.SoLuong * 500);
                        phiVanChuyen = await _ghnService.CalculateShippingFeeAsync(model.DistrictId, model.WardCode, totalWeight);
                    }

                    HttpContext.Session.SetInt32("PhiVanChuyen", phiVanChuyen);
                }
                catch
                {
                    ModelState.AddModelError("", "Không thể tính phí vận chuyển. Vui lòng chọn lại địa chỉ.");
                    ViewBag.PaypalClientId = _paypalClient.ClientId;
                    ViewBag.Provinces = await _ghnService.GetProvincesAsync();
                    return View(Cart);
                }
            }

            double giamGia = 0;
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
                    giamGia = Math.Min(giamGia, tongTienHang);
                }
                else
                {
                    ModelState.AddModelError("MaGiamGia", "Mã giảm giá không hợp lệ hoặc hết hạn.");
                    ViewBag.PaypalClientId = _paypalClient.ClientId;
                    ViewBag.Provinces = await _ghnService.GetProvincesAsync();
                    return View(Cart);
                }
            }

            int tongCong = tongTienHang - (int)giamGia + phiVanChuyen;

            // LƯU THÔNG TIN CHECKOUT VÀO SESSION (cho VNPay/PayPal)
            HttpContext.Session.SetString("CheckoutHoTen", model.HoTen ?? "");
            HttpContext.Session.SetString("CheckoutDienThoai", model.DienThoai ?? "");
            HttpContext.Session.SetString("CheckoutDiaChi", model.DiaChi ?? "");
            HttpContext.Session.SetString("CheckoutGhiChu", model.GhiChu ?? "");
            HttpContext.Session.SetInt32("CheckoutProvinceId", model.ProvinceId);
            HttpContext.Session.SetInt32("CheckoutDistrictId", model.DistrictId);
            HttpContext.Session.SetString("CheckoutWardCode", model.WardCode ?? "");
            HttpContext.Session.SetString("CheckoutGiongKhachHang", model.GiongKhachHang.ToString());

            if (payment == "Thanh toán VNPay")
            {
                var vnpayModel = new VnPaymentRequestModel
                {
                    Amount = tongCong,
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

                string diaChiDayDu = model.DiaChi ?? khachHang?.DiaChi ?? "";

                //RÚT GỌN NỘI DUNG GHI CHÚ
                string ghiChuFinal = string.IsNullOrWhiteSpace(model.GhiChu)
                    ? "Thanh toán khi nhận hàng (COD)"
                    : $"Thanh toán khi nhận hàng (COD) - {model.GhiChu}";

                var hoadon = new HoaDon
                {
                    MaKh = customerId,
                    DiaChi = diaChiDayDu,
                    ProvinceId = model.ProvinceId,
                    DistrictId = model.DistrictId,
                    WardCode = model.WardCode,
                    DienThoai = model.DienThoai ?? khachHang?.DienThoai,
                    HoTen = model.HoTen ?? khachHang?.HoTen,
                    NgayDat = DateTime.Now,
                    CachThanhToan = "Thanh toán khi nhận hàng",
                    CachVanChuyen = "Giao hàng tận nơi",
                    MaTrangThai = 0,
                    GhiChu = ghiChuFinal,
                    PhiVanChuyen = phiVanChuyen,
                    GiamGia = (float)giamGia
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

                    if (giamGia > 0)
                    {
                        var coupon = db.MaGiamGias.Single(c => c.Code == model.MaGiamGia);
                        coupon.SoLuongDaDung++;
                        db.Update(coupon);
                        db.SaveChanges();
                    }

                    transaction.Commit();
                    HttpContext.Session.Set<List<CartItem>>(MySetting.CART_KEY, new List<CartItem>());
                    HttpContext.Session.Remove("PhiVanChuyen");

                    ViewBag.PaymentMethod = "COD";
                    ViewBag.MaHd = hoadon.MaHd;
                    return View("Success");
                }
                catch
                {
                    db.Database.RollbackTransaction();
                    ModelState.AddModelError("", "Đặt hàng không thành công. Vui lòng thử lại sau.");
                    ViewBag.PaypalClientId = _paypalClient.ClientId;
                    ViewBag.Provinces = await _ghnService.GetProvincesAsync();
                    return View(Cart);
                }
            }

            ViewBag.PaypalClientId = _paypalClient.ClientId;
            ViewBag.Provinces = await _ghnService.GetProvincesAsync();
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

                // LẤY THÔNG TIN TỪ SESSION
                var hoTen = HttpContext.Session.GetString("CheckoutHoTen") ?? "";
                var dienThoai = HttpContext.Session.GetString("CheckoutDienThoai") ?? "";
                var diaChi = HttpContext.Session.GetString("CheckoutDiaChi") ?? "";
                var ghiChu = HttpContext.Session.GetString("CheckoutGhiChu") ?? "";
                var provinceId = HttpContext.Session.GetInt32("CheckoutProvinceId") ?? 0;
                var districtId = HttpContext.Session.GetInt32("CheckoutDistrictId") ?? 0;
                var wardCode = HttpContext.Session.GetString("CheckoutWardCode") ?? "";

                // XỬ LÝ GHI CHÚ
                string ghiChuFinal = string.IsNullOrWhiteSpace(ghiChu)
                    ? "Thanh toán thành công qua PayPal"
                    : $"Thanh toán thành công qua PayPal - {ghiChu}";

                var hoadon = new HoaDon
                {
                    MaKh = customerId,
                    DiaChi = !string.IsNullOrWhiteSpace(diaChi) ? diaChi : khachHang?.DiaChi,
                    ProvinceId = provinceId > 0 ? provinceId : khachHang?.ProvinceId,
                    DistrictId = districtId > 0 ? districtId : khachHang?.DistrictId,
                    WardCode = !string.IsNullOrWhiteSpace(wardCode) ? wardCode : khachHang?.WardCode,
                    DienThoai = !string.IsNullOrWhiteSpace(dienThoai) ? dienThoai : khachHang?.DienThoai,
                    HoTen = !string.IsNullOrWhiteSpace(hoTen) ? hoTen : khachHang?.HoTen,
                    NgayDat = DateTime.Now,
                    CachThanhToan = "Thanh toán qua Paypal",
                    CachVanChuyen = "Giao hàng tận nơi",
                    MaTrangThai = 0,
                    GhiChu = ghiChuFinal,
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

                // XÓA THÔNG TIN CHECKOUT TRONG SESSION
                HttpContext.Session.Remove("CheckoutHoTen");
                HttpContext.Session.Remove("CheckoutDienThoai");
                HttpContext.Session.Remove("CheckoutDiaChi");
                HttpContext.Session.Remove("CheckoutGhiChu");
                HttpContext.Session.Remove("CheckoutProvinceId");
                HttpContext.Session.Remove("CheckoutDistrictId");
                HttpContext.Session.Remove("CheckoutWardCode");
                HttpContext.Session.Remove("PhiVanChuyen");
                HttpContext.Session.Remove("GiamGia");
                HttpContext.Session.Remove("MaGiamGia");

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

        #region VNPay callback
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
            var phiVanChuyen = HttpContext.Session.GetInt32("PhiVanChuyen") ?? 30000;
            var giamGiaStr = HttpContext.Session.GetString("GiamGia");
            var giamGia = string.IsNullOrEmpty(giamGiaStr) ? 0 : double.Parse(giamGiaStr);

            // LẤY THÔNG TIN TỪ SESSION
            var hoTen = HttpContext.Session.GetString("CheckoutHoTen") ?? "";
            var dienThoai = HttpContext.Session.GetString("CheckoutDienThoai") ?? "";
            var diaChi = HttpContext.Session.GetString("CheckoutDiaChi") ?? "";
            var ghiChu = HttpContext.Session.GetString("CheckoutGhiChu") ?? "";
            var provinceId = HttpContext.Session.GetInt32("CheckoutProvinceId") ?? 0;
            var districtId = HttpContext.Session.GetInt32("CheckoutDistrictId") ?? 0;
            var wardCode = HttpContext.Session.GetString("CheckoutWardCode") ?? "";

            // Lấy thông tin khách hàng
            var customerId = HttpContext.User.Claims.SingleOrDefault(p => p.Type == MySetting.CLAIM_CUSTOMERID)?.Value;
            var khachHang = db.KhachHangs.SingleOrDefault(p => p.MaKh == customerId);

            // XỬ LÝ GHI CHÚ
            string ghiChuFinal = string.IsNullOrWhiteSpace(ghiChu)
                ? "Thanh toán thành công qua VNPay"
                : $"Thanh toán thành công qua VNPay - Ghi chú: {ghiChu}";

            // Tạo hóa đơn
            var hoadon = new HoaDon
            {
                MaKh = customerId,
                DiaChi = !string.IsNullOrWhiteSpace(diaChi) ? diaChi : (khachHang?.DiaChi ?? "Địa chỉ mặc định"),
                ProvinceId = provinceId > 0 ? provinceId : khachHang?.ProvinceId,
                DistrictId = districtId > 0 ? districtId : khachHang?.DistrictId,
                WardCode = !string.IsNullOrWhiteSpace(wardCode) ? wardCode : khachHang?.WardCode,
                DienThoai = !string.IsNullOrWhiteSpace(dienThoai) ? dienThoai : (khachHang?.DienThoai ?? "SĐT mặc định"),
                HoTen = !string.IsNullOrWhiteSpace(hoTen) ? hoTen : (khachHang?.HoTen ?? "Tên KH mặc định"),
                NgayDat = DateTime.Now,
                CachThanhToan = "Thanh toán VNPay",
                CachVanChuyen = "Giao hàng tận nơi",
                MaTrangThai = 0,
                GhiChu = ghiChuFinal,
                PhiVanChuyen = phiVanChuyen,
                GiamGia = (float)giamGia
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

                //XÓA THÔNG TIN CHECKOUT TRONG SESSION
                HttpContext.Session.Remove("CheckoutHoTen");
                HttpContext.Session.Remove("CheckoutDienThoai");
                HttpContext.Session.Remove("CheckoutDiaChi");
                HttpContext.Session.Remove("CheckoutGhiChu");
                HttpContext.Session.Remove("CheckoutProvinceId");
                HttpContext.Session.Remove("CheckoutDistrictId");
                HttpContext.Session.Remove("CheckoutWardCode");
                HttpContext.Session.Remove("PhiVanChuyen");
                HttpContext.Session.Remove("GiamGia");
                HttpContext.Session.Remove("MaGiamGia");

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
        #endregion

        [HttpPost]
        public IActionResult AddToCartAjax(int id, int quantity = 1)
        {
            try
            {
                var giohang = Cart;
                var item = giohang.SingleOrDefault(p => p.MaHh == id);

                if (item == null)
                {
                    var hanghoa = db.HangHoas.SingleOrDefault(p => p.MaHh == id);
                    if (hanghoa == null)
                    {
                        return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
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

                var totalQuantity = giohang.Sum(p => p.SoLuong);
                var totalAmount = giohang.Sum(p => p.ThanhTien);

                return Json(new
                {
                    success = true,
                    message = $"Đã thêm {item.TenHH} vào giỏ hàng",
                    totalQuantity = totalQuantity,
                    totalAmount = totalAmount,
                    productName = item.TenHH
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GetCartDropdown() 
        {
            var cart = Cart;
            var model = new CartModel
            {
                Quantity = cart.Sum(p => p.SoLuong),
                Total = cart.Sum(p => p.ThanhTien),
                Items = cart
            };

            return PartialView("~/Views/Shared/Components/Cart/CartPanel.cshtml", model);
        }

    }
}

