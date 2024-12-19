using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;

namespace FPTU_ELibrary.Domain.Specifications;

public class BookCategorySpecification : BaseSpecification<BookCategory>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }

    public BookCategorySpecification(BookCategorySpecParams bookCategorySpecParams,
        int pageSize, int pageIndex) : base(bc =>
        (string.IsNullOrEmpty(bookCategorySpecParams.Search) ||
         ((!string.IsNullOrEmpty(bc.EnglishName) && bc.EnglishName.Contains(bookCategorySpecParams.Search)) ||
          (!string.IsNullOrEmpty(bc.VietnameseName) && bc.VietnameseName.Contains(bookCategorySpecParams.Search))
         )
        ))
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        
        EnableSplitQuery();

        if (bookCategorySpecParams.EnglishName != null)
        {
            AddFilter(x => x.IsDelete == bookCategorySpecParams.IsDelete &&
                           x.EnglishName.Contains(bookCategorySpecParams.EnglishName));
        }
        if (bookCategorySpecParams.VietnameseName != null)
        {
            AddFilter(x => x.IsDelete == bookCategorySpecParams.IsDelete &&
                           x.VietnameseName.Contains(bookCategorySpecParams.VietnameseName));
        }
        if (!string.IsNullOrEmpty(bookCategorySpecParams.Sort))
        {
            var sortBy = bookCategorySpecParams.Sort.Trim();
            var isDescending = sortBy.StartsWith("-");
            var propertyName = isDescending ? sortBy.Substring(1) : sortBy;

            ApplySorting(propertyName, isDescending);
        }
    }
    private void ApplySorting(string propertyName, bool isDescending)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // Use Reflection to dynamically apply sorting
        var parameter = Expression.Parameter(typeof(BookCategory), "x");
        var property = Expression.Property(parameter, propertyName);
        var sortExpression = Expression.Lambda<Func<BookCategory, object>>(Expression.Convert(property, typeof(object)), parameter);

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