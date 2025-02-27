using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Notifications;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Dtos.Roles;

namespace FPTU_ELibrary.Application.Dtos.LibraryCard;

public class LibraryCardHolderDto
{
    // Key
    public Guid UserId { get; set; }
    
    // Role in the system
    public int RoleId { get; set; }
    
    // Library card information
    public Guid? LibraryCardId { get; set; }
    
    // Basic user information
    public string Email { get; set; } = null!;
    public string? FirstName { get; set; } 
    public string? LastName { get; set; } 
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public string? Address { get; set; }
    public string? Gender { get; set; }
    public DateTime? Dob { get; set; }
    
    // Mark as active or not
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    
    // Check whether user is created from employee or not
    public bool IsEmployeeCreated { get; set; }
    
    // Creation and modify date
    public DateTime CreateDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
    
    // Mapping entities
    public SystemRoleDto Role { get; set; } = null!;
    public LibraryCardDto? LibraryCard { get; set; } 
    
    // Borrow and returning entities
    public ICollection<BorrowRequestDto> BorrowRequests { get; set; } = new List<BorrowRequestDto>();
    
    public ICollection<DigitalBorrowDto> DigitalBorrows { get; set; } = new List<DigitalBorrowDto>();
    
    public ICollection<BorrowRecordDto> BorrowRecords { get; set; } = new List<BorrowRecordDto>();
    
    public ICollection<ReservationQueueDto> ReservationQueues { get; set; } = new List<ReservationQueueDto>();
    
    public ICollection<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
    
    // Notifications
    public ICollection<NotificationRecipientDto> NotificationRecipients { get; set; } = new List<NotificationRecipientDto>();
}

public static class LibraryCardholderDtoExtensions
{
    public static LibraryCardHolderDto ToLibraryCardHolderDto(this UserDto userDto)
    {
        return new()
        {
            UserId = userDto.UserId,
            RoleId = userDto.RoleId,
            LibraryCardId = userDto.LibraryCardId,
            Email = userDto.Email,
            FirstName = userDto.FirstName,
            LastName = userDto.LastName,
            Avatar = !string.IsNullOrEmpty(userDto.Avatar) ? userDto.Avatar : null,
            Phone = userDto.Phone,
            Address = userDto.Address,
            Gender = userDto.Gender,
            Dob = userDto.Dob,
            IsActive = userDto.IsActive,
            IsDeleted = userDto.IsDeleted,
            IsEmployeeCreated = userDto.IsEmployeeCreated,
            CreateDate = userDto.CreateDate,
            ModifiedDate = userDto.ModifiedDate,
            ModifiedBy = userDto.ModifiedBy,
            LibraryCard = userDto.LibraryCard,
            NotificationRecipients = userDto.NotificationRecipients,
            Transactions = new List<TransactionDto>(),
            DigitalBorrows = new List<DigitalBorrowDto>(),
            BorrowRequests = new List<BorrowRequestDto>(),
            BorrowRecords = new List<BorrowRecordDto>(),
            ReservationQueues = new List<ReservationQueueDto>()
        };
    }
}