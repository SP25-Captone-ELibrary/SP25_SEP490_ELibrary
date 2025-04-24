using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;

namespace FPTU_ELibrary.Domain.Specifications;

public class LibraryClosureDaySpecification : BaseSpecification<LibraryClosureDay>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }

    public LibraryClosureDaySpecification(LibraryClosureDaySpecParams specParams, int pageIndex, int pageSize)
        :base(s => 
            string.IsNullOrEmpty(specParams.Search) ||
            (
                (s.Day > 0 && specParams.ParsedSearchDate.HasValue && s.Day == specParams.ParsedSearchDate.Value.Day) ||
                (s.Month > 0 && specParams.ParsedSearchDate.HasValue && s.Month == specParams.ParsedSearchDate.Value.Month) ||
                (s.Year > 0 && specParams.ParsedSearchDate.HasValue && s.Year == specParams.ParsedSearchDate.Value.Year) ||
                (!string.IsNullOrEmpty(s.VieDescription) && s.VieDescription.Contains(specParams.Search)) ||
                (!string.IsNullOrEmpty(s.EngDescription) && s.EngDescription.Contains(specParams.Search))
            )
        )
    {
        // Pagination
        PageIndex = pageIndex;
        PageSize = pageSize;
        
        // Apply sorting
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
            AddOrderByDescending(n => n.ClosureDayId);
        }
    }
    
    private void ApplySorting(string propertyName, bool isDescending)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // Initialize expression parameter with type of LibraryItem (x)
        var parameter = Expression.Parameter(typeof(LibraryClosureDay), "x");
        // Assign property base on property name (x.PropertyName)
        var property = Expression.Property(parameter, propertyName);
        // Building a complete sort lambda expression (x => x.PropertyName)
        var sortExpression =
            Expression.Lambda<Func<LibraryClosureDay, object>>(Expression.Convert(property, typeof(object)), parameter);

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