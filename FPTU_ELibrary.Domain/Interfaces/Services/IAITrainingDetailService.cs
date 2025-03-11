using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IAITrainingDetailService<TDto> : IGenericService<AITrainingDetail, TDto, int>
    where TDto : class
{
}