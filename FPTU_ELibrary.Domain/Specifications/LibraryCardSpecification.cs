using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;

namespace FPTU_ELibrary.Domain.Specifications;

public class LibraryCardSpecification : BaseSpecification<LibraryCard>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    
    public LibraryCardSpecification(LibraryCardSpecParams specParams, int pageIndex, int pageSize)
        : base(lc => 
            string.IsNullOrEmpty(specParams.Search) || 
            (!string.IsNullOrEmpty(lc.FullName) && lc.FullName.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(lc.Barcode) && lc.Barcode.Contains(specParams.Search))
            )
    {
        // Pagination
        PageIndex = pageIndex;
        PageSize = pageSize;
        
        // Enable split query
        EnableSplitQuery();
        
        // Add filter
        if (specParams.IssuanceMethod != null) // Issuance method
        {
            AddFilter(lc => lc.IssuanceMethod == specParams.IssuanceMethod);
        }
        if (specParams.Status != null) // Status
        {
            AddFilter(lc => lc.Status == specParams.Status);
        }
        if (specParams.IsAllowToBorrowMore != null) // Is allow to borrow more
        {
            AddFilter(lc => lc.IsAllowBorrowMore == specParams.IsAllowToBorrowMore);
        }
        if (specParams.IsReminderSent != null) // Is reminder sent
        {
            AddFilter(lc => lc.IsReminderSent == specParams.IsReminderSent);
        }
        if (specParams.IsExtended != null) // Is extended
        {
            AddFilter(lc => lc.IsExtended == specParams.IsExtended);
        }
        if (specParams.IsArchived != null) // Is archived
        {
            AddFilter(lc => lc.IsArchived == specParams.IsArchived);
        }
        if (specParams.IssueDateRange != null
            && specParams.IssueDateRange.Length > 1) // With range of issue date
        {
            if (specParams.IssueDateRange[0].HasValue && specParams.IssueDateRange[1].HasValue)
            {
                AddFilter(x => x.IssueDate.Date >= specParams.IssueDateRange[0]!.Value.Date
                               && x.IssueDate.Date <= specParams.IssueDateRange[1]!.Value.Date);
            }
            else if ((specParams.IssueDateRange[0] is null && specParams.IssueDateRange[1].HasValue))
            {
                AddFilter(x => x.IssueDate.Date <= specParams.IssueDateRange[1]!.Value.Date);
            }
            else if (specParams.IssueDateRange[0].HasValue && specParams.IssueDateRange[1] is null)
            {
                AddFilter(x => x.IssueDate.Date >= specParams.IssueDateRange[0]!.Value.Date);
            }
        }
        if (specParams.ExpiryDateRange != null
            && specParams.ExpiryDateRange.Length > 1) // With range of expiry date
        {
            if (specParams.ExpiryDateRange[0].HasValue && specParams.ExpiryDateRange[1].HasValue)
            {
                AddFilter(x => x.ExpiryDate.HasValue && 
                                  x.ExpiryDate.Value.Date >= specParams.ExpiryDateRange[0]!.Value.Date
                               && x.ExpiryDate.Value.Date <= specParams.ExpiryDateRange[1]!.Value.Date);
            }
            else if ((specParams.ExpiryDateRange[0] is null && specParams.ExpiryDateRange[1].HasValue))
            {
                AddFilter(x => x.ExpiryDate.HasValue &&
                               x.ExpiryDate.Value.Date <= specParams.ExpiryDateRange[1]!.Value.Date);
            }
            else if (specParams.ExpiryDateRange[0].HasValue && specParams.ExpiryDateRange[1] is null)
            {
                AddFilter(x => x.ExpiryDate.HasValue &&
                               x.ExpiryDate.Value.Date >= specParams.ExpiryDateRange[0]!.Value.Date);
            }
        }
        if (specParams.SuspensionEndDateRange != null
            && specParams.SuspensionEndDateRange.Length > 1) // With range of suspension end date
        {
            if (specParams.SuspensionEndDateRange[0].HasValue && specParams.SuspensionEndDateRange[1].HasValue)
            {
                AddFilter(x => x.SuspensionEndDate.HasValue && 
                               x.SuspensionEndDate.Value.Date >= specParams.SuspensionEndDateRange[0]!.Value.Date
                               && x.SuspensionEndDate.Value.Date <= specParams.SuspensionEndDateRange[1]!.Value.Date);
            }
            else if ((specParams.SuspensionEndDateRange[0] is null && specParams.SuspensionEndDateRange[1].HasValue))
            {
                AddFilter(x => x.SuspensionEndDate.HasValue &&
                               x.SuspensionEndDate.Value.Date <= specParams.SuspensionEndDateRange[1]!.Value.Date);
            }
            else if (specParams.SuspensionEndDateRange[0].HasValue && specParams.SuspensionEndDateRange[1] is null)
            {
                AddFilter(x => x.SuspensionEndDate.HasValue &&
                               x.SuspensionEndDate.Value.Date >= specParams.SuspensionEndDateRange[0]!.Value.Date);
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
            AddOrderByDescending(u => u.IssueDate);
        }
    }
    
    private void ApplySorting(string propertyName, bool isDescending)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // Initialize expression parameter with type of Employee (x)
        var parameter = Expression.Parameter(typeof(LibraryCard), "x");
        // Assign property base on property name (x.PropertyName)
        var property = Expression.Property(parameter, propertyName);
        // Building a complete sort lambda expression (x => x.PropertyName)
        var sortExpression =
            Expression.Lambda<Func<LibraryCard, object>>(Expression.Convert(property, typeof(object)), parameter);

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