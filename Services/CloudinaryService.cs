using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace SmartBorrowLK.Services
{
    public interface ICloudinaryService
    {
        Task<string> UploadImageAsync(IFormFile file);
        Task<string> UploadImageAsync(byte[] imageBytes, string fileName);
    }

    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;

        public CloudinaryService(IConfiguration config, ILogger<CloudinaryService> logger)
        {
            _logger = logger;
            var account = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return string.Empty;

            try
            {
                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Transformation = new Transformation().Height(500).Width(500).Crop("fill").Gravity("face")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                _logger.LogInformation("Cloudinary upload success: {Url}", uploadResult.SecureUrl);
                return uploadResult.SecureUrl.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cloudinary upload failed");
                return string.Empty;
            }
        }

        public async Task<string> UploadImageAsync(byte[] imageBytes, string fileName)
        {
            if (imageBytes == null || imageBytes.Length == 0) return string.Empty;

            try
            {
                using var stream = new MemoryStream(imageBytes);
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(fileName, stream),
                    Transformation = new Transformation().Height(500).Width(500).Crop("fill").Gravity("face")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                _logger.LogInformation("Cloudinary upload (bytes) success: {Url}", uploadResult.SecureUrl);
                return uploadResult.SecureUrl.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cloudinary upload (bytes) failed");
                return string.Empty;
            }
        }
    }
}