using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Application.Services.IServices;

public interface ICategoryService<TDto> : IGenericService<Category, TDto,int>
    where TDto : class
{ 
    Task<IServiceResult> HardDeleteRangeAsync(int[] bookCategoryIds);
}