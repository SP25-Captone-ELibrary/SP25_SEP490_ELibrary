using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Locations;

namespace FPTU_ELibrary.Application.Dtos.Borrows;

public class GetAssignableItemInstanceDto
{
    public LibraryItemInstanceDto LibraryItemInstance { get; set; } = null!;
    public GetLibraryShelfDetailDto? ShelfDetail { get; set; }
}