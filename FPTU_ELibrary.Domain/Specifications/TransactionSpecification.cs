using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.EntityFrameworkCore;

namespace FPTU_ELibrary.Domain.Specifications;

public class TransactionSpecification : BaseSpecification<Transaction>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }

    public TransactionSpecification(TransactionSpecParams specParams, int pageIndex, int pageSize,
        string? email = null, Guid? userId = null)
        : base(t =>
            string.IsNullOrEmpty(specParams.Search) ||
            (!string.IsNullOrEmpty(t.TransactionCode) && t.TransactionCode.Contains(specParams.Search)) ||
            (!string.IsNullOrEmpty(t.Description) && t.Description.Contains(specParams.Search))
        )
    {
        // Pagination
        PageIndex = pageIndex;
        PageSize = pageSize;

        // Enable split query
        EnableSplitQuery();

        ApplyInclude(q => q.Include(t => t.User));
        
        // Filter
        if (!string.IsNullOrEmpty(email))
        {
            AddFilter(t => t.User.Email == email);
        }
        if (userId != null && userId != Guid.Empty)
        {
            AddFilter(t => t.UserId == userId);
        }
        if (specParams.TransactionStatus != null)
        {
            AddFilter(t => t.TransactionStatus == specParams.TransactionStatus);
        }
        if (specParams.TransactionType != null)
        {
            AddFilter(t => t.TransactionType == specParams.TransactionType);
        }
        if (specParams.TransactionMethod != null)
        {
            AddFilter(t => t.TransactionMethod == specParams.TransactionMethod);
        }
        
        if (specParams.TransactionDateRange != null
            && specParams.TransactionDateRange.Length > 1) // With range of transaction date
        {
            if (specParams.TransactionDateRange[0].HasValue && specParams.TransactionDateRange[1].HasValue)
            {
                AddFilter(x => x.TransactionDate.HasValue &&
                    x.TransactionDate.Value.Date >= specParams.TransactionDateRange[0]!.Value.Date &&
                    x.TransactionDate.Value.Date <= specParams.TransactionDateRange[1]!.Value.Date);
            }
            else if ((specParams.TransactionDateRange[0] is null && specParams.TransactionDateRange[1].HasValue))
            {
                AddFilter(x => x.TransactionDate.HasValue &&
                    x.TransactionDate.Value.Date <= specParams.TransactionDateRange[1]!.Value.Date);
            }
            else if (specParams.TransactionDateRange[0].HasValue && specParams.TransactionDateRange[1] is null)
            {
                AddFilter(x => x.TransactionDate.HasValue &&
                    x.TransactionDate.Value.Date >= specParams.TransactionDateRange[0]!.Value.Date);
            }
        }
        
        if (specParams.ExpiredAtRange != null
            && specParams.ExpiredAtRange.Length > 1) // With range of expired at
        {
            if (specParams.ExpiredAtRange[0].HasValue && specParams.ExpiredAtRange[1].HasValue)
            {
                AddFilter(x => x.ExpiredAt.HasValue &&
                               x.ExpiredAt.Value.Date >= specParams.ExpiredAtRange[0]!.Value.Date &&
                               x.ExpiredAt.Value.Date <= specParams.ExpiredAtRange[1]!.Value.Date);
            }
            else if ((specParams.ExpiredAtRange[0] is null && specParams.ExpiredAtRange[1].HasValue))
            {
                AddFilter(x => x.ExpiredAt.HasValue &&
                               x.ExpiredAt.Value.Date <= specParams.ExpiredAtRange[1]!.Value.Date);
            }
            else if (specParams.ExpiredAtRange[0].HasValue && specParams.ExpiredAtRange[1] is null)
            {
                AddFilter(x => x.ExpiredAt.HasValue &&
                               x.ExpiredAt.Value.Date >= specParams.ExpiredAtRange[0]!.Value.Date);
            }
        }
        
        if (specParams.CancelledAtRange != null
            && specParams.CancelledAtRange.Length > 1) // With range of cancellation date
        {
            if (specParams.CancelledAtRange[0].HasValue && specParams.CancelledAtRange[1].HasValue)
            {
                AddFilter(x => x.CancelledAt!.Value.Date >= specParams.CancelledAtRange[0]!.Value.Date
                               && x.CancelledAt!.Value.Date <= specParams.CancelledAtRange[1]!.Value.Date);
            }
            else if ((specParams.CancelledAtRange[0] is null && specParams.CancelledAtRange[1].HasValue))
            {
                AddFilter(x => x.CancelledAt!.Value.Date <= specParams.CancelledAtRange[1]!.Value.Date);
            }
            else if (specParams.CancelledAtRange[0].HasValue && specParams.CancelledAtRange[1] is null)
            {
                AddFilter(x => x.CancelledAt!.Value.Date >= specParams.CancelledAtRange[0]!.Value.Date);
            }
        }

        if (specParams.AmountRange != null
            && specParams.AmountRange.Length > 1)
        {
            if (specParams.AmountRange[0].HasValue && specParams.AmountRange[1].HasValue)
            {
                AddFilter(x => x.Amount >= specParams.AmountRange[0]!.Value
                               && x.Amount <= specParams.AmountRange[1]!.Value);
            }
            else if ((specParams.AmountRange[0] is null && specParams.AmountRange[1].HasValue))
            {
                AddFilter(x => x.Amount <= specParams.AmountRange[1]!.Value);
            }
            else if (specParams.AmountRange[0].HasValue && specParams.AmountRange[1] is null)
            {
                AddFilter(x => x.Amount >= specParams.AmountRange[0]!.Value);
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
            AddOrderByDescending(u => u.CreatedAt);
        }
    }

    private void ApplySorting(string propertyName, bool isDescending)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // Initialize expression parameter with type of Transaction (x)
        var parameter = Expression.Parameter(typeof(Transaction), "x");
        // Assign property base on property name (x.PropertyName)
        var property = Expression.Property(parameter, propertyName);
        // Building a complete sort lambda expression (x => x.PropertyName)
        var sortExpression =
            Expression.Lambda<Func<Transaction, object>>(Expression.Convert(property, typeof(object)), parameter);

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