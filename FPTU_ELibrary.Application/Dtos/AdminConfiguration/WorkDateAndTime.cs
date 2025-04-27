using FPTU_ELibrary.Application.Configurations;

namespace FPTU_ELibrary.Application.Dtos.AdminConfiguration;

public class WorkDateAndTime
{
    public string WeekDate { get; set; } = null!;
    public string Open { get; set; } = null!;
    public string Close { get; set; } = null!;
}
public class Schedule
{
    public List<string> Days { get; set; } = new();
    public string Open { get; set; }
    public string Close { get; set; }
}
public class PerformedLibrarySchedule
{   
    public List<Schedule> Schedules { get; set; } = new();
}

public static class PerformedLibraryScheduleExtension
{
    public static LibrarySchedule ToLibrarySchedule(this PerformedLibrarySchedule performedLibrarySchedule)
    {
        var librarySchedule = new LibrarySchedule();

        foreach (var schedule in performedLibrarySchedule.Schedules)
        {
            var daySchedule = new DaySchedule
            {
                Days = schedule.Days
                    .Select(day => Enum.Parse<DayOfWeek>(day))
                    .ToList(),
                Open = TimeSpan.Parse(schedule.Open),
                Close = TimeSpan.Parse(schedule.Close)
            };

            librarySchedule.Schedules.Add(daySchedule);
        }

        return librarySchedule;
    }
}