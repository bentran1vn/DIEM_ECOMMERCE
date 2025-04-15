using System.ComponentModel.DataAnnotations;

namespace DiemEcommerce.Infrastructure.DependencyInjection.Options;

public class VnPayOption
{
    [Required]public string BaseUrl { get; set; }
    [Required]public string Command { get; set; }
    [Required]public string CurrCode { get; set; }
    [Required]public string Version { get; set; }
    [Required]public string Locale { get; set; }
    [Required]public string TmnCode { get; set; }
    [Required]public string HashSecret { get; set; }
    [Required]public string UrlCallBack { get; set; }
}