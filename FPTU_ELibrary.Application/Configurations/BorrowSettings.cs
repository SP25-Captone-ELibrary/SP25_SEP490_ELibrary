namespace FPTU_ELibrary.Application.Configurations;

public class BorrowSettings
{
    public int BorrowRequestExpirationInDays { get; set; }
    public int BorrowAmountOnceTime { get; set; }
    public int TotalMissedPickUpAllow { get; set; }
    public int EndSuspensionInDays { get; set; }
}