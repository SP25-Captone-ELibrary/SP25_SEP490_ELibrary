using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IAuditTrailService<TDto> : IReadOnlyService<AuditTrail, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetAllRoleAuditTrailAsync(ISpecification<AuditTrail> spec, bool tracked);
    Task<IServiceResult> GetAuditDetailByDateUtcAndEntityNameAsync(string dateUtc, string entityName, TrailType trailType);
}