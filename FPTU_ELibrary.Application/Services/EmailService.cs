using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Services.IServices;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Serilog;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;
namespace FPTU_ELibrary.Application.Services
{
	public class EmailService : IEmailService
	{
		private readonly IConfiguration _configuration;
		private readonly ILogger _logger;

		public EmailService(IConfiguration configuration, ILogger logger)
		{
			_configuration = configuration;
			_logger = logger;
		}

		public async Task<MimeMessage> ConstructEmailMessageAsync(EmailMessageDto message, bool isBodyHtml = false)
		{
			// Initialize MimeMessage
			var emailMsg = new MimeMessage();
			// Add sender
			emailMsg.From.Add(new MailboxAddress("email", _configuration["EmailSettings:From"]));
			// Add list of recipients
			emailMsg.To.AddRange(message.To);

			// Add CC recipients if provided
			if (message.Cc != null && message.Cc.Any())
			{
				emailMsg.Cc.AddRange(message.Cc);
			}
			// Add BCC recipients if provided
			if (message.Bcc != null && message.Bcc.Any())
			{
				emailMsg.Bcc.AddRange(message.Bcc);
			}

			// Email subject
			emailMsg.Subject = message.Subject;

			// Inititalize BodyBuilder
			var builder = new BodyBuilder();
			// Email content
			if (!isBodyHtml) // Is plain text
			{
				builder.TextBody = message.Content;
			}
			else // Is Html body
			{
				builder.HtmlBody = message.Content;
			}

			// Add attachments if any
			if (message.Attachments != null && message.Attachments.Any())
			{
				foreach (var attachment in message.Attachments)
				{
					builder.Attachments.Add(attachment.FileName, attachment.FileBytes, ContentType.Parse(attachment.ContentType));
				}
			}

			// Construct message body based on the text-based bodies, the link resources, and the attachments
			emailMsg.Body = builder.ToMessageBody();

			return await Task.FromResult(emailMsg);
		}

		public async Task<bool> SendAsync(MimeMessage mailMessage)
		{
			var smtpHost = _configuration["EmailSettings:SmtpHost"];
			var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]!);
			var username = _configuration["EmailSettings:SmtpCredential:UserName"];
			var password = _configuration["EmailSettings:SmtpCredential:Password"];

			using var client = new SmtpClient();
			try
			{
				// Connect to SMTP server with SSL
				await client.ConnectAsync(smtpHost, smtpPort, true);

				// Authenticate
				await client.AuthenticateAsync(username, password);

				// Asynchronously send specified message
				await client.SendAsync(mailMessage);

				_logger.Information("Email sent successfully to {Recipients}.",
					string.Join(", ", mailMessage.To.Select(r => r.ToString())));

				return true;
			}
			catch (SmtpCommandException ex)
			{
				// Log command-specific errors
				_logger.Error("SMTP command error: {ErrorCode}, {Message}",
					ex.StatusCode, ex.Message);
				throw new Exception("Error invoke when progress send email");
			}
			catch (SmtpProtocolException ex)
			{
				// Log protocol-specific errors
				_logger.Error("SMTP protocol error: {Message}", ex.Message);
				throw new Exception("Error invoke when progress send email");
			}
			catch (Exception ex)
			{
				// Catch-all for unexpected exceptions
				_logger.Error(ex, "Unexpected error while sending email: {Message}", ex.Message);
				throw new Exception("Error invoke when progress send email");
			}
			finally
			{
				if (client.IsConnected)
				{
					await client.DisconnectAsync(true); 
				}

				client.Dispose(); 
			}
		}

		public async Task<bool> SendEmailAsync(EmailMessageDto message, bool isBodyHtml = false)
		{
			// Construct email message
			var emailMSg = await ConstructEmailMessageAsync(message, isBodyHtml);
			// Progress send email
			return await SendAsync(emailMSg);
		}
	}
}
