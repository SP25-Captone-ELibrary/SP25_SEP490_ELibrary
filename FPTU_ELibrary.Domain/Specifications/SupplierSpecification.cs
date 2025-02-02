using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;

namespace FPTU_ELibrary.Domain.Specifications;

public class SupplierSpecification : BaseSpecification<Supplier>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }

    public SupplierSpecification(SupplierSpecParams specParams, int pageIndex, int pageSize)
        : base(s => 
            // Search with terms
            string.IsNullOrEmpty(specParams.Search) || 
            (
                // Supplier name
                (!string.IsNullOrEmpty(s.SupplierName) && s.SupplierName.Contains(specParams.Search)) || 
                // Contact person
                (!string.IsNullOrEmpty(s.ContactPerson) && s.ContactPerson.Contains(specParams.Search)) || 
                // Contact email
                (!string.IsNullOrEmpty(s.ContactEmail) && s.ContactEmail.Contains(specParams.Search)) || 
                // Contact phone
                (!string.IsNullOrEmpty(s.ContactPhone) && s.ContactPhone.Contains(specParams.Search)) || 
                // Address
                (!string.IsNullOrEmpty(s.Address) && s.Address.Contains(specParams.Search)) || 
                // Country
                (!string.IsNullOrEmpty(s.Country) && s.Country.Contains(specParams.Search)) || 
                // City
                (!string.IsNullOrEmpty(s.City) && s.City.Contains(specParams.Search))
            )
        )
    {
        // Assign page size and page index
        PageIndex = pageIndex;
        PageSize = pageSize;

        // Enable split query
        EnableSplitQuery();
        
        // Progress filter
        if (specParams.SupplierType != null)
        {
            AddFilter(s => s.SupplierType == specParams.SupplierType);
        }
        if (specParams.IsActive != null) // With status
        {
            AddFilter(x => x.IsActive == specParams.IsActive);
        }
        if (specParams.IsDeleted != null) // Is deleted
        {
            AddFilter(x => x.IsDeleted == specParams.IsDeleted);
        }
        if (specParams.CreateDateRange != null
            && specParams.CreateDateRange.Length > 1) // With range of create date 
        {
            if (specParams.CreateDateRange[0].HasValue && specParams.CreateDateRange[1].HasValue)
            {
                AddFilter(x =>
                    x.CreatedAt >= specParams.CreateDateRange[0]!.Value.Date
                    && x.CreatedAt <= specParams.CreateDateRange[1]!.Value.Date);
            }
            else if (specParams.CreateDateRange[0] is null && specParams.CreateDateRange[1].HasValue)
            {
                AddFilter(x => x.CreatedAt <= specParams.CreateDateRange[1]);
            }
            else if (specParams.CreateDateRange[0].HasValue && specParams.CreateDateRange[1] is null)
            {
                AddFilter(x => x.CreatedAt >= specParams.CreateDateRange[0]);
            }
        }
        if (specParams.ModifiedDateRange != null
            && specParams.ModifiedDateRange.Length > 1) // With range of modified date
        {
            if (specParams.ModifiedDateRange[0].HasValue && specParams.ModifiedDateRange[1].HasValue)
            {
                AddFilter(x => x.UpdatedAt.HasValue &&
                               x.UpdatedAt.Value.Date >= specParams.ModifiedDateRange[0]!.Value.Date
                               && x.UpdatedAt.Value.Date <= specParams.ModifiedDateRange[1]!.Value.Date);
            }
            else if ((specParams.ModifiedDateRange[0] is null && specParams.ModifiedDateRange[1].HasValue))
            {
                AddFilter(x => x.UpdatedAt <= specParams.ModifiedDateRange[1]);
            }
            else if (specParams.ModifiedDateRange[0].HasValue && specParams.ModifiedDateRange[1] is null)
            {
                AddFilter(x => x.UpdatedAt >= specParams.ModifiedDateRange[0]);
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
            
            // Uppercase sort value
            specParams.Sort = specParams.Sort.ToUpper();

            // Apply sorting
            ApplySorting(specParams.Sort, isDescending);
        }
        else
        {
            // Default order by id
            AddOrderByDescending(u => u.SupplierId);
        }
    }
    
    private void ApplySorting(string propertyName, bool isDescending)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // Initialize expression parameter with type of Supplier (x)
        var parameter = Expression.Parameter(typeof(Supplier), "x");
        // Assign property base on property name (x.PropertyName)
        var property = Expression.Property(parameter, propertyName);
        // Building a complete sort lambda expression (x => x.PropertyName)
        var sortExpression =
            Expression.Lambda<Func<Supplier, object>>(Expression.Convert(property, typeof(object)), parameter);

        if (isDescending)
        {
            AddOrderByDescending(sortExpression);
        }
        else
        {
            AddOrderBy(sortExpression);
        }
    }
}