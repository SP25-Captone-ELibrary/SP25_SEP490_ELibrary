using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;

namespace FPTU_ELibrary.Domain.Specifications;

public class CategorySpecification : BaseSpecification<Category>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }

    public CategorySpecification(CategorySpecParams categorySpecParams,
        int pageSize, int pageIndex) : base(bc =>
        (string.IsNullOrEmpty(categorySpecParams.Search) ||
         ((!string.IsNullOrEmpty(bc.EnglishName) && bc.EnglishName.Contains(categorySpecParams.Search)) ||
          (!string.IsNullOrEmpty(bc.VietnameseName) && bc.VietnameseName.Contains(categorySpecParams.Search))
         )
        ))
    {
        PageIndex = pageIndex;
        PageSize = pageSize;

        EnableSplitQuery();

        if (categorySpecParams.EnglishName != null)
        {
            AddFilter(x =>
                x.EnglishName.Contains(categorySpecParams.EnglishName));
        }

        if (categorySpecParams.VietnameseName != null)
        {
            AddFilter(x => 
                           x.VietnameseName.Contains(categorySpecParams.VietnameseName));
        }

        if (!string.IsNullOrEmpty(categorySpecParams.Sort))
        {
            var sortBy = categorySpecParams.Sort.Trim();
            var isDescending = sortBy.StartsWith("-");
            var propertyName = isDescending ? sortBy.Substring(1) : sortBy;

            ApplySorting(propertyName, isDescending);
        }
    }

    private void ApplySorting(string propertyName, bool isDescending)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // Use Reflection to dynamically apply sorting
        var parameter = Expression.Parameter(typeof(Category), "x");
        var property = Expression.Property(parameter, propertyName);
        var sortExpression =
            Expression.Lambda<Func<Category, object>>(Expression.Convert(property, typeof(object)), parameter);

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