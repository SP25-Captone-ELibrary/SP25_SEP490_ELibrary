using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IAuthorService<TDto> : IGenericService<Author, TDto, int>
    where TDto : class
{
}