using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.EntityFrameworkCore;

namespace FPTU_ELibrary.Domain.Specifications;

public class LibraryItemGroupSpecification : BaseSpecification<LibraryItemGroup>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public string? Search { get; set; }
    
    public LibraryItemGroupSpecification(LibraryItemGroupSpecParams specParams, int pageIndex, int pageSize)
    {
        // Pagination 
        PageIndex = pageIndex;
        PageSize = pageSize;
        
        // Search
        Search = specParams.Search;
    }
}