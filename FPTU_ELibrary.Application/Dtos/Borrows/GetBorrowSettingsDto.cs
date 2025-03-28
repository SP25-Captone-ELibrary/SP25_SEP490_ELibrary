namespace FPTU_ELibrary.Application.Dtos.Borrows;

public class GetBorrowSettingsDto
{
    public int PickUpExpirationInDays { get; set; }
    public int ExtendPickUpInDays { get; set; }
    public int BorrowAmountOnceTime { get; set; }
    public int TotalMissedPickUpAllow { get; set; }
    public int EndSuspensionInDays { get; set; }
    public int MaxBorrowExtension { get; set; }
    public int AllowToExtendInDays { get; set; }
    public int TotalBorrowExtensionInDays { get; set; }
    public int OverdueOrLostHandleInDays { get; set; }
    public int FineExpirationInDays { get; set; }
    public int LostAmountPercentagePerDay { get; set; }
}