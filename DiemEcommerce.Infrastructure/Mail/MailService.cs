using DiemEcommerce.Application.Abstractions;
using DiemEcommerce.Infrastructure.DependencyInjection.Options;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DiemEcommerce.Infrastructure.Mail;

public class MailService : IMailService
{
    private readonly MailOption _mailOptions;

    public MailService(IOptions<MailOption> mailOptions)
    {
        _mailOptions = mailOptions.Value;
    }

    public async Task SendMail(MailContent mailContent)
    {
        MimeMessage email = new();
        email.Sender = new MailboxAddress(_mailOptions?.DisplayName, _mailOptions?.Mail);
        email.From.Add(new MailboxAddress(_mailOptions?.DisplayName, _mailOptions?.Mail));
        email.To.Add(MailboxAddress.Parse(mailContent.To));
        email.Subject = mailContent.Subject;


        BodyBuilder builder = new();
        builder.HtmlBody = mailContent.Body;
        email.Body = builder.ToMessageBody();

        // dùng SmtpClient của MailKit
        using MailKit.Net.Smtp.SmtpClient smtp = new();
        
        await smtp.ConnectAsync(_mailOptions?.Host, _mailOptions!.Port, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_mailOptions.Mail, _mailOptions.Password);
        await smtp.SendAsync(email);
        
        // try
        // {
        //     smtp.Connect(_mailOptions?.Host, _mailOptions!.Port, SecureSocketOptions.StartTls);
        //     smtp.Authenticate(_mailOptions.Mail, _mailOptions.Password);
        //     await smtp.SendAsync(email);
        // }
        // catch (Exception ex)
        // {
        //     // Gửi mail thất bại, nội dung email sẽ lưu vào thư mục mailssave
        //     // System.IO.Directory.CreateDirectory("mailssave");
        //     // var emailsavefile = string.Format(@"mailssave/{0}.eml", Guid.NewGuid());
        //     // await email.WriteToAsync(emailsavefile);
        //
        //     System.Console.WriteLine("errors: ", ex);
        //
        //     // logger.LogInformation("Lỗi gửi mail, lưu tại - " + emailsavefile);
        //     // logger.LogError(ex.Message);
        // }

        await smtp.DisconnectAsync(true);
    }
}