namespace FPTU_ELibrary.Application.Configurations;

public class BorrowSettings
{
    public int BorrowRequestExpirationInDays { get; set; }
    public int BorrowAmountOnceTime { get; set; }
    public int TotalMissedPickUpAllow { get; set; }
    public int EndSuspensionInDays { get; set; }
    public int MaxBorrowExtension { get; set; }
    public int AllowToExtendInDays { get; set; }
    public int TotalBorrowExtensionInDays { get; set; }
    public int OverdueOrLostHandleInDays { get; set; }
}