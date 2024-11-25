using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace FPTU_ELibrary.Application.Dtos
{
	public class EmailAttachmentDto
	{
		public string FileName { get; set; } = null!;
		public byte[] FileBytes { get; set; } = null!;
		public string ContentType { get; set; } = null!; // Using MediaTypeNames
	}
}
