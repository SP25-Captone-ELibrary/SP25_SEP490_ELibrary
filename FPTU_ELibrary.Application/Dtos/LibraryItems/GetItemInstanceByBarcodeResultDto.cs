namespace FPTU_ELibrary.Application.Dtos.LibraryItems;

public class GetItemInstanceByBarcodeResultDto
{
    // Key
    public int LibraryItemInstanceId { get; set; }

    // Copy of which item
    public int LibraryItemId { get; set; }
    
    // Copy code and its status
    public string Barcode { get; set; } = null!;
    public string Status { get; set; } = null!;
    
    // Creation and update datetime
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string? UpdatedBy { get; set; }

    // Mark as delete
    public bool IsDeleted { get; set; }
    
    // Mark as the instance has been circulated
    public bool IsCirculated { get; set; }

    // Mapping entities
    public LibraryItemDto LibraryItem { get; set; } = null!;

    public ICollection<LibraryItemConditionHistoryDto> LibraryItemConditionHistories { get; set; } = new List<LibraryItemConditionHistoryDto>();
}

public static class GetItemInstanceByBarcodeResultDtoExtensions
{
    public static GetItemInstanceByBarcodeResultDto ToGetByBarcodeResultDto(this LibraryItemInstanceDto dto)
    {
        return new()
        {
            LibraryItemInstanceId = dto.LibraryItemInstanceId,
            LibraryItemId = dto.LibraryItemId,
            Barcode = dto.Barcode,
            Status = dto.Status,
            CreatedAt = dto.CreatedAt,
            CreatedBy = dto.CreatedBy,
            UpdatedAt = dto.UpdatedAt,
            UpdatedBy = dto.UpdatedBy,
            IsDeleted = dto.IsDeleted,
            IsCirculated = dto.IsCirculated,
            LibraryItem = dto.LibraryItem,
            LibraryItemConditionHistories = dto.LibraryItemConditionHistories
        };
    }
}