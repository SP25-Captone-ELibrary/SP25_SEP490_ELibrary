using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

using SystemFeatureEnum = FPTU_ELibrary.Domain.Common.Enums.SystemFeature;
namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ISystemFeatureService<TDto> : IGenericService<SystemFeature, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetByNameAsync(SystemFeatureEnum featureName);
}