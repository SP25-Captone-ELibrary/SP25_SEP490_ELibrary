using FPTU_ELibrary.Application.Dtos.Borrows;

namespace FPTU_ELibrary.Application.Dtos.Users;

public class UserPendingActivityDto
{
    public List<GetBorrowRequestDto> PendingBorrowRequests { get; set; } = new();   
    public List<GetBorrowRecordDto> ActiveBorrowRecords { get; set; } = new();
    public List<GetReservationQueueDto> AssignedReservationQueues { get; set; } = new();
    public List<GetReservationQueueDto> PendingReservationQueues { get; set; } = new();
    public UserPendingActivitySummaryDto SummaryActivity { get; set; } = new();
}

public class UserPendingActivitySummaryDto
{
    public int TotalRequesting { get; set; } = 0;
    public int TotalBorrowing { get; set; } = 0;
    public int TotalAssignedReserving { get; set; } = 0;
    public int TotalPendingReserving { get; set; } = 0;
    public int TotalBorrowOnce { get; set; } = 0;
    public int RemainTotal { get; set; } = 0;
    public bool IsAtLimit { get; set; } = false;
}