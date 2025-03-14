using Microsoft.AspNetCore.Mvc;
using PhongNguyenPuppy_MVC.Data;
using PhongNguyenPuppy_MVC.ViewModels;

namespace PhongNguyenPuppy_MVC.ViewComponents
{
    public class MenuLoaiViewComponent : ViewComponent
    {
        private readonly PhongNguyenPuppyContext db;

        public MenuLoaiViewComponent(PhongNguyenPuppyContext context) => db = context;

        public IViewComponentResult Invoke()
        {
            var data = db.Loais.Select(lo => new MenuLoaiVM {
                MaLoai = lo.MaLoai,
                TenLoai = lo.TenLoai,
                SoLuong = lo.HangHoas.Count
            }).OrderBy(p => p.TenLoai);

            return View(data); // Default.cshtml
            //return View("Default",data);
        }
    }
}

