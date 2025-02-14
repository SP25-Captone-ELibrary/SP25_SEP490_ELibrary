using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Specifications.Params;

public class LibraryCardSpecParams : BaseSpecParams
{
    public LibraryCardIssuanceMethod? IssuanceMethod { get; set; }
    public LibraryCardStatus? Status { get; set; }
    
    public bool? IsAllowToBorrowMore { get; set; }
    public bool? IsReminderSent { get; set; }
    public bool? IsExtended { get; set; }
    public bool? IsArchived { get; set; }

    public DateTime?[]? IssueDateRange { get; set; }
    public DateTime?[]? ExpiryDateRange { get; set; }
    public DateTime?[]? SuspensionEndDateRange { get; set; }
}