namespace FPTU_ELibrary.Domain.Entities;

public class LibraryClosureDay
{
    public int ClosureDayId { get; set; }
    public int Day { get; set; }
    public int Month { get; set; }
    public int? Year { get; set; }
    public string? VieDescription { get; set; }
    public string? EngDescription { get; set; }
}