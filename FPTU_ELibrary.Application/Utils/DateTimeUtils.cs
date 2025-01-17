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

			// Disallow the current date as a valid DOB
			if (dobYear == currentYear && dobDayOfYear == currentDayOfYear)
			{
				return false;
			}

			// Allow all other dates in the past
			return true;
		}

		public static bool IsOver18(DateTime dateOfBirth)
		{
			// Check if the date is in the future
			if (dateOfBirth > DateTime.Now)
			{
				return false;
			}

			// Calculate the person's age
			DateTime today = DateTime.Now;
			int age = today.Year - dateOfBirth.Year;

			// Adjust age if the birthdate has not occurred yet in this year 
			if(dateOfBirth > today.AddYears(-age)) age--;

			// Check if the age is 18 or older
			return age > 18;
		}
	}
}
