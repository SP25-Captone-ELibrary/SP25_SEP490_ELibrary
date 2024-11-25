using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPTU_ELibrary.Application.Dtos
{
	public class JobRoleDto
	{
		// Key
		public int JobRoleId { get; set; }

		// Job role detail
		public string EnglishName { get; set; } = null!;
		public string VietnameseName { get; set; } = null!;
	}
}
