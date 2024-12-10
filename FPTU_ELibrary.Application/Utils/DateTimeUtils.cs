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
			int currentDayOfYear = DateTime.Now.DayOfYear;
			int dobYear = date.Year;
			int dobDayOfYear = date.DayOfYear;

			// Check if the date is in the future
			if (date > DateTime.Now)
			{
				return false;
			}

			// Check if the year is not too old
			if (dobYear <= currentYear && dobYear > (currentYear - 120))
			{
				// Disallow the current date as a valid DOB
				if (dobYear == currentYear && dobDayOfYear == currentDayOfYear)
				{
					return false;
				}

				return true;
			}

			return false;
		}
	}
}
