using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Domain.Interfaces.Services
{
    public interface IBookService<TDto> : IGenericService<Book, TDto, int>
        where TDto : class
    {
        Task<IServiceResult> CreateAsync(TDto dto, string byEmail);
        Task<IServiceResult> UpdateAsync(int id, TDto dto, string byEmail);
        Task<IServiceResult> GetCreateInformationAsync();
    }
}