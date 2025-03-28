using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Specifications.Params;

public class ReservationQueueSpecParams : BaseSpecParams
{
    public ReservationQueueStatus? QueueStatus { get; set; }
    public bool? IsReservedAfterRequestFailed { get; set; }
    public bool? IsAppliedLabel { get; set; }
    public bool? IsNotified { get; set; }
    
    public DateTime?[]? ReservationDateRange { get; set; }
    public DateTime?[]? ExpiryDateRange { get; set; }
    public DateTime?[]? AssignDateRange { get; set; }
    public DateTime?[]? CollectedDateRange { get; set; }
    public DateTime?[]? ExpectedAvailableDateMinRange { get; set; }
    public DateTime?[]? ExpectedAvailableDateMaxRange { get; set; }
}