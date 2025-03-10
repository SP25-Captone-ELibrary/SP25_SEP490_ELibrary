using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.LibraryCard;

public class LibraryCardHolderBorrowRecordDto
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
    
    // Count total request item
    public int TotalRecordItem { get; set; }

    // Borrow record processed by which employee
    public Guid? ProcessedBy { get; set; }

    public BorrowRequestDto? BorrowRequest { get; set; }
    public EmployeeDto? ProcessedByNavigation { get; set; } = null!;
    public List<LibraryCardHolderBorrowRecordDetailDto> BorrowRecordDetails { get; set; } = new();
    public List<FineDto> Fines { get; set; } = new();
}

public static class LibraryCardHolderBorrowRecordExtensions
{
    public static LibraryCardHolderBorrowRecordDto ToCardHolderBorrowRecordDto(
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
            BorrowRequest = dto.BorrowRequest,
            Fines = dto.Fines.ToList(),
            BorrowRecordDetails = dto.BorrowRecordDetails.Any() 
                ? dto.BorrowRecordDetails.Select(brd => new LibraryCardHolderBorrowRecordDetailDto()
                {
                    BorrowRecordDetailId = brd.BorrowRecordDetailId,
                    BorrowRecordId = brd.BorrowRecordId,
                    ConditionId = brd.ConditionId,
                    ReturnConditionId = brd.ReturnConditionId,
                    ConditionCheckDate = brd.ConditionCheckDate,
                    ConditionImages = brd.ImagePublicIds?.Split(',').ToList() ?? new(),
                    Condition = brd.Condition,
                    ReturnDate = brd.ReturnDate,
                    DueDate = brd.DueDate,
                    Status = brd.Status,
                    TotalExtension = brd.TotalExtension,
                    IsReminderSent = brd.IsReminderSent,
                    ReturnCondition = conditions != null && conditions.Any() 
                        ? conditions.FirstOrDefault(c => c.ConditionId == brd.ReturnConditionId) 
                        : null,
                    LibraryItem = brd.LibraryItemInstance.LibraryItem != null!
                        ? new LibraryItemDto()
                        {
                            LibraryItemId = brd.LibraryItemInstance.LibraryItem.LibraryItemId,
                            Title = brd.LibraryItemInstance.LibraryItem.Title,
                            SubTitle = brd.LibraryItemInstance.LibraryItem.SubTitle,
                            Responsibility = brd.LibraryItemInstance.LibraryItem.Responsibility,
                            Edition = brd.LibraryItemInstance.LibraryItem.Edition,
                            EditionNumber = brd.LibraryItemInstance.LibraryItem.EditionNumber,
                            Language = brd.LibraryItemInstance.LibraryItem.Language,
                            OriginLanguage = brd.LibraryItemInstance.LibraryItem.OriginLanguage,
                            Summary = brd.LibraryItemInstance.LibraryItem.Summary,
                            CoverImage = brd.LibraryItemInstance.LibraryItem.CoverImage,
                            PublicationYear = brd.LibraryItemInstance.LibraryItem.PublicationYear,
                            Publisher = brd.LibraryItemInstance.LibraryItem.Publisher,
                            PublicationPlace = brd.LibraryItemInstance.LibraryItem.PublicationPlace,
                            ClassificationNumber = brd.LibraryItemInstance.LibraryItem.ClassificationNumber,
                            CutterNumber = brd.LibraryItemInstance.LibraryItem.CutterNumber,
                            Isbn = brd.LibraryItemInstance.LibraryItem.Isbn,
                            Ean = brd.LibraryItemInstance.LibraryItem.Ean,
                            EstimatedPrice = brd.LibraryItemInstance.LibraryItem.EstimatedPrice,
                            PageCount = brd.LibraryItemInstance.LibraryItem.PageCount,
                            PhysicalDetails = brd.LibraryItemInstance.LibraryItem.PhysicalDetails,
                            Dimensions = brd.LibraryItemInstance.LibraryItem.Dimensions,
                            AccompanyingMaterial = brd.LibraryItemInstance.LibraryItem.AccompanyingMaterial,
                            Genres = brd.LibraryItemInstance.LibraryItem.Genres,
                            GeneralNote = brd.LibraryItemInstance.LibraryItem.GeneralNote,
                            BibliographicalNote = brd.LibraryItemInstance.LibraryItem.BibliographicalNote,
                            TopicalTerms = brd.LibraryItemInstance.LibraryItem.TopicalTerms,
                            AdditionalAuthors = brd.LibraryItemInstance.LibraryItem.AdditionalAuthors,
                            CategoryId = brd.LibraryItemInstance.LibraryItem.CategoryId,
                            ShelfId = brd.LibraryItemInstance.LibraryItem.ShelfId,
                            GroupId = brd.LibraryItemInstance.LibraryItem.GroupId,
                            Status = brd.LibraryItemInstance.LibraryItem.Status,
                            IsDeleted = brd.LibraryItemInstance.LibraryItem.IsDeleted,
                            IsTrained = brd.LibraryItemInstance.LibraryItem.IsTrained,
                            CanBorrow = brd.LibraryItemInstance.LibraryItem.CanBorrow,
                            TrainedAt = brd.LibraryItemInstance.LibraryItem.TrainedAt,
                            CreatedAt = brd.LibraryItemInstance.LibraryItem.CreatedAt,
                            UpdatedAt = brd.LibraryItemInstance.LibraryItem.UpdatedAt,
                            UpdatedBy = brd.LibraryItemInstance.LibraryItem.UpdatedBy,
                            CreatedBy = brd.LibraryItemInstance.LibraryItem.CreatedBy,
                            LibraryItemInstances = new List<LibraryItemInstanceDto>()
                            {
                                brd.LibraryItemInstance
                            }
                        }
                        : null!,
                    BorrowDetailExtensionHistories = brd.BorrowDetailExtensionHistories.Any()
                        ? brd.BorrowDetailExtensionHistories.ToList() 
                        : new(),
                }).ToList()
                : new()
        };
    }
}