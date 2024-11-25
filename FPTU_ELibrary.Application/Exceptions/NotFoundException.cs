using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPTU_ELibrary.Application.Exceptions
{
	public class NotFoundException : Exception
	{
		public NotFoundException(string name, string key) : base($"{name} ({key}) was not found")
		{
		}

		public NotFoundException(string message) : base(message)
		{
		}
	}
}
