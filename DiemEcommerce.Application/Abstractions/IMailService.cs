namespace DiemEcommerce.Application.Abstractions;

public interface IMailService
{
    Task SendMail(MailContent mailContent);
}

public class MailContent
{
    public string? To { get; set; }              // Địa chỉ gửi đến
    public string? Subject { get; set; }         // Chủ đề (tiêu đề email)
    public string? Body { get; set; }            // Nội dung (hỗ trợ HTML) của email

}