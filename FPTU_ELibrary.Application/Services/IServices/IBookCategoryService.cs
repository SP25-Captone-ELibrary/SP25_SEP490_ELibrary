using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Application.Services.IServices;

public interface IBookCategoryService<TDto> : IGenericService<BookCategory, TDto,int>
    where TDto : class
{
    Task<IServiceResult> GetAll();
    Task<IServiceResult> Update(int id, TDto dto,string roleName);
    Task<IServiceResult> Delete(int id,string roleName);
    Task<IServiceResult> SoftDelete(int id,string roleName);
    Task<IServiceResult> Create(TDto dto, string roleName);
}