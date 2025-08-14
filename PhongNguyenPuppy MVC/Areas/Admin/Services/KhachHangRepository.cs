using System;
using System.Linq;
using PhongNguyenPuppy_MVC.Areas.Admin.ViewModels;
using PhongNguyenPuppy_MVC.Data;
using PhongNguyenPuppy_MVC.Models;

namespace PhongNguyenPuppy_MVC.Areas.Admin.Services
{
    public class KhachHangRepository : IKhachHangRepository
    {
        private readonly PhongNguyenPuppyContext _context;

        public KhachHangRepository(PhongNguyenPuppyContext context)
        {
            _context = context;
        }

        public PagedResult<KhachHang> LayTatCa(string tuKhoa, int trang)
        {
            int pageSize = 10;

            var query = _context.KhachHangs.AsQueryable();

            if (!string.IsNullOrEmpty(tuKhoa))
            {
                query = query.Where(kh => kh.HoTen.Contains(tuKhoa) || kh.Email.Contains(tuKhoa));
            }

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var items = query
                .OrderBy(kh => kh.MaKh)
                .Skip((trang - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<KhachHang>
            {
                Items = items,
                TongSoTrang = totalPages,
                TrangHienTai = trang,
                TongSoLuong = totalItems,
                SoLuongMoiTrang = pageSize
            };
        }
        public List<string> LayDanhSachEmailKhachHang()
        {
            return _context.KhachHangs
                           .Where(kh => !string.IsNullOrEmpty(kh.Email) && kh.HieuLuc) // chỉ lấy khách hoạt động
                           .Select(kh => kh.Email)
                           .Distinct()
                           .ToList();
        }
        public KhachHang LayTheoId(string id)
        {
            return _context.KhachHangs.FirstOrDefault(kh => kh.MaKh == id);
        }

        public void CapNhat(SuaKhachHangVM vm)
        {
            var kh = _context.KhachHangs.FirstOrDefault(k => k.MaKh == vm.MaKh);
            if (kh == null) return;

            kh.HoTen = vm.HoTen;
            kh.Email = vm.Email;
            kh.DienThoai = vm.DienThoai;
            kh.HieuLuc = vm.HieuLuc;

            _context.SaveChanges();
        }

        public void Xoa(string id)
        {
            var kh = _context.KhachHangs.FirstOrDefault(k => k.MaKh == id);
            if (kh != null)
            {
                _context.KhachHangs.Remove(kh);
                _context.SaveChanges();
            }
        }


    }
}
