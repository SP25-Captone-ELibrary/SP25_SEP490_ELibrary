using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ISupplementRequestDetailService<TDto> : IGenericService<SupplementRequestDetail, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetAllByTrackingIdAsync(int trackingId, ISpecification<SupplementRequestDetail> spec);
    Task<IServiceResult> AddFinalizedSupplementRequestFileAsync(int trackingId, string url);
}