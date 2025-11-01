
namespace PhongNguyenPuppy_MVC.Helpers
{
    public class SeoData
    {
        public string Title { get; set; } = "PhongNguyen Puppy - Chuyên cung cấp Thức ăn & Đồ dùng dành cho chó";
        public string Description { get; set; } = "PhongNguyen Puppy Shop cung cấp thức ăn, đồ dùng chất lượng cao cho chó cưng của bạn. Giao hàng toàn quốc, giá tốt nhất.";
        public string Keywords { get; set; } = "thức ăn chó, đồ dùng chó, phụ kiện chó, puppy shop";
        public string ImageUrl { get; set; } = "/images/default-og-image.jpg";
        public string Url { get; set; } = string.Empty;
        public string CanonicalUrl { get; set; } = string.Empty; // URL canonical chính thức
        public string Type { get; set; } = "website";
        public string SiteName { get; set; } = "PhongNguyen Puppy Shop";
        public string Author { get; set; } = "PhongNguyen Puppy";
    }

    public static class SeoHelper
    {
        public static string GenerateSlug(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            // Chuyển về chữ thường
            text = text.ToLower();

            // Thay thế ký tự có dấu
            var vietnameseMap = new Dictionary<string, string>
            {
                {"á|à|ả|ã|ạ|ă|ắ|ặ|ằ|ẳ|ẵ|â|ấ|ầ|ẩ|ẫ|ậ", "a"},
                {"đ", "d"},
                {"é|è|ẻ|ẽ|ẹ|ê|ế|ề|ể|ễ|ệ", "e"},
                {"í|ì|ỉ|ĩ|ị", "i"},
                {"ó|ò|ỏ|õ|ọ|ô|ố|ồ|ổ|ỗ|ộ|ơ|ớ|ờ|ở|ỡ|ợ", "o"},
                {"ú|ù|ủ|ũ|ụ|ư|ứ|ừ|ử|ữ|ự", "u"},
                {"ý|ỳ|ỷ|ỹ|ỵ", "y"}
            };

            foreach (var pair in vietnameseMap)
            {
                text = System.Text.RegularExpressions.Regex.Replace(text, pair.Key, pair.Value);
            }

            // Loại bỏ ký tự đặc biệt, chỉ giữ chữ, số và dấu gạch ngang
            text = System.Text.RegularExpressions.Regex.Replace(text, @"[^a-z0-9\s-]", "");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s", "-");

            return text;
        }

        /// <summary>
        /// Tạo canonical URL chuẩn (loại bỏ query string không cần thiết)
        /// </summary>
        public static string GetCanonicalUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return string.Empty;

            // Loại bỏ query parameters không cần thiết (giữ lại những cái quan trọng)
            var uri = new Uri(url);
            var baseUrl = $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";

            return baseUrl.TrimEnd('/');
        }

        /// <summary>
        /// Chuẩn hóa URL (lowercase, remove trailing slash)
        /// </summary>
        public static string NormalizeUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return string.Empty;

            url = url.ToLower();
            url = url.TrimEnd('/');

            return url;
        }

        /// <summary>
        /// Tạo canonical URL từ HttpRequest và loại bỏ tracking parameters
        /// </summary>
        public static string BuildCanonicalUrl(HttpRequest request, params string[] allowedParams)
        {
            var baseUrl = $"{request.Scheme}://{request.Host}{request.Path}";

            if (allowedParams.Length == 0)
                return baseUrl;

            var queryParams = request.Query
                .Where(q => allowedParams.Contains(q.Key))
                .Select(q => $"{q.Key}={Uri.EscapeDataString(q.Value)}");

            return queryParams.Any()
                ? $"{baseUrl}?{string.Join("&", queryParams)}"
                : baseUrl;
        }

        /// <summary>
        /// Chuẩn hóa description SEO (140-160 ký tự)
        /// </summary>
        public static string NormalizeDescription(string description, int minLength = 140, int maxLength = 160)
        {
            if (string.IsNullOrEmpty(description))
                return string.Empty;

            // Loại bỏ khoảng trắng thừa
            description = System.Text.RegularExpressions.Regex.Replace(description, @"\s+", " ").Trim();

            // Nếu quá ngắn, cảnh báo (nhưng vẫn trả về)
            if (description.Length < minLength)
            {
                // Log warning nếu cần
                return description;
            }

            // Nếu quá dài, cắt bớt tại từ cuối cùng
            if (description.Length > maxLength)
            {
                var trimmed = description.Substring(0, maxLength - 3);
                var lastSpace = trimmed.LastIndexOf(' ');
                if (lastSpace > maxLength - 20) // Đảm bảo không cắt quá nhiều
                {
                    return trimmed.Substring(0, lastSpace) + "...";
                }
                return trimmed + "...";
            }

            return description;
        }
    }
}