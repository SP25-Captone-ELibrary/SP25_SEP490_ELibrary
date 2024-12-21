using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IFinePolicyService<TDto> :IGenericService<FinePolicy, TDto, int>
    where TDto : class
{
}