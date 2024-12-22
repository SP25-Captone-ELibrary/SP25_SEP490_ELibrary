using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.AspNetCore.Http;
using BookCategory = FPTU_ELibrary.Domain.Entities.BookCategory;

namespace FPTU_ELibrary.Application.Services.IServices;

public interface IBookCategoryService<TDto> : IGenericService<BookCategory, TDto,int>
    where TDto : class
{ 
    Task<IServiceResult> HardDeleteRangeAsync(int[] bookCategoryIds);
    Task<IServiceResult> ImportBookCategoryAsync(IFormFile bookCategories, DuplicateHandle duplicateHandle);
}