using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhongNguyenPuppy_MVC.Data;
using PhongNguyenPuppy_MVC.Helpers;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace PhongNguyenPuppy_MVC.Controllers
{
    public class SitemapController : Controller
    {
        private readonly PhongNguyenPuppyContext _context;
        private readonly IConfiguration _configuration;

        public SitemapController(PhongNguyenPuppyContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [Route("/sitemap.xml")]
        public async Task<IActionResult> Index()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var sitemapNodes = new List<SitemapNode>();

            // 1. Trang chủ
            sitemapNodes.Add(new SitemapNode(baseUrl, "daily", "1.0"));

            // 2. Các trang tĩnh khác (ví dụ: liên hệ, giới thiệu)
            sitemapNodes.Add(new SitemapNode($"{baseUrl}/contact.html", "monthly", "0.7"));
            sitemapNodes.Add(new SitemapNode($"{baseUrl}/dang-ky-nhan-tin", "monthly", "0.6"));

            // 3. Tất cả sản phẩm
            var products = await _context.HangHoas.ToListAsync();
            foreach (var product in products)
            {
                var slug = SeoHelper.GenerateSlug(product.TenHh);
                sitemapNodes.Add(new SitemapNode($"{baseUrl}/san-pham/{slug}/{product.MaHh}", "weekly", "0.9"));
            }

            // 4. Tất cả danh mục
            var categories = await _context.Loais.ToListAsync();
            foreach (var category in categories)
            {
                var slug = SeoHelper.GenerateSlug(category.TenLoai);
                sitemapNodes.Add(new SitemapNode($"{baseUrl}/danh-muc/{slug}/{category.MaLoai}", "weekly", "0.8"));
            }

            // Tạo XML
            var stream = new MemoryStream();
            var writer = new XmlTextWriter(stream, Encoding.UTF8);
            writer.Formatting = Formatting.Indented;
            writer.WriteStartDocument();
            writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

            var serializer = new XmlSerializer(typeof(SitemapNode));
            foreach (var node in sitemapNodes)
            {
                serializer.Serialize(writer, node, new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty }));
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();

            stream.Position = 0;
            return new FileStreamResult(stream, "application/xml");
        }
    }
}