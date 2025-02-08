using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;

namespace FPTU_ELibrary.Domain.Specifications;

public class LibraryCardPackageSpecification : BaseSpecification<LibraryCardPackage>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    
    public LibraryCardPackageSpecification(LibraryCardPackageSpecParams specParams, int pageIndex, int pageSize)
        : base(lc => 
            string.IsNullOrEmpty(specParams.Search) || 
            (!string.IsNullOrEmpty(lc.PackageName) && lc.PackageName.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(lc.Description) && lc.PackageName.Contains(specParams.Search)) || 
            (specParams.ParsedPrice != null && lc.Price == specParams.ParsedPrice)
        )
    {
        // Pagination
        PageIndex = pageIndex;
        PageSize = pageSize;
            
        // Enable split query
        EnableSplitQuery();
        
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
            // Default order by create date
            AddOrderByDescending(u => u.CreatedAt);
        }
    }
    
    private void ApplySorting(string propertyName, bool isDescending)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // Initialize expression parameter with type of LibraryCardPackage (x)
        var parameter = Expression.Parameter(typeof(Employee), "x");
        // Assign property base on property name (x.PropertyName)
        var property = Expression.Property(parameter, propertyName);
        // Building a complete sort lambda expression (x => x.PropertyName)
        var sortExpression =
            Expression.Lambda<Func<LibraryCardPackage, object>>(Expression.Convert(property, typeof(object)), parameter);

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