using Microsoft.AspNetCore.Http;

namespace DiemEcommerce.Application.Abstractions;

public interface IMediaService
{
     Task<string> UploadImageAsync(IFormFile file);
}