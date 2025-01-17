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
        
        // Progress filter
        if (specParams.AuthorCode != null) // With author code
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
        if (specParams.ModifiedDateRange != null
            && specParams.ModifiedDateRange.Length > 1) // With range of dob
        {
            if (specParams.ModifiedDateRange[0].HasValue && specParams.ModifiedDateRange[1].HasValue)
            {
                AddFilter(x => x.UpdateDate.HasValue &&
                               x.UpdateDate.Value.Date >= specParams.ModifiedDateRange[0]!.Value.Date
                               && x.UpdateDate.Value.Date <= specParams.ModifiedDateRange[1]!.Value.Date);
            }
            else if ((specParams.ModifiedDateRange[0] is null && specParams.ModifiedDateRange[1].HasValue))
            {
                AddFilter(x => x.UpdateDate <= specParams.ModifiedDateRange[1]);
            }
            else if (specParams.ModifiedDateRange[0].HasValue && specParams.ModifiedDateRange[1] is null)
            {
                AddFilter(x => x.UpdateDate >= specParams.ModifiedDateRange[0]);
            }
        }
        
        if (specParams.CreateDateRange != null
            && specParams.CreateDateRange.Length > 1) // With range of dob
        {
            if (specParams.CreateDateRange[0].HasValue && specParams.CreateDateRange[1].HasValue)
            {
                AddFilter(x =>
                               x.CreateDate.Date >= specParams.CreateDateRange[0]!.Value.Date
                               && x.CreateDate.Date <= specParams.CreateDateRange[1]!.Value.Date);
            }
            else if ((specParams.CreateDateRange[0] is null && specParams.CreateDateRange[1].HasValue))
            {
                AddFilter(x => x.CreateDate <= specParams.CreateDateRange[1]);
            }
            else if (specParams.CreateDateRange[0].HasValue && specParams.CreateDateRange[1] is null)
            {
                AddFilter(x => x.CreateDate >= specParams.CreateDateRange[0]);
            }
        }
        
        if (specParams.DobRange != null
            && specParams.DobRange.Length > 1) // With range of dob
        {
            if (specParams.DobRange[0].HasValue && specParams.DobRange[1].HasValue)
            {
                AddFilter(x => x.Dob.HasValue &&
                               x.Dob.Value.Date >= specParams.DobRange[0]!.Value.Date
                               && x.Dob.Value.Date <= specParams.DobRange[1]!.Value.Date);
            }
            else if ((specParams.DobRange[0] is null && specParams.DobRange[1].HasValue))
            {
                AddFilter(x => x.Dob != null && 
                               x.Dob.Value.Date <= specParams.DobRange[1]!.Value.Date);
            }
            else if (specParams.DobRange[0].HasValue && specParams.DobRange[1] is null)
            {
                AddFilter(x => x.Dob != null && 
                               x.Dob.Value.Date >= specParams.DobRange[0]!.Value.Date);
            }
        }
        
        if (specParams.DateOfDeathRange != null
            && specParams.DateOfDeathRange.Length > 1) // With range of dob
        {
            if (specParams.DateOfDeathRange[0].HasValue && specParams.DateOfDeathRange[1].HasValue)
            {
                AddFilter(x => x.DateOfDeath.HasValue &&
                               x.DateOfDeath.Value.Date >= specParams.DateOfDeathRange[0]!.Value.Date
                               && x.DateOfDeath.Value.Date <= specParams.DateOfDeathRange[1]!.Value.Date);
            }
            else if ((specParams.DateOfDeathRange[0] is null && specParams.DateOfDeathRange[1].HasValue))
            {
                AddFilter(x => x.DateOfDeath <= specParams.DateOfDeathRange[1]);
            }
            else if (specParams.DateOfDeathRange[0].HasValue && specParams.DateOfDeathRange[1] is null)
            {
                AddFilter(x => x.DateOfDeath >= specParams.DateOfDeathRange[0]);
            }
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
                { "AUTHORCODE", x => x.AuthorCode },
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
        else
        {
            // Default sort by ID 
            AddOrderByDescending(a => a.AuthorId);
        }
    }
}