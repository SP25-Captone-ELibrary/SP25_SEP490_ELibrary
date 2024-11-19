using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPTU_ELibrary.API.Configurations
{
	public class ElasticSettings
	{
		public string Url { get; set; } = string.Empty;
		public string DefaultIndex { get; set; } = string.Empty;
		public string Username { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
	}
}
