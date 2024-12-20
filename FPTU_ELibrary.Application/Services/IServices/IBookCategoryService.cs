using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Application.Services.IServices;

public interface IBookCategoryService<TDto> : IGenericService<BookCategory, TDto,int>
    where TDto : class
{
    Task<IServiceResult> SoftDeleteAsync(int bookCategoryId);
    Task<IServiceResult> SoftDeleteRangeAsync(int[] bookCategoryIds);
    Task<IServiceResult> UndoDeleteAsync(int bookCategoryId);
    Task<IServiceResult> UndoDeleteRangeAsync(int[] bookCategoryIds);
    Task<IServiceResult> HardDeleteRangeAsync(int[] bookCategoryIds);
}