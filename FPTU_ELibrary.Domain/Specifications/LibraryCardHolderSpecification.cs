using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.EntityFrameworkCore;

namespace FPTU_ELibrary.Domain.Specifications;

public class LibraryCardHolderSpecification : BaseSpecification<User>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }

    public LibraryCardHolderSpecification(LibraryCardHolderSpecParams specParams, int pageIndex, int pageSize)
        : base(e =>
            // Search with terms
            string.IsNullOrEmpty(specParams.Search) ||
            (
                // Email
                (!string.IsNullOrEmpty(e.Email) && e.Email.Contains(specParams.Search)) ||
                // Phone
                (!string.IsNullOrEmpty(e.Phone) && e.Phone.Contains(specParams.Search)) ||
                // Address
                (!string.IsNullOrEmpty(e.Address) && e.Address.Contains(specParams.Search)) ||
                // Individual FirstName and LastName search
                (!string.IsNullOrEmpty(e.FirstName) && e.FirstName.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(e.LastName) && e.LastName.Contains(specParams.Search)) ||
                // Full Name search
                (!string.IsNullOrEmpty(e.FirstName) &&
                 !string.IsNullOrEmpty(e.LastName) &&
                 (e.FirstName + " " + e.LastName).Contains(specParams.Search))
            ))
    {
        // Apply pagination
        PageIndex = pageIndex;
        PageSize = pageSize;
        
        // Apply include
        ApplyInclude(q => q
            .Include(u => u.Role)
            .Include(u => u.LibraryCard!)
        );
        
        // Enable split query
        EnableSplitQuery();
        
        // Exclude all user with role of (Admin)
        AddFilter(u => u.Role.EnglishName != nameof(Role.Administration));
        
        // Apply Sorting
        if (!string.IsNullOrEmpty(specParams.Sort))
        {
            var sortBy = specParams.Sort.Trim();
            var isDescending = sortBy.StartsWith("-");
            var propertyName = isDescending ? sortBy.Substring(1) : sortBy;

            ApplySorting(propertyName, isDescending);
        }
        else
        {
            // Default order by create date
            AddOrderByDescending(u => u.CreateDate);
        }
    }
    
    private void ApplySorting(string propertyName, bool isDescending)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // Use Reflection to dynamically apply sorting
        var parameter = Expression.Parameter(typeof(User), "x");
        var property = Expression.Property(parameter, propertyName);
        var sortExpression =
            Expression.Lambda<Func<User, object>>(Expression.Convert(property, typeof(object)), parameter);

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