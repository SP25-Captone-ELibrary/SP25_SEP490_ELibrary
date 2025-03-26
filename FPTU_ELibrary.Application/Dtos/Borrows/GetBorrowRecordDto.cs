using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.Borrows;

public class GetBorrowRecordDto
{
    // Key
    public int BorrowRecordId { get; set; }

    // Optional link to BorrowRequest (only for remote borrowing)
    public int? BorrowRequestId { get; set; }  
    
    // Foreign keys
    public Guid LibraryCardId { get; set; }
    
    // Borrow record tracking
    public DateTime BorrowDate { get; set; }
    
    // Borrow type
    public BorrowType BorrowType { get; set; }
    
    // True if borrowed via kiosk
    public bool SelfServiceBorrow { get; set; } 

    // True if return via kiosk 
    public bool? SelfServiceReturn { get; set; }
    
    // Total record item
    public int TotalRecordItem { get; set; }

    // Borrow record processed by which employee
    public Guid? ProcessedBy { get; set; }
    
    // Mark as has fine to payment
    public bool HasFineToPayment { get; set; }
    
    public GetBorrowRequestDto? BorrowRequest { get; set; }
    public EmployeeDto? ProcessedByNavigation { get; set; }
    public List<GetBorrowRecordDetailDto> BorrowRecordDetails { get; set; } = new();
}

public static class GetBorrowRecordDtoExtensions
{
    public static GetBorrowRecordDto ToGetBorrowRecordDto(
        this BorrowRecordDto dto,
        List<LibraryItemConditionDto>? conditions)
    {
        return new()
        {
            BorrowRecordId = dto.BorrowRecordId,
            BorrowRequestId = dto.BorrowRequestId,
            LibraryCardId = dto.LibraryCardId,
            BorrowDate = dto.BorrowDate,
            SelfServiceBorrow = dto.SelfServiceBorrow,
            SelfServiceReturn = dto.SelfServiceReturn,
            BorrowType = dto.BorrowType,
            TotalRecordItem = dto.TotalRecordItem,
            ProcessedBy = dto.ProcessedBy,
            // References, Navigations
            ProcessedByNavigation = dto.ProcessedByNavigation,
            BorrowRequest = dto.BorrowRequest?.ToGetBorrowRequestDto(),
            BorrowRecordDetails = dto.BorrowRecordDetails.Any() 
                ? dto.BorrowRecordDetails.Select(brd => new GetBorrowRecordDetailDto()
                {
                    BorrowRecordDetailId = brd.BorrowRecordDetailId,
                    BorrowRecordId = brd.BorrowRecordId,
                    ConditionId = brd.ConditionId,
                    ReturnConditionId = brd.ReturnConditionId,
                    LibraryItemInstanceId = brd.LibraryItemInstanceId,
                    ConditionCheckDate = brd.ConditionCheckDate,
                    ConditionImages = brd.ImagePublicIds?.Split(',').ToList() ?? new(),
                    Condition = brd.Condition,
                    DueDate = brd.DueDate,
                    ReturnDate = brd.ReturnDate,
                    Status = brd.Status,
                    TotalExtension = brd.TotalExtension,
                    IsReminderSent = brd.IsReminderSent,
                    BorrowDetailExtensionHistories = brd.BorrowDetailExtensionHistories.ToList(),
                    ReturnCondition = conditions != null && conditions.Any() 
                        ? conditions.FirstOrDefault(c => c.ConditionId == brd.ReturnConditionId) 
                        : null,
                    LibraryItem = brd.LibraryItemInstance.LibraryItem != null!
                        ? brd.LibraryItemInstance.LibraryItem.ToLibraryItemDetailDto()
                        : null!,
                    Fines = brd.Fines.Any() ? brd.Fines.ToList() : new()
                }).ToList()
                : new(),
            HasFineToPayment = dto.BorrowRecordDetails.Any() && 
                               dto.BorrowRecordDetails.Any(r => r.Fines.Any(f => f.Status != FineStatus.Paid))
        };
    }
}