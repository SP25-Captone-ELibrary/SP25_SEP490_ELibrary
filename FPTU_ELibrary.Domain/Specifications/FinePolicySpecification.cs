using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;

namespace FPTU_ELibrary.Domain.Specifications;

public class FinePolicySpecification : BaseSpecification<FinePolicy>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }

    //GENERATE CODE WITH THE SAME FORMAT AS NotificationSpecification
    public FinePolicySpecification(
        FinePolicyParams finePolicyParams,
        int pageIndex,
        int pageSize
    ) : base(n =>
    (
        string.IsNullOrEmpty(finePolicyParams.Search) ||
        (
            (!string.IsNullOrEmpty(n.ConditionType) && n.ConditionType.Contains(finePolicyParams.Search)) ||
            (!string.IsNullOrEmpty(n.Description) && n.Description.Contains(finePolicyParams.Search))
        )
    ))
    {
        PageIndex = pageIndex;
        PageSize = pageSize;

        EnableSplitQuery();

        if (finePolicyParams.FineAmountPerDay != null)
        {
            AddFilter(x => x.FineAmountPerDay == finePolicyParams.FineAmountPerDay);
        }
        else if (finePolicyParams.FixedFineAmount != null)
        {
            AddFilter(x => x.FixedFineAmount == finePolicyParams.FixedFineAmount);
        }
        else if (!string.IsNullOrEmpty(finePolicyParams.ConditionType))
        {
            AddFilter(x => x.ConditionType.Contains(finePolicyParams.ConditionType));
        }
        else if (!string.IsNullOrEmpty(finePolicyParams.Description))
        {
            AddFilter(x => x.Description.Contains(finePolicyParams.Description));
        }

        if (!string.IsNullOrEmpty(finePolicyParams.Sort))
        {
            var sortBy = finePolicyParams.Sort.Trim();
            var isDescending = sortBy.StartsWith("-");
            var propertyName = isDescending ? sortBy.Substring(1) : sortBy;

            ApplySorting(propertyName, isDescending);
        }
        else
        {
            AddOrderByDescending(n => n.FinePolicyId);
        }
    }

    private void ApplySorting(string propertyName, bool isDescending)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // Use Reflection to dynamically apply sorting
        var parameter = Expression.Parameter(typeof(FinePolicy), "x");
        var property = Expression.Property(parameter, propertyName);
        var sortExpression =
            Expression.Lambda<Func<FinePolicy, object>>(Expression.Convert(property, typeof(object)), parameter);

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