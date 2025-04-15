using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DiemEcommerce.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace DiemEcommerce.Infrastructure.Media;

public class CloudinaryService : IMediaService
{
    private readonly Cloudinary _cloudinary;
    
    public CloudinaryService(Cloudinary cloudinary)
    {
        _cloudinary = cloudinary;
    }
    
    public async Task<string> UploadImageAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty or null.", nameof(file));
        }
        if (!IsImageFile(file))
        {
            throw new ArgumentException("File is not a valid image.", nameof(file));
        }
        using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream)
        };
        var uploadResult = await _cloudinary.UploadAsync(uploadParams);
        return uploadResult.SecureUrl.ToString();
    }
    
    private bool IsImageFile(IFormFile file)
    {
        // This is a basic check. For more robust validation, consider using a library like MimeDetective
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedExtensions.Contains(fileExtension);
    }
}