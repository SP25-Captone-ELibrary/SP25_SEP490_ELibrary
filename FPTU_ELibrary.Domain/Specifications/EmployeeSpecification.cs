using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.EntityFrameworkCore;

namespace FPTU_ELibrary.Domain.Specifications;

public class EmployeeSpecification : BaseSpecification<Employee>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }

    public EmployeeSpecification(EmployeeSpecParams specParams, int pageIndex, int pageSize)
        : base(e =>
            // Search with terms
            string.IsNullOrEmpty(specParams.Search) ||
            (
                // Employee code
                (!string.IsNullOrEmpty(e.EmployeeCode) && e.EmployeeCode.Contains(specParams.Search)) ||
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
        // Assign page size and page index
        PageIndex = pageIndex;
        PageSize = pageSize;

        // Enable split query
        EnableSplitQuery();

        // Include role 
        ApplyInclude(q => q
            .Include(e => e.Role));

        // Default order by first name
        AddOrderBy(e => e.FirstName);

        // Progress filter
        if (specParams.RoleId > 0) // With role
        {
            AddFilter(x => x.RoleId == specParams.RoleId);
        }

        if (specParams.EmployeeCode != null) // With employee code
        {
            AddFilter(x => x.EmployeeCode == specParams.EmployeeCode);
        }

        if (!string.IsNullOrEmpty(specParams.FirstName)) // With first name
        {
            AddFilter(x => x.FirstName.ToLower().Contains(specParams.FirstName.ToLower()));
        }

        if (!string.IsNullOrEmpty(specParams.LastName)) // With last name
        {
            AddFilter(x => x.LastName.ToLower().Contains(specParams.LastName.ToLower()));
        }

        if (!string.IsNullOrEmpty(specParams.Gender)) // With gender
        {
            AddFilter(x => x.Gender == specParams.Gender);
        }

        if (specParams.IsActive != null) // With status
        {
            AddFilter(x => x.IsActive == specParams.IsActive);
        }

        if (specParams.IsDeleted != null) // Is deleted
        {
            AddFilter(x => x.IsDeleted == specParams.IsDeleted);
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
                AddFilter(x => x.Dob <= specParams.DobRange[1]);
            }
            else if (specParams.DobRange[0].HasValue && specParams.DobRange[1] is null)
            {
                AddFilter(x => x.Dob >= specParams.DobRange[0]);
            }
        }

        if (specParams.CreateDateRange != null
            && specParams.CreateDateRange.Length > 1) // With range of create date 
        {
            if (specParams.CreateDateRange[0].HasValue && specParams.CreateDateRange[1].HasValue)
            {
                AddFilter(x =>
                    x.CreateDate.Date >= specParams.CreateDateRange[0]!.Value.Date
                    && x.CreateDate.Date <= specParams.CreateDateRange[1]!.Value.Date);
            }
            else if (specParams.CreateDateRange[0] is null && specParams.CreateDateRange[1].HasValue)
            {
                AddFilter(x => x.CreateDate.Date <= specParams.CreateDateRange[1]);
            }
            else if (specParams.CreateDateRange[0].HasValue && specParams.CreateDateRange[1] is null)
            {
                AddFilter(x => x.CreateDate.Date >= specParams.CreateDateRange[0]);
            }
        }
        
        // With range of modified date like dob
        if (specParams.ModifiedDateRange != null
            && specParams.ModifiedDateRange.Length > 1) // With range of dob
        {
            if (specParams.ModifiedDateRange[0].HasValue && specParams.ModifiedDateRange[1].HasValue)
            {
                AddFilter(x => x.ModifiedDate.HasValue &&
                               x.ModifiedDate.Value.Date >= specParams.ModifiedDateRange[0]!.Value.Date
                               && x.ModifiedDate.Value.Date <= specParams.ModifiedDateRange[1]!.Value.Date);
            }
            else if ((specParams.ModifiedDateRange[0] is null && specParams.ModifiedDateRange[1].HasValue))
            {
                AddFilter(x => x.ModifiedDate <= specParams.ModifiedDateRange[1]);
            }
            else if (specParams.ModifiedDateRange[0].HasValue && specParams.ModifiedDateRange[1] is null)
            {
                AddFilter(x => x.ModifiedDate >= specParams.ModifiedDateRange[0]);
            }
        }
        
        if (specParams.HireDateRange != null
            && specParams.HireDateRange.Length > 1) // With range of dob
        {
            if (specParams.HireDateRange[0].HasValue && specParams.HireDateRange[1].HasValue)
            {
                AddFilter(x => x.HireDate.HasValue &&
                               x.HireDate.Value.Date >= specParams.HireDateRange[0]!.Value.Date
                               && x.HireDate.Value.Date <= specParams.HireDateRange[1]!.Value.Date);
            }
            else if ((specParams.HireDateRange[0] is null && specParams.HireDateRange[1].HasValue))
            {
                AddFilter(x => x.HireDate <= specParams.HireDateRange[1]);
            }
            else if (specParams.HireDateRange[0].HasValue && specParams.HireDateRange[1] is null)
            {
                AddFilter(x => x.HireDate >= specParams.HireDateRange[0]);
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
            // Default order by create date
            AddOrderByDescending(u => u.CreateDate);
        }

        // Apply paging 
        // ApplyPaging(skip: pageSize * (pageIndex - 1), take: pageSize);
    }
    
    private void ApplySorting(string propertyName, bool isDescending)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // Initialize expression parameter with type of Employee (x)
        var parameter = Expression.Parameter(typeof(Employee), "x");
        // Assign property base on property name (x.PropertyName)
        var property = Expression.Property(parameter, propertyName);
        // Building a complete sort lambda expression (x => x.PropertyName)
        var sortExpression =
            Expression.Lambda<Func<Employee, object>>(Expression.Convert(property, typeof(object)), parameter);

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