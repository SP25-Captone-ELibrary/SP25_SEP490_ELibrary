using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Specifications.Params;

public class LibraryCardHolderSpecParams : BaseSpecParams
{
    public Gender? Gender { get; set; }
    public DateTime?[]? DobRange { get; set; }
    
    // Library card filtering
    public LibraryCardIssuanceMethod? IssuanceMethod { get; set; }
    public LibraryCardStatus? CardStatus { get; set; }
    public bool? IsAllowBorrowMore { get; set; }
    public bool? IsReminderSent { get; set; }
    public bool? IsExtended { get; set; }
    public bool? IsArchived { get; set; }
    public DateTime?[]? CardIssueDateRange { get; set; }
    public DateTime?[]? CardExpiryDateRange { get; set; }
    public DateTime?[]? SuspensionDateRange { get; set; }
}