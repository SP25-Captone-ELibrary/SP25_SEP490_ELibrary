using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;

namespace FPTU_ELibrary.Domain.Specifications;

public class AuditTrailSpecification : BaseSpecification<AuditTrail>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    
    public AuditTrailSpecification(AuditTrailSpecParams specParams, int pageIndex, int pageSize)
        : base(x => x.EntityId == specParams.EntityId // with specific entity ID
                    && x.EntityName == specParams.EntityName) // with specific entity name
    {
        // Assign page size and page index
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}