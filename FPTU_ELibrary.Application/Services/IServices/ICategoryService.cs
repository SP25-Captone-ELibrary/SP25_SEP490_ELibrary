using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Services.IServices;

public interface ICategoryService<TDto> : IGenericService<Category, TDto, int>
    where TDto : class
{
    Task<IServiceResult> DeleteRangeAsync(int[] bookCategoryIds);
    Task<IServiceResult> ImportCategoryAsync(IFormFile bookCategories, DuplicateHandle duplicateHandle);
}