using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace PhongNguyenPuppy_MVC.Areas.Admin.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration configuration)
        {
            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                throw new Exception("Cloudinary credentials not found in configuration");
            }

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }

        /// <summary>
        /// Upload IFormFile lên Cloudinary
        /// </summary>
        public async Task<string> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File không hợp lệ");

            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "phongnguyen-puppy/email-marketing", // Tạo folder riêng
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"Cloudinary upload failed: {uploadResult.Error?.Message}");
            }

            return uploadResult.SecureUrl.ToString();
        }

        /// <summary>
        /// Upload ảnh từ đường dẫn local (wwwroot/Hinh/HangHoa) lên Cloudinary
        /// </summary>
        public async Task<string> UploadImageFromPathAsync(string localPath)
        {
            if (!File.Exists(localPath))
                throw new FileNotFoundException("File không tồn tại", localPath);

            var fileName = Path.GetFileName(localPath);

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(localPath),
                Folder = "phongnguyen-puppy/products",
                PublicId = Path.GetFileNameWithoutExtension(fileName),
                Overwrite = false // Không ghi đè nếu đã tồn tại
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"Cloudinary upload failed: {uploadResult.Error?.Message}");
            }

            return uploadResult.SecureUrl.ToString();
        }

        /// <summary>
        /// Kiểm tra xem ảnh đã tồn tại trên Cloudinary chưa
        /// </summary>
        public async Task<string?> GetExistingImageUrlAsync(string fileName)
        {
            try
            {
                var publicId = $"phongnguyen-puppy/products/{Path.GetFileNameWithoutExtension(fileName)}";
                var getResourceParams = new GetResourceParams(publicId);
                var result = await _cloudinary.GetResourceAsync(getResourceParams);

                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return result.SecureUrl;
                }
            }
            catch
            {
                // Ảnh chưa tồn tại
            }

            return null;
        }
    }
}