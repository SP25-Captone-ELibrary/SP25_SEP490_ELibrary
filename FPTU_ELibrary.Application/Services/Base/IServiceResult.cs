using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPTU_ELibrary.Application.Services.Base
{
	public interface IServiceResult
	{
		int Status { get; set; }
		string? Message { get; set; }
		object? Data { get; set; }
	}
}
