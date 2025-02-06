using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.EntityFrameworkCore;

namespace FPTU_ELibrary.Domain.Specifications;

public class BorrowRequestSpecification : BaseSpecification<BorrowRequest>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    
    public BorrowRequestSpecification(BorrowRequestSpecParams specParams, int pageIndex, int pageSize)
        :base(br => 
            // Search with terms
            string.IsNullOrEmpty(specParams.Search) || 
            (
                // Description
                (!string.IsNullOrEmpty(br.Description) && br.Description.Contains(specParams.Search)) ||
                // Cancellation reason
                (!string.IsNullOrEmpty(br.CancellationReason) && br.CancellationReason.Contains(specParams.Search)) ||
                // BorrowRequestDetails
                // BorrowRequest -> BorrowRequestDetails
                br.BorrowRequestDetails.Any(brd => 
                    // Item title
                    !string.IsNullOrEmpty(brd.LibraryItem.Title) && brd.LibraryItem.Title.Contains(specParams.Search) ||
                    // Item ISBN
                    !string.IsNullOrEmpty(brd.LibraryItem.Isbn) && brd.LibraryItem.Isbn.Contains(specParams.Search) ||
                    // Item Cutter number
                    !string.IsNullOrEmpty(brd.LibraryItem.CutterNumber) && brd.LibraryItem.CutterNumber.Contains(specParams.Search) ||
                    // Item DDC
                    !string.IsNullOrEmpty(brd.LibraryItem.ClassificationNumber) && brd.LibraryItem.ClassificationNumber.Contains(specParams.Search) ||
                    // Item Genres
                    !string.IsNullOrEmpty(brd.LibraryItem.Genres) && brd.LibraryItem.Genres.Contains(specParams.Search) ||
                    // Item TopicalTerms
                    !string.IsNullOrEmpty(brd.LibraryItem.TopicalTerms) && brd.LibraryItem.TopicalTerms.Contains(specParams.Search)
                )
            ))
    {
        // Pagination
        PageIndex = pageIndex;
        PageSize = pageSize;
        
        // Enable split query
        EnableSplitQuery();
        
        // Apply include
        ApplyInclude(q => q
            .Include(br => br.BorrowRequestDetails)
                .ThenInclude(brd => brd.LibraryItem)
        );
        
        // Add filter 
        if (specParams.Status != null) // Status
        {
            AddFilter(br => br.Status == specParams.Status);
        }
        
        if (specParams.RequestDateRange != null
            && specParams.RequestDateRange.Length > 1) // With range of request date
        {
            if (specParams.RequestDateRange[0].HasValue && specParams.RequestDateRange[1].HasValue)
            {
                AddFilter(x => x.RequestDate.Date >= specParams.RequestDateRange[0]!.Value.Date
                               && x.RequestDate.Date <= specParams.RequestDateRange[1]!.Value.Date);
            }
            else if ((specParams.RequestDateRange[0] is null && specParams.RequestDateRange[1].HasValue))
            {
                AddFilter(x => x.RequestDate.Date <= specParams.RequestDateRange[1]!.Value.Date);
            }
            else if (specParams.RequestDateRange[0].HasValue && specParams.RequestDateRange[1] is null)
            {
                AddFilter(x => x.RequestDate.Date >= specParams.RequestDateRange[0]!.Value.Date);
            }
        }
        
        if (specParams.ExpirationDateRange != null
            && specParams.ExpirationDateRange.Length > 1) // With range of expiration date
        {
            if (specParams.ExpirationDateRange[0].HasValue && specParams.ExpirationDateRange[1].HasValue)
            {
                AddFilter(x => x.ExpirationDate.Date >= specParams.ExpirationDateRange[0]!.Value.Date
                               && x.ExpirationDate.Date <= specParams.ExpirationDateRange[1]!.Value.Date);
            }
            else if ((specParams.ExpirationDateRange[0] is null && specParams.ExpirationDateRange[1].HasValue))
            {
                AddFilter(x => x.ExpirationDate.Date <= specParams.ExpirationDateRange[1]!.Value.Date);
            }
            else if (specParams.ExpirationDateRange[0].HasValue && specParams.ExpirationDateRange[1] is null)
            {
                AddFilter(x => x.ExpirationDate.Date >= specParams.ExpirationDateRange[0]!.Value.Date);
            }
        }
        
        if (specParams.CancelledAtRange != null
            && specParams.CancelledAtRange.Length > 1) // With range of cancel date
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
            AddOrderByDescending(n => n.RequestDate);
        }
    }
    
    private void ApplySorting(string propertyName, bool isDescending)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // Initialize expression parameter with type of LibraryItem (x)
        var parameter = Expression.Parameter(typeof(BorrowRequest), "x");
        // Assign property base on property name (x.PropertyName)
        var property = Expression.Property(parameter, propertyName);
        // Building a complete sort lambda expression (x => x.PropertyName)
        var sortExpression =
            Expression.Lambda<Func<BorrowRequest, object>>(Expression.Convert(property, typeof(object)), parameter);

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