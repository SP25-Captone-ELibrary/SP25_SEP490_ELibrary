using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;

namespace FPTU_ELibrary.Domain.Specifications;

public class BookSpecification : BaseSpecification<Book>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    
    public BookSpecification(BookSpecParams specParams, int pageIndex, int pageSize)
        : base()
    {
    }
}