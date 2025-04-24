using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.LibraryItems;

namespace FPTU_ELibrary.Application.Utils
{
	public class DateTimeUtils
	{
		private readonly List<DaySchedule> _schedules;
		private readonly HashSet<(int Month, int Day)> _recurringClosures;
		private readonly HashSet<DateTime> _specificClosures;

		public DateTimeUtils()
		{
			_schedules = new();
			_recurringClosures = new();
			_specificClosures = new();
		}

		public DateTimeUtils(List<DaySchedule> schedules, List<LibraryClosureDayDto>? closureDays)
		{
			_schedules = schedules;

			if (closureDays != null)
			{
				// Extract recurring closures (Year = null)
                _recurringClosures = closureDays
                	.Where(c => !c.Year.HasValue)
                	.Select(c => (c.Month, c.Day))
                	.ToHashSet();
                
                // Extract specific closures (Year != null)
                _specificClosures = closureDays
                	.Where(c => c.Year.HasValue)
                	.Select(c => new DateTime(c.Year!.Value, c.Month, c.Day))
                	.ToHashSet();
			}
			else
			{
				// Set default closure days
				_recurringClosures = new();
				_specificClosures = new();
			}
		}
		
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

		public static DateTime TruncateToSeconds(DateTime datetime)
			=> new (datetime.Ticks - (datetime.Ticks % TimeSpan.TicksPerSecond), datetime.Kind);

		#region Calculate a due date/expiry date by adding N "library days"
		//	Summary:
		//		Is there any schedule entry for open day
		private bool IsLibraryOpen(DateTime date) =>
			_schedules.Any(s => s.Days.Contains(date.DayOfWeek));
		
		//	Summary:
		//		Check whether specific date time belongs to closure dates
		private bool IsClosed(DateTime date)
		{
			// 1. Weekly off-day
			if(!IsLibraryOpen(date))
				return true;
					
			// 2. Specific maintenance, events or something else like that
			if (_specificClosures.Contains(date.Date))
				return true;
			
			// 3. Recurring annual closure (holidays, ...)
			if (_recurringClosures.Contains((date.Month, date.Day)))
				return true;
			
			return false;
		}
		
		//	Summary:
		//		Get the closing Timespan (e.g. 19:00) for a given day
		private TimeSpan GetCloseTime(DayOfWeek day) =>
			_schedules.First(s => s.Days.Contains(day)).Close;

		//	Summary:
		//		Determine whether the given date can count as the first of valid date (date start to apply counting expiry/overdue date)
		//		or move to the next day
		private DateTime GetFirstCountableDay(DateTime performDate, bool includeSameDay = true)
		{
			// TODO: Apply mid-day logic for calculating overdue/expiry date

			// Include same day as datetime starter
			if (includeSameDay)
			{
				TimeSpan closingTime = GetCloseTime(performDate.DayOfWeek);
				bool beforeClosing = performDate.TimeOfDay < closingTime;
				
				// Apply before closing time logic
				if (!IsClosed(performDate) && beforeClosing)
				{
					return performDate.Date;
				}
			}
			
			// Find the next valid open day
			DateTime nextDate = performDate.Date.AddDays(1);
			while (IsClosed(nextDate))
			{
				nextDate = nextDate.AddDays(1);
			}

			return nextDate;
		}
		
		//	Summary:
		//		Calculate the due date or expiry date by adding specific number of days before expired or overdue
		public DateTime CalculateExpiryOrDueDate(DateTime performDate, int daysToExpireOrOverdue, bool includeSameDay = true)
		{
			// Require number of days must be greater than 0
			if (daysToExpireOrOverdue == 0) throw new Exception("The days to calculate expiry or due date parameter must be greater than 0");

			// Determine the first day to count
			DateTime currentDate = GetFirstCountableDay(performDate);
			// Initialize days counted
			int daysCounted = 1;
			
			// Loop until reach the required number of days
			while (daysCounted < daysToExpireOrOverdue)
			{
				currentDate = currentDate.AddDays(1);
				if (!IsClosed(currentDate))
				{
					daysCounted++;
				}
			}
			
			// Set to library closing time on the final day
			TimeSpan closingTime = GetCloseTime(currentDate.DayOfWeek);
			return currentDate.Date.Add(closingTime);
		}
		
		//	Summary:
		//		Accumulate datetime to retrieve the next day from the given date that not exist in weekly day-off, occasional days,...
		public DateTime GetBestDayAfters(DateTime date, int totalDays)
		{
			// Initialize days counted
            int daysCounted = 1;
            
            // Loop until reach the required number of days
            while (daysCounted < totalDays)
            {
	            date = date.AddDays(1);
            	if (!IsClosed(date))
            	{
            		daysCounted++;
            	}
            }
            
            // Set to library closing time on the final day
            TimeSpan closingTime = GetCloseTime(date.DayOfWeek);
            return date.Date.Add(closingTime);
		}
		#endregion
	}
}
