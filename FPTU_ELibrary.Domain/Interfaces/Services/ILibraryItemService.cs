using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ILibraryItemService<TDto> : IGenericService<LibraryItem, TDto, int>
    where TDto : class
{
    Task<IServiceResult> CreateAsync(TDto dto, int trackingDetailId);
    Task<IServiceResult> AddRangeInstancesWithoutSaveChangesAsync(List<TDto> itemListIncludeInstances);
    Task<IServiceResult> GetEnumValueAsync();
    Task<IServiceResult> GetDetailAsync(int id, string? email = null);
    Task<IServiceResult> GetByBarcodeAsync(string barcode);
    Task<IServiceResult> GetByIsbnAsync(string isbn);
    Task<IServiceResult> GetRecentReadByIdsAsync(int[] ids, int pageIndex, int pageSize);
    Task<IServiceResult> GetNewArrivalsAsync(int pageIndex, int pageSize);
    Task<IServiceResult> GetTrendingAsync(int pageIndex, int pageSize);
    Task<IServiceResult> GetByCategoryAsync(int categoryId, int pageIndex, int pageSize);
    Task<IServiceResult> GetItemsInGroupAsync(int id, int pageIndex, int pageSize);
    Task<IServiceResult> GetReviewsAsync(int id, int pageIndex, int pageSize);
    Task<IServiceResult> GetRelatedItemsAsync(int id, int pageIndex, int pageSize);
    Task<IServiceResult> GetFirstAuthorAsync(int id);
    Task<IServiceResult> GetByInstanceBarcodeAsync(string barcode);
    Task<IServiceResult> GetItemClassificationNumAsync(int[] ids);
    Task<IServiceResult> GetAllForRecommendationAsync();
    Task<IServiceResult> GetAllWithoutAdvancedSpecAsync(ISpecification<LibraryItem> specification, bool tracked = true);
    Task<IServiceResult> CheckUnavailableForBorrowRequestAsync(string email, int[] ids);
    Task<IServiceResult> UpdateBorrowStatusWithoutSaveChangesAsync(int id, bool canBorrow);
    Task<IServiceResult> SoftDeleteAsync(int id);
    Task<IServiceResult> SoftDeleteRangeAsync(int[] ids);
    Task<IServiceResult> UndoDeleteAsync(int id);
    Task<IServiceResult> UndoDeleteRangeAsync(int[] ids);
    Task<IServiceResult> DeleteRangeAsync(int[] ids);
    Task<IServiceResult> UpdateTrainingStatusAsync(List<int> libraryItemIds);
    Task<IServiceResult> UpdateStatusAsync(int id);
    Task<IServiceResult> UpdateShelfLocationAsync(int id, int? shelfId);
    Task<IServiceResult> UpdateGroupIdAsync(List<int> libraryItemIds, int newGroupId);
    Task<IServiceResult> DetectWrongImportDataAsync<TCsvRecord>(int startRowIndex, List<TCsvRecord> records, List<string> coverImageNames);
    Task<IServiceResult> DetectDuplicatesInFileAsync<TCsvRecord>(List<TCsvRecord> records, string[] scanningFields);
    Task<IServiceResult> ExportAsync(ISpecification<LibraryItem> spec);

    #region Archived Function
    // Task<IServiceResult> ImportAsync(
    //     IFormFile? file, List<IFormFile> coverImageFiles, 
    //     string[]? scanningFields, DuplicateHandle? duplicateHandle = null);
    #endregion
}