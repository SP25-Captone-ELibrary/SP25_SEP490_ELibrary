using FPTU_ELibrary.Domain.Entities;

namespace FPTU_ELibrary.Domain.Interfaces.Services.Base;

public interface IAITraningImageService<TDto> : IGenericService<AITrainingImage, TDto, int>
    where TDto : class
{
    
}