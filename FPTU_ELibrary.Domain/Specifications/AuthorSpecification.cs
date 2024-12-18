using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;

namespace FPTU_ELibrary.Domain.Specifications;

public class AuthorSpecification : BaseSpecification<Author>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }

    public AuthorSpecification(AuthorSpecParams specParams, int pageIndex, int pageSize)
        : base(e => 
            // Search with terms
            string.IsNullOrEmpty(specParams.Search) || 
            (
                // Employee code
                (!string.IsNullOrEmpty(e.AuthorCode) && e.AuthorCode.Contains(specParams.Search)) ||
                // Nationality
                (!string.IsNullOrEmpty(e.Nationality) && e.Nationality.Contains(specParams.Search)) ||
                // Full Name search
                (!string.IsNullOrEmpty(e.FullName) && e.FullName.Contains(specParams.Search))
            ))
    {
        // Assign page size and page index
        PageIndex = pageIndex;
        PageSize = pageSize;
        
        // Enable split query
        EnableSplitQuery();
        
        // Default order by first name
        AddOrderBy(e => e.FullName);
        
        // Progress filter
        if (specParams.AuthorCode != null) // With employee code
        {
            AddFilter(x => x.AuthorCode == specParams.AuthorCode);
        }
        if (!string.IsNullOrEmpty(specParams.Nationality)) // With nationality
        {
            AddFilter(x => x.Nationality == specParams.Nationality);
        }
        if (specParams.IsDeleted != null) // Is deleted
        {
            AddFilter(x => x.IsDeleted == specParams.IsDeleted);       
        }
        if (specParams.DobRange != null 
            && specParams.DobRange.Length > 1) // With range of dob
        {
            AddFilter(x => x.Dob.HasValue &&
                           x.Dob.Value.Date >= specParams.DobRange[0].Date 
                           && x.Dob.Value.Date <= specParams.DobRange[1].Date);       
        }
        if (specParams.CreateDateRange != null 
            && specParams.CreateDateRange.Length > 1) // With range of create date 
        {
            AddFilter(x => 
                x.CreateDate >= specParams.CreateDateRange[0].Date 
                && x.CreateDate <= specParams.CreateDateRange[1].Date);       
        }
        if (specParams.ModifiedDateRange != null 
            && specParams.ModifiedDateRange.Length > 1) // With range of create date 
        {
            AddFilter(x => x.UpdateDate.HasValue &&
                           x.UpdateDate.Value.Date >= specParams.ModifiedDateRange[0].Date 
                           && x.UpdateDate.Value.Date <= specParams.ModifiedDateRange[1].Date);       
        }
        if (specParams.DateOfDeathRange != null 
            && specParams.DateOfDeathRange.Length > 1) // With range of death date 
        {
            AddFilter(x => x.DateOfDeath.HasValue &&
                           x.DateOfDeath.Value.Date >= specParams.DateOfDeathRange[0].Date 
                           && x.DateOfDeath.Value.Date <= specParams.DateOfDeathRange[1].Date);       
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
            var sortMappings = new Dictionary<string, Expression<Func<Author, object>>>()
            {
                { "AUTHORCODE", x => x.AuthorCode ?? string.Empty },
                { "FULLNAME", x => x.FullName },
                { "NATIONALITY", x => x.Nationality ?? string.Empty },
                { "BIOGRAPHY", x => x.Biography ?? string.Empty },
                { "DOB", x => x.Dob ?? null! },
                { "DATEOFDEATH", x => x.DateOfDeath ?? null! },
                { "CREATEDATE", x => x.CreateDate },
                { "MODIFIEDDATE", x => x.UpdateDate ?? null! }
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