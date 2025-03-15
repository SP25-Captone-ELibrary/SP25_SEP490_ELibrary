namespace FPTU_ELibrary.Application.Dtos.Borrows;

public class ConfirmMissingReturnItemDto
{
    public List<BorrowRecordDetailDto> BorrowRecordDetails { get; set; } = new();
}