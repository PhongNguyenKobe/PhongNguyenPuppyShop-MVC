using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhongNguyenPuppy_MVC.Data;
using System.Security.Claims;

namespace PhongNguyenPuppy_MVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminController : Controller
    {
        private readonly PhongNguyenPuppyContext _context;

        public AdminController(PhongNguyenPuppyContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string email, string password)
        {
            var nhanVien = _context.NhanViens.FirstOrDefault(nv => nv.Email == email);

            if (nhanVien == null || nhanVien.MatKhau != password)
            {
                ModelState.AddModelError("", "Tài khoản hoặc mật khẩu không đúng.");
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, nhanVien.HoTen),
                new Claim(ClaimTypes.Email, nhanVien.Email),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var claimsIdentity = new ClaimsIdentity(claims, "AdminScheme");

            await HttpContext.SignInAsync("AdminScheme", new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        [Authorize(AuthenticationSchemes = "AdminScheme", Roles = "Admin")]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize(AuthenticationSchemes = "AdminScheme", Roles = "Admin")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("AdminScheme");
            return RedirectToAction("Login", "Admin", new { area = "Admin" });
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
