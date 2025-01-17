using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.EntityFrameworkCore;

namespace FPTU_ELibrary.Domain.Specifications;

public class LibraryResourceSpecification : BaseSpecification<LibraryResource>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }

    public LibraryResourceSpecification(LibraryResourceSpecParams specParams, int pageIndex, int pageSize)
        : base(br =>
            // Search with terms
            string.IsNullOrEmpty(specParams.Search) ||
            (
                // Resource type
                (!string.IsNullOrEmpty(br.ResourceType) && br.ResourceType.Contains(specParams.Search)) ||
                // Resource URL
                (!string.IsNullOrEmpty(br.ResourceUrl) && br.ResourceUrl.Contains(specParams.Search)) ||
                // File format
                (!string.IsNullOrEmpty(br.FileFormat) && br.FileFormat.Contains(specParams.Search)) ||
                // Provider
                (!string.IsNullOrEmpty(br.Provider) && br.Provider.Contains(specParams.Search)) ||
                // ProviderPublicId
                (!string.IsNullOrEmpty(br.ProviderPublicId) && br.ProviderPublicId.Contains(specParams.Search)) ||
                // Created By
                (!string.IsNullOrEmpty(br.CreatedBy) && br.CreatedBy.Contains(specParams.Search)) ||
                // Updated By
                (!string.IsNullOrEmpty(br.UpdatedBy) && br.UpdatedBy.Contains(specParams.Search))
            ))
    {
        // Assign page size and page index
        PageIndex = pageIndex;
        PageSize = pageSize;
        
        // Enable split query
        EnableSplitQuery();
        
        // Apply including book
        // TODO: Change logic to LibraryItem
        // ApplyInclude(q => 
        //     q.Include(br => br.Book));
        
        // Default order by first name
        AddOrderBy(e => e.ResourceType);
        
        // Process filter
        if (!string.IsNullOrEmpty(specParams.ResourceType)) // Resource type
        {
            AddFilter(x => x.ResourceType == specParams.ResourceType);
        }
        if (!string.IsNullOrEmpty(specParams.FileFormat)) // File format
        { 
            AddFilter(x => x.FileFormat == specParams.FileFormat);
        }
        if (!string.IsNullOrEmpty(specParams.Provider))  // Provider
        {
            AddFilter(x => x.Provider == specParams.Provider);
        }
        if (specParams.IsDeleted != null) // Is deleted
        {
            AddFilter(x => x.IsDeleted == specParams.IsDeleted);   
        }
        if (specParams.LastCreatedAtRange != null 
            && specParams.LastCreatedAtRange.Length > 1) // With range of latest create date 
        {
            AddFilter(x => 
                x.CreatedAt >= specParams.LastCreatedAtRange[0].Date 
                && x.CreatedAt <= specParams.LastCreatedAtRange[1].Date);       
        }
        if (specParams.LastUpdatedAtRange != null 
            && specParams.LastUpdatedAtRange.Length > 1) // With range of latest update date 
        {
            AddFilter(x => x.UpdatedAt.HasValue &&
                           x.UpdatedAt.Value.Date >= specParams.LastUpdatedAtRange[0].Date 
                           && x.UpdatedAt.Value.Date <= specParams.LastUpdatedAtRange[1].Date);       
        }
        
        // Progress sorting
        if (!string.IsNullOrEmpty(specParams.Sort))
        {
            // Check is descending sorting 
            var isDescending = specParams.Sort.StartsWith("-");
            if (isDescending)
            {
                specParams.Sort = specParams.Sort.Trim('-');
            }

            specParams.Sort = specParams.Sort.ToUpper();
            
            // Define sorting pattern
            var sortMappings = new Dictionary<string, Expression<Func<LibraryResource, object>>>()
            {
                { "RESOURCETYPE", x => x.ResourceType },
                { "RESOURCEURL", x => x.ResourceUrl },
                { "RESOURCESIZE", x => x.ResourceSize ?? null! },
                { "FILEFORMAT", x => x.FileFormat },
                { "PROVIDER", x => x.Provider },
                { "CREATEDAT", x => x.CreatedAt },
                { "UPDATEDAT", x => x.UpdatedAt ?? null! },
            };
        
            // Get sorting pattern
            if (sortMappings.TryGetValue(specParams.Sort.ToUpper(), 
                    out var sortExpression))
            {
                if(isDescending) AddOrderByDescending(sortExpression);
                else AddOrderBy(sortExpression);    
            }
        }
    }
}