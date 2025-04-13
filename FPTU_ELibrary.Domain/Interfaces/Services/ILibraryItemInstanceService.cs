using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ILibraryItemInstanceService<TDto> : IGenericService<LibraryItemInstance, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GenerateBarcodeRangeAsync(int categoryId, int totalItem, int? skipItem);
    Task<IServiceResult> GetByBarcodeAsync(string barcode);
    Task<IServiceResult> GetByBarcodeToConfirmUpdateShelfAsync(string barcode);
    Task<IServiceResult> CheckExistBarcodeAsync(string barcode);
    Task<IServiceResult> AddRangeToLibraryItemAsync(int libraryItemId, List<TDto> libraryItemInstances);
    Task<IServiceResult> AddRangeBarcodeWithoutSaveChangesAsync(string isbn, int conditionId, string barcodeRangeFrom, string barcodeRangeTo);
    Task<IServiceResult> UpdateRangeAsync(int libraryItemId, List<TDto> itemInstanceDtos);
    Task<IServiceResult> SoftDeleteAsync(int libraryItemInstanceId);
    Task<IServiceResult> SoftDeleteRangeAsync(int libraryItemId, List<int> libraryItemInstanceIds);
    Task<IServiceResult> UndoDeleteAsync(int libraryItemInstanceId);
    Task<IServiceResult> UndoDeleteRangeAsync(int libraryItemId, List<int> libraryItemInstanceIds);
    Task<IServiceResult> DeleteRangeAsync(int libraryItemId, List<int> libraryItemInstanceIds);
    Task<IServiceResult> CountTotalItemInstanceAsync(int libraryItemId);
    Task<IServiceResult> CountTotalItemInstanceAsync(List<int> libraryItemIds);
    Task<IServiceResult> UpdateInShelfAsync(string barcode);
    Task<IServiceResult> UpdateOutOfShelfAsync(string barcode);
    Task<IServiceResult> UpdateRangeInShelfAsync(List<string> barcodes);
    Task<IServiceResult> UpdateRangeOutOfShelfAsync(List<string> barcodes);
    // Task<IServiceResult> UpdateLostStatusWithoutSaveChangesAsync(int libraryItemInstanceId);
    Task<IServiceResult> UpdateRangeStatusAndInventoryWithoutSaveChangesAsync(List<int> libraryItemInstanceIds,
        LibraryItemInstanceStatus status, bool isProcessBorrowRequest);
    Task<IServiceResult> MarkAsLostAsync(int id);
    Task<IServiceResult> MarkLostAsFoundAsync(int id);
}