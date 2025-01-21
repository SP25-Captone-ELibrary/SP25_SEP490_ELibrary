using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ILibraryItemService<TDto> : IGenericService<LibraryItem, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetEnumValueAsync();
    Task<IServiceResult> GetDetailAsync(int id);
    Task<IServiceResult> UpdateBorrowStatusWithoutSaveChangesAsync(int id, bool canBorrow);
    Task<IServiceResult> SoftDeleteAsync(int id);
    Task<IServiceResult> SoftDeleteRangeAsync(int[] ids);
    Task<IServiceResult> UndoDeleteAsync(int id);
    Task<IServiceResult> UndoDeleteRangeAsync(int[] ids);
    Task<IServiceResult> DeleteRangeAsync(int[] ids);
    // Task<IServiceResult> UpdateTrainingStatusAsync(Guid trainingBookCode);
    // Task<IServiceResult> GetRelatedEditionWithMatchFieldAsync(TDto dto, string fieldName);
    Task<IServiceResult> UpdateStatusAsync(int id);
    Task<IServiceResult> UpdateShelfLocationAsync(int id, int? shelfId);
    // Task<IServiceResult> ImportAsync(
    //     IFormFile? file, List<IFormFile> coverImageFiles, string[]? scanningFields);
    // Task<IServiceResult> ExportAsync(ISpecification<LibraryItem> spec);
}