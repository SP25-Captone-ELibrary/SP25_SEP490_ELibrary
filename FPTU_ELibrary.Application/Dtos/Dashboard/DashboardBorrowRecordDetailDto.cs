using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Entities;

namespace FPTU_ELibrary.Application.Dtos.Dashboard;

public class DashboardBorrowRecordDetailDto
{
    public BorrowRecordDetailDto BorrowRecordDetail { get; set; } = null!;
    public LibraryCardDto LibraryCard { get; set; } = null!;
    public LibraryItemDto LibraryItem { get; set; } = null!;
}

public static class DashboardBorrowRecordDetailDtoExtensions
{
    public static DashboardBorrowRecordDetailDto ToDashboardBorrowRecordDetailDto(
        this BorrowRecordDetailDto dto, LibraryCardDto card, LibraryItemDto item)
    {
        return new()
        {
            BorrowRecordDetail = dto,
            LibraryCard = card,
            LibraryItem = item
        };
    }
}