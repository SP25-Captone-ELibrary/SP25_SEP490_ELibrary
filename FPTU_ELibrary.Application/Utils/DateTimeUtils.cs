using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPTU_ELibrary.Application.Utils
{
	public class DateTimeUtils
	{
		public static bool IsValidAge(DateTime date)
		{
			int currentYear = DateTime.Now.Year;
			int dobYear = date.Year;

			if (dobYear <= currentYear && dobYear > (currentYear - 120))
			{
				return true;
			}

			return false;
		}
	}
}
