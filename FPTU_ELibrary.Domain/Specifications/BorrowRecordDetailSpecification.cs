using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;

namespace FPTU_ELibrary.Domain.Specifications;

public class BorrowRecordDetailSpecification : BaseSpecification<BorrowRecordDetail>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    
    public BorrowRecordDetailSpecification(BorrowRecordDetailSpecParams specParams, int pageIndex, int pageSize)
    {
        // Pagination
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}