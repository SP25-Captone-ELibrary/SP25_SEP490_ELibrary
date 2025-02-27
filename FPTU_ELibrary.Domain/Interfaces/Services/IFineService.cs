using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Application.Services.IServices;

public interface IFineService<TDto> : IGenericService<Fine, TDto, int>
    where TDto : class
{
}