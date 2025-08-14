using System.Linq;
using Microsoft.AspNetCore.Mvc;
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

        public KhachHangController(
            IKhachHangRepository khachHangRepository,
            IDichVuGuiEmail dichVuGuiEmail,
            IDichVuThongKe dichVuThongKe)
        {
            _khachHangRepository = khachHangRepository;
            _dichVuGuiEmail = dichVuGuiEmail;
            _dichVuThongKe = dichVuThongKe;
        }

        // 1. Hiển thị danh sách khách hàng
        public IActionResult DanhSach(string tuKhoa = "", int trang = 1)
        {
            var ketQua = _khachHangRepository.LayTatCa(tuKhoa, trang); // chỉ truyền đúng số tham số

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
                DanhSachEmail = _khachHangRepository.LayDanhSachEmailKhachHang()
            };

            return View(vm);
        }


        [HttpPost]
        public IActionResult GuiEmail(GuiEmailVM vm)
        {
            if (string.IsNullOrEmpty(vm.TieuDe) || string.IsNullOrEmpty(vm.NoiDung) || vm.DanhSachEmail == null || !vm.DanhSachEmail.Any())
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin email.");
                return View(vm);
            }

            foreach (var email in vm.DanhSachEmail)
            {
                _dichVuGuiEmail.Gui(email, vm.TieuDe, vm.NoiDung);
            }

            TempData["ThongBao"] = "Email đã được gửi thành công!";
            return RedirectToAction("GuiEmail");
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
