namespace PhongNguyenPuppy_MVC.Areas.Admin.Services
{
    public interface ICloudinaryService
    {
        Task<string> UploadImageAsync(IFormFile file);
        Task<string> UploadImageFromPathAsync(string localPath);
    }
}