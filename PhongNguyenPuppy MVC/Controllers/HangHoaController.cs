using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhongNguyenPuppy_MVC.Data;
using PhongNguyenPuppy_MVC.ViewModels;
using PhongNguyenPuppy_MVC.Helpers;

namespace PhongNguyenPuppy_MVC.Controllers
{
    public class HangHoaController : Controller
    {
        private readonly PhongNguyenPuppyContext db;

        public HangHoaController(PhongNguyenPuppyContext context)
        {
            db = context;
        }

        // GET: HangHoa
        public IActionResult Index(int? loai, int page = 1, string sortOrder = "", int? minPrice = null, int? maxPrice = null, string category = "", string search = "")
        {
            int pageSize = 12;
            IQueryable<HangHoa> hangHoas = db.HangHoas.Include(p => p.MaLoaiNavigation);

            if (!string.IsNullOrEmpty(search))
            {
                hangHoas = hangHoas.Where(p => p.TenHh.Contains(search));
            }
            // Lọc theo loại sản phẩm
            if (loai.HasValue)
            {
                hangHoas = hangHoas.Where(p => p.MaLoai == loai.Value);
            }

            // Lọc theo khoảng giá
            if (minPrice.HasValue)
            {
                hangHoas = hangHoas.Where(p => p.DonGia >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                hangHoas = hangHoas.Where(p => p.DonGia <= maxPrice.Value);
            }

            // Lọc theo danh mục đặc biệt
            if (!string.IsNullOrEmpty(category))
            {
                switch (category)
                {
                    case "Sale":
                    case "Discount":
                        hangHoas = hangHoas.Where(p => p.GiamGia > 0);
                        break;
                }
            }

            // Xử lý sắp xếp
            switch (sortOrder)
            {
                case "price_asc":
                    hangHoas = hangHoas.OrderBy(p => p.DonGia);
                    break;
                case "price_desc":
                    hangHoas = hangHoas.OrderByDescending(p => p.DonGia);
                    break;
                case "Fresh":
                    hangHoas = hangHoas.OrderByDescending(p => p.MaHh);
                    break;
                case "Sale":
                    hangHoas = hangHoas.Where(p => p.GiamGia > 0).OrderByDescending(p => p.GiamGia);
                    break;
                default:
                    hangHoas = hangHoas.OrderBy(p => p.MaHh);
                    break;
            }
            // ĐẾM SỐ LƯỢNG (SAU KHI ĐÃ FILTER VÀ SORT)
            var totalItems = hangHoas.Count();

            var result = hangHoas
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new HangHoaVM
                {
                    MaHh = p.MaHh,
                    TenHh = p.TenHh,
                    DonGia = p.DonGia ?? 0,
                    Hinh = p.Hinh ?? "",
                    MoTaNgan = p.MoTaDonVi ?? "",
                    TenLoai = p.MaLoaiNavigation.TenLoai
                });

            //  Tạo Title và Description động theo context
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            string title, description, h1Heading;
            string? tenLoai = null;

            if (loai.HasValue)
            {
                tenLoai = db.Loais.FirstOrDefault(l => l.MaLoai == loai.Value)?.TenLoai;
                title = $"{tenLoai} - PhongNguyen Puppy Shop"; // ~40-50 ký tự
                description = $"Khám phá {totalItems}+ sản phẩm {tenLoai} chất lượng cao cho chó cưng tại PhongNguyen Puppy. Giao hàng toàn quốc, giá tốt. Mua ngay!"; // ~145 ký tự
                h1Heading = $"{tenLoai} Chất Lượng Cao"; // 20-40 ký tự 

            }
            else if (!string.IsNullOrEmpty(category))
            {
                switch (category)
                {
                    case "Fresh":
                        title = "Sản Phẩm Mới - PhongNguyen Puppy";
                        description = "Khám phá các sản phẩm mới nhất cho chó cưng tại PhongNguyen Puppy Shop. Thức ăn, đồ dùng chất lượng cao. Cập nhật liên tục, giao hàng nhanh!"; // 152 ký tự
                        h1Heading = "Sản Phẩm Mới Nhất Cho Chó Cưng"; // 35 ký tự 
                        break;
                    case "Sale":
                    case "Discount":
                        title = "Sản Phẩm Giảm Giá - PhongNguyen Puppy";
                        description = $"Giảm giá sốc {totalItems}+ sản phẩm chất lượng cho chó cưng. Tiết kiệm đến 50%. Mua ngay!";
                        h1Heading = "Giảm Giá Sốc - Tiết Kiệm Đến 50%"; // 38 ký tự
                        break;
                    default:
                        title = "Sản Phẩm Cho Chó - PhongNguyen Puppy";
                        description = "Thức ăn, đồ dùng chất lượng cao cho chó cưng. Giao hàng nhanh, giá tốt nhất.";
                        h1Heading = "Sản Phẩm Chất Lượng Cho Chó Cưng"; // 38 ký tự 
                        break;
                }
            }
            else if (!string.IsNullOrEmpty(search))
            {
                title = $"{search} - Tìm Kiếm - PhongNguyen Puppy";
                description = $"Tìm thấy {totalItems} sản phẩm '{search}' tại PhongNguyen Puppy Shop. Thức ăn, đồ dùng chất lượng cao cho chó cưng. Giao hàng nhanh, giá tốt!"; // ~145 ký tự
                h1Heading = $"Kết Quả Tìm Kiếm: {search}"; // Động theo query 
            }
            else
            {
                title = "Sản Phẩm Cho Chó - PhongNguyen Puppy Shop"; // 47 ký tự
                description = $"Khám phá {totalItems}+ sản phẩm thức ăn, đồ dùng chất lượng cho chó cưng. Giao hàng toàn quốc, giá tốt nhất.";
                h1Heading = "Thức Ăn & Đồ Dùng Cho Chó"; // 30 ký tự 
            }
            //Chuẩn hóa description
            description = SeoHelper.NormalizeDescription(description, 140, 160);

            var canonicalUrl = loai.HasValue
                ? $"{baseUrl}/danh-muc/{SeoHelper.GenerateSlug(tenLoai ?? "")}/{loai}"
                : $"{baseUrl}/hang-hoa";

            ViewData["SeoData"] = new SeoData
            {
                Title = title,
                Description = description,
                Keywords = loai.HasValue
                    ? $"{tenLoai}, thức ăn chó, đồ dùng chó, {tenLoai} cho chó"
                    : "thức ăn chó, đồ dùng chó, phụ kiện chó, puppy shop",
                CanonicalUrl = canonicalUrl,
                Type = "product.group"
            };
            // Truyền H1 qua ViewBag
            ViewBag.H1Heading = h1Heading;
            // Truyền dữ liệu cho pagination SEO
            ViewBag.BaseCanonicalUrl = canonicalUrl;
            ViewBag.CurrentPage = page;
            // Truyền dữ liệu cho view
            ViewBag.Search = search;
            ViewBag.PageNumber = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.Loai = loai;
            ViewBag.SortOrder = sortOrder;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Category = category;

            return View(result);
        }

        // GET: HangHoa/Search
        public IActionResult Search(string query, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction("Index");
            }

            var pageSize = 12;
            var products = db.HangHoas
                .Where(p => p.TenHh.Contains(query) || p.MoTa.Contains(query))
                .Select(p => new HangHoaVM
                {
                    MaHh = p.MaHh,
                    TenHh = p.TenHh,
                    DonGia = p.DonGia ?? 0,
                    Hinh = p.Hinh ?? "",
                    MoTaNgan = p.MoTaDonVi ?? "",
                    TenLoai = p.MaLoaiNavigation.TenLoai
                })
                .AsNoTracking();

            var totalItems = products.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var result = products
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Query = query;
            ViewBag.PageNumber = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.H1Heading = $"Tìm kiếm: \"{query}\" - {totalItems} sản phẩm";

            // Thêm nội dung SEO
            ViewBag.PageContent = GetSearchPageContent(query);

            // SEO Data
            ViewData["SeoData"] = new SeoData
            {
                Title = $"Tìm kiếm \"{query}\" - {totalItems} sản phẩm | PhongNguyen Puppy Shop",
                Description = $"Tìm thấy {totalItems} sản phẩm cho từ khóa \"{query}\". Thức ăn, đồ dùng chó chất lượng cao, giá tốt. Giao hàng toàn quốc, tư vấn miễn phí.",
                Keywords = $"{query}, thức ăn chó, đồ dùng chó, phụ kiện chó, puppy shop",
                CanonicalUrl = $"{Request.Scheme}://{Request.Host}/HangHoa/Search?query={query}"
            };

            return View(result);
        }

        public IActionResult Detail(int id, string? slug)
        {
            var product = db.HangHoas
                .Include(p => p.MaLoaiNavigation)
                .SingleOrDefault(p => p.MaHh == id);

            if (product == null)
            {
                TempData["Message"] = $"Không tìm thấy sản phẩm {id}";
                return Redirect("/404");
            }

            var correctSlug = SeoHelper.GenerateSlug(product.TenHh);
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var canonicalUrl = $"{baseUrl}/san-pham/{correctSlug}/{id}";

            if (string.IsNullOrEmpty(slug) || slug != correctSlug)
            {
                return RedirectToActionPermanent("Detail", new { id, slug = correctSlug });
            }

            // Title ngắn gọn, tránh lặp từ
            var productTitle = product.TenHh.Length > 35
                ? $"{product.TenHh.Substring(0, 35)}... - PhongNguyen"
                : $"{product.TenHh} - PhongNguyen Puppy";

            var h1Heading = product.TenHh; // Tên sản phẩm làm H1 

            ViewData["SeoData"] = new SeoData
            {
                Title = productTitle,
                Description = !string.IsNullOrEmpty(product.MoTaDonVi) && product.MoTaDonVi.Length > 30
                    ? product.MoTaDonVi.Substring(0, Math.Min(155, product.MoTaDonVi.Length)) + "..."
                    : $"Mua {product.TenHh} chính hãng tại PhongNguyen Puppy. Giá {product.DonGia?.ToString("N0")}đ. Giao hàng nhanh, chất lượng đảm bảo.",
                Keywords = $"{product.TenHh}, {product.MaLoaiNavigation.TenLoai}, thức ăn chó, đồ dùng chó",
                ImageUrl = product.Hinh ?? "/images/default-product.jpg",
                CanonicalUrl = canonicalUrl,
                Type = "product"
            };

            ViewBag.H1Heading = h1Heading; 

            var result = new ChiTietHangHoaVM
            {
                MaHh = product.MaHh,
                TenHh = product.TenHh,
                DonGia = product.DonGia ?? 0,
                ChiTiet = product.MoTa ?? "",
                Hinh = product.Hinh ?? "",
                MoTaNgan = product.MoTaDonVi ?? "",
                TenLoai = product.MaLoaiNavigation.TenLoai,
                SoLuongTon = 10,
                DiemDanhGia = 5,
                RelatedProducts = db.HangHoas
                    .Where(r => r.MaLoai == product.MaLoai && r.MaHh != product.MaHh)
                    .Take(6)
                    .Select(r => new ChiTietHangHoaVM
                    {
                        MaHh = r.MaHh,
                        TenHh = r.TenHh,
                        DonGia = r.DonGia ?? 0,
                        ChiTiet = r.MoTa ?? "",
                        Hinh = r.Hinh ?? "",
                        MoTaNgan = r.MoTaDonVi ?? "",
                        TenLoai = r.MaLoaiNavigation.TenLoai,
                        SoLuongTon = 10,
                        DiemDanhGia = 5
                    }).ToList()
            };

            return View(result);
        }

        // Thêm method helper
        private PageContentVM GetSearchPageContent(string query)
        {
            return new PageContentVM
            {
                Title = $"Kết quả tìm kiếm: {query}",
                IntroText = $"Chúng tôi tìm thấy các sản phẩm phù hợp với từ khóa \"{query}\". PhongNguyen Puppy chuyên cung cấp thức ăn, đồ dùng chất lượng cao cho chó cưng với giá tốt nhất thị trường.",
                Sections = new List<ContentSection>
        {
            new ContentSection
            {
                Heading = "Tại Sao Chọn PhongNguyen Puppy?",
                Content = "PhongNguyen Puppy là cửa hàng uy tín chuyên cung cấp thức ăn và phụ kiện cho chó với hơn 5 năm kinh nghiệm. Chúng tôi cam kết mang đến sản phẩm chính hãng, chất lượng cao từ các thương hiệu nổi tiếng như Royal Canin, Pedigree, SmartHeart với giá cả cạnh tranh nhất.",
                IconClass = "fa fa-certificate"
            },
            new ContentSection
            {
                Heading = "Đa Dạng Sản Phẩm Chất Lượng",
                Content = "Chúng tôi cung cấp đầy đủ các loại thức ăn khô, thức ăn ướt, snack dinh dưỡng, sữa tắm, phụ kiện chăm sóc, đồ chơi và phụ kiện huấn luyện cho chó mọi lứa tuổi từ chó con đến chó trưởng thành. Mỗi sản phẩm đều được tuyển chọn kỹ lưỡng đảm bảo an toàn và hiệu quả.",
                IconClass = "fa fa-box-open"
            },
            new ContentSection
            {
                Heading = "Giao Hàng Toàn Quốc - Thanh Toán Linh Hoạt",
                Content = "PhongNguyen Puppy hỗ trợ giao hàng nhanh chóng toàn quốc với nhiều hình thức thanh toán tiện lợi: COD, chuyển khoản, ví điện tử. Đặc biệt miễn phí vận chuyển cho đơn hàng trên 500.000 VNĐ tại TP.HCM và các tỉnh lân cận.",
                IconClass = "fa fa-shipping-fast"
            },
            new ContentSection
            {
                Heading = "Tư Vấn Dinh Dưỡng Miễn Phí",
                Content = "Đội ngũ chuyên gia của chúng tôi luôn sẵn sàng tư vấn miễn phí về dinh dưỡng, chế độ ăn uống phù hợp cho từng giống chó, lứa tuổi và tình trạng sức khỏe. Liên hệ hotline hoặc chat online để được hỗ trợ tận tình.",
                IconClass = "fa fa-comments"
            }
        },
                BottomText = "Hãy tin tưởng lựa chọn PhongNguyen Puppy - Nơi mang đến sự chăm sóc tốt nhất cho người bạn bốn chân của bạn. Chúng tôi không chỉ bán sản phẩm mà còn đồng hành cùng bạn trong hành trình nuôi dưỡng thú cưng khỏe mạnh, hạnh phúc."
            };
        }
    }
}