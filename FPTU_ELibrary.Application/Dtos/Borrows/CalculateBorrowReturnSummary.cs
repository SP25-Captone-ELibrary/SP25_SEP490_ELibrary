namespace FPTU_ELibrary.Application.Dtos.Borrows;

public class CalculateBorrowReturnSummary
{
    public int TotalRequested { get; set; }
    public int TotalBorrowed { get; set; }
    public int TotalReturned { get; set; }
    public int TotalReserved { get; set; }
    public decimal UnpaidFees { get; set; }
}