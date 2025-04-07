using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.EntityFrameworkCore;

namespace FPTU_ELibrary.Domain.Specifications;

public class SupplementRequestDetailSpecification : BaseSpecification<SupplementRequestDetail>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }

    public SupplementRequestDetailSpecification(SupplementRequestDetailSpecParams specParams, 
        int pageIndex, int pageSize)
        : base(w =>
            string.IsNullOrEmpty(specParams.Search) ||
            (
                (!string.IsNullOrEmpty(w.Title) && w.Title.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(w.Author) && w.Author.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(w.Publisher) && w.Publisher.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(w.PublishedDate) && w.PublishedDate.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(w.Description) && w.Description.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(w.Isbn) && w.Isbn.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(w.Dimensions) && w.Dimensions.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(w.Categories) && w.Categories.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(w.Language) && w.Language.Contains(specParams.Search))
            )
        )
    {
        // Assign pagination fields
        PageIndex = pageIndex;
        PageSize = pageSize;
        
        // Apply including supplier
        ApplyInclude(q => q
            .Include(w => w.RelatedLibraryItem)
                .ThenInclude(li => li!.LibraryItemInventory)
            .Include(w => w.RelatedLibraryItem)
                .ThenInclude(w => w.Category)
            .Include(w => w.RelatedLibraryItem).ThenInclude(li => li!.Shelf)
            .Include(w => w.RelatedLibraryItem)
                .ThenInclude(li => li!.LibraryItemAuthors)
                    .ThenInclude(lia => lia.Author)
        );
        
        // Enable split query
        EnableSplitQuery();
        
        // Total item range
        if (specParams.PageCountRange != null
            && specParams.PageCountRange.Length > 1)
        {
            if (specParams.PageCountRange[0].HasValue && specParams.PageCountRange[1].HasValue)
            {
                AddFilter(x => 
                    x.PageCount >= specParams.PageCountRange[0]!.Value
                    && x.PageCount <= specParams.PageCountRange[1]!.Value);
            }else if (specParams.PageCountRange[0] is null && specParams.PageCountRange[1].HasValue)
            {
                AddFilter(x => x.PageCount <= specParams.PageCountRange[1]!.Value);
            }
            else if(specParams.PageCountRange[0].HasValue && specParams.PageCountRange[1] is null)
            {
                AddFilter(x => x.PageCount >= specParams.PageCountRange[0]!.Value);
            }
        }
        
        // Average rating range
        if (specParams.AverageRatingRange != null
            && specParams.AverageRatingRange.Length > 1)
        {
            if (specParams.AverageRatingRange[0].HasValue && specParams.AverageRatingRange[1].HasValue)
            {
                AddFilter(x => 
                    x.AverageRating >= specParams.AverageRatingRange[0]!.Value
                    && x.AverageRating <= specParams.AverageRatingRange[1]!.Value);
            }else if (specParams.AverageRatingRange[0] is null && specParams.AverageRatingRange[1].HasValue)
            {
                AddFilter(x => x.AverageRating <= specParams.AverageRatingRange[1]!.Value);
            }
            else if(specParams.AverageRatingRange[0].HasValue && specParams.AverageRatingRange[1] is null)
            {
                AddFilter(x => x.AverageRating >= specParams.AverageRatingRange[0]!.Value);
            }
        }
        
        // Rating counts
        if (specParams.RatingsCountRange != null
            && specParams.RatingsCountRange.Length > 1)
        {
            if (specParams.RatingsCountRange[0].HasValue && specParams.RatingsCountRange[1].HasValue)
            {
                AddFilter(x => 
                    x.RatingsCount >= specParams.RatingsCountRange[0]!.Value
                    && x.RatingsCount <= specParams.RatingsCountRange[1]!.Value);
            }else if (specParams.RatingsCountRange[0] is null && specParams.RatingsCountRange[1].HasValue)
            {
                AddFilter(x => x.RatingsCount <= specParams.RatingsCountRange[1]!.Value);
            }
            else if(specParams.RatingsCountRange[0].HasValue && specParams.RatingsCountRange[1] is null)
            {
                AddFilter(x => x.RatingsCount >= specParams.RatingsCountRange[0]!.Value);
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
            AddOrderBy(n => n.SupplementRequestDetailId);
        }
    }
    
    private void ApplySorting(string propertyName, bool isDescending)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // Initialize expression parameter with type of LibraryItem (x)
        var parameter = Expression.Parameter(typeof(SupplementRequestDetail), "x");
        // Assign property base on property name (x.PropertyName)
        var property = Expression.Property(parameter, propertyName);
        // Building a complete sort lambda expression (x => x.PropertyName)
        var sortExpression =
            Expression.Lambda<Func<SupplementRequestDetail, object>>(Expression.Convert(property, typeof(object)), parameter);

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