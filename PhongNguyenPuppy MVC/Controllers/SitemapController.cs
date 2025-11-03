using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhongNguyenPuppy_MVC.Data;
using PhongNguyenPuppy_MVC.Helpers;
using System.Text;

namespace PhongNguyenPuppy_MVC.Controllers
{
    public class SitemapController : Controller
    {
        private readonly PhongNguyenPuppyContext _context;

        public SitemapController(PhongNguyenPuppyContext context)
        {
            _context = context;
        }

        [Route("sitemap.xml")]
        [ResponseCache(Duration = 86400)] // Cache 24h
        public IActionResult Index()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var sitemap = new StringBuilder();

            sitemap.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sitemap.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

            // Trang chủ
            sitemap.AppendLine("<url>");
            sitemap.AppendLine($"  <loc>{baseUrl}/</loc>");
            sitemap.AppendLine($"  <lastmod>{DateTime.Now:yyyy-MM-dd}</lastmod>");
            sitemap.AppendLine("  <changefreq>daily</changefreq>");
            sitemap.AppendLine("  <priority>1.0</priority>");
            sitemap.AppendLine("</url>");

            // Trang sản phẩm
            sitemap.AppendLine("<url>");
            sitemap.AppendLine($"  <loc>{baseUrl}/hang-hoa</loc>");
            sitemap.AppendLine($"  <lastmod>{DateTime.Now:yyyy-MM-dd}</lastmod>");
            sitemap.AppendLine("  <changefreq>daily</changefreq>");
            sitemap.AppendLine("  <priority>0.9</priority>");
            sitemap.AppendLine("</url>");

            // Danh mục sản phẩm
            var categories = _context.Loais.ToList();
            foreach (var cat in categories)
            {
                var slug = SeoHelper.GenerateSlug(cat.TenLoai);
                sitemap.AppendLine("<url>");
                sitemap.AppendLine($"  <loc>{baseUrl}/danh-muc/{slug}/{cat.MaLoai}</loc>");
                sitemap.AppendLine($"  <lastmod>{DateTime.Now:yyyy-MM-dd}</lastmod>");
                sitemap.AppendLine("  <changefreq>weekly</changefreq>");
                sitemap.AppendLine("  <priority>0.8</priority>");
                sitemap.AppendLine("</url>");
            }

            // Chi tiết sản phẩm
            var products = _context.HangHoas.ToList();
            foreach (var product in products)
            {
                var slug = SeoHelper.GenerateSlug(product.TenHh);
                sitemap.AppendLine("<url>");
                sitemap.AppendLine($"  <loc>{baseUrl}/san-pham/{slug}/{product.MaHh}</loc>");
                sitemap.AppendLine($"  <lastmod>{DateTime.Now:yyyy-MM-dd}</lastmod>");
                sitemap.AppendLine("  <changefreq>weekly</changefreq>");
                sitemap.AppendLine("  <priority>0.7</priority>");
                sitemap.AppendLine("</url>");
            }

            sitemap.AppendLine("</urlset>");

            return Content(sitemap.ToString(), "application/xml", Encoding.UTF8);
        }
    }
}