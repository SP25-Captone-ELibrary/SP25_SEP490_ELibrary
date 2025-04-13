using FPTU_ELibrary.Application.Dtos.AIServices.Classification;

namespace FPTU_ELibrary.Application.Dtos.LibraryItems;

public class GetItemPotentialGroup
{
    public LibraryItemGroupDto GroupDetail { get; set; } = null!;
    public CheckedGroupResponseDto<string> CheckResponse { get; set; } = new();
}