using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IAuditTrailService<TDto> : IReadOnlyService<AuditTrail, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetAuditDetailByDateUtcAndEntityNameAsync(string dateUtc, string entityName, TrailType trailType);
}