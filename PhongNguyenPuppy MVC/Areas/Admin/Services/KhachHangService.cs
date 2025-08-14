using PhongNguyenPuppy_MVC.Data;

namespace PhongNguyenPuppy_MVC.Areas.Admin.Services
{
    public class KhachHangService
    {
        private readonly PhongNguyenPuppyContext _context;

        public KhachHangService(PhongNguyenPuppyContext context)
        {
            _context = context;
        }

        public List<string> LayDanhSachEmail()
        {
            return _context.KhachHangs
                           .Where(kh => !string.IsNullOrEmpty(kh.Email))
                           .Select(kh => kh.Email)
                           .Distinct()
                           .ToList();
        }
    }

}
