using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPTU_ELibrary.Application.Dtos
{
	public class EmailMessageDto
	{
		// List of recipients
		public List<MailboxAddress> To { get; set; } = null!;
		// List of CC
		public List<MailboxAddress> Cc { get; set; } = null!;
		// List of BCC 
		public List<MailboxAddress> Bcc { get; set; } = null!;
		// List of attachments
		public List<EmailAttachmentDto> Attachments { get; set; } = null!;
        // Email subject
        public string Subject { get; set; } = null!;
		// Plain content
		public string Content { get; set; } = null!;

        // Params constructors
        public EmailMessageDto(IEnumerable<string> to, string subject, string content)
		{
			// Iterate each email str and initialize MailboxAddress for each email str
			To = [.. to.Select(t => new MailboxAddress("email", t))];
			Subject = subject;
			Content = content;	
		}

		// Sending email with CC 
		public EmailMessageDto(IEnumerable<string> to, IEnumerable<string> cc, string subject, string content)
		{
			// Iterate each email str and initialize MailboxAddress for each email str
			To = [.. to.Select(t => new MailboxAddress("email", t))];
			Cc = [.. cc.Select(c => new MailboxAddress("email", c))];
			Subject = subject;
			Content = content;
		}

		// Sending email with CC and BCC
		public EmailMessageDto(IEnumerable<string> to, IEnumerable<string> cc, IEnumerable<string> bcc, string subject, string content)
		{
			// Iterate each email str and initialize MailboxAddress for each email str
			To = [.. to.Select(t => new MailboxAddress("email", t))];
			Cc = [.. cc.Select(c => new MailboxAddress("email", c))];
			Bcc = [.. bcc.Select(b => new MailboxAddress("email", b))];
			Subject = subject;
			Content = content;
		}
	}
}
