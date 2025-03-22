using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Application.Services.IServices;

public interface ILibraryResourceUrlService<TDto> : IGenericService<LibraryResourceUrl, TDto, int> 
    where TDto : class
{
    
}