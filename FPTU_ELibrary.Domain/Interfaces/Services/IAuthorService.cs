using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IAuthorService<TDto> : IGenericService<Author, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetAuthorDetailByIdAsync(int id);
    Task<IServiceResult> GetAllByCodesAsync(string[] authorCodes);
    Task<IServiceResult> GetRelatedAuthorItemsAsync(int authorId, int pageIndex, int pageSize);
    Task<IServiceResult> SoftDeleteAsync(int id);
    Task<IServiceResult> SoftDeleteRangeAsync(int[] ids);
    Task<IServiceResult> UndoDeleteAsync(int id);
    Task<IServiceResult> UndoDeleteRangeAsync(int[] ids);
    Task<IServiceResult> DeleteRangeAsync(int[] ids);
    Task<IServiceResult> ImportAsync(IFormFile? file, DuplicateHandle duplicateHandle, string[]? scanningFields);
    Task<IServiceResult> ExportAsync(ISpecification<Author> spec);
}