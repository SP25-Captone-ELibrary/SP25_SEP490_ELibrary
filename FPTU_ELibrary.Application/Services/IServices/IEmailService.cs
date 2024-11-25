using FPTU_ELibrary.Application.Dtos;
using MimeKit;

namespace FPTU_ELibrary.Application.Services.IServices
{
	public interface IEmailService
	{
		Task<MimeMessage> ConstructEmailMessageAsync(EmailMessageDto message, bool isBodyHtml);
		Task<bool> SendEmailAsync(EmailMessageDto message, bool isBodyHtml);
		Task<bool> SendAsync(MimeMessage mailMessage);
	}
}
