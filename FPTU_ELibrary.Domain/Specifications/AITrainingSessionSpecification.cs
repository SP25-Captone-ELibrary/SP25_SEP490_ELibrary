using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.EntityFrameworkCore;

namespace FPTU_ELibrary.Domain.Specifications;

public class AITrainingSessionSpecification: BaseSpecification<AITrainingSession>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }

    public AITrainingSessionSpecification(AITrainingSessionSpecParams aiTrainingSessionSpecParams
        ,int pageIndex, int pageSize)
    :base(ats =>
        // Search with terms
        string.IsNullOrEmpty(aiTrainingSessionSpecParams.Search) ||
        (
            // TrainingStatus
            (!string.IsNullOrEmpty(ats.TrainingStatus.ToString()) && ats.TrainingStatus.ToString()
                .Contains(aiTrainingSessionSpecParams.Search))
        ))
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        // Enable split query
        EnableSplitQuery();
        //Include AITrainingDetail
        ApplyInclude(q => q
            .Include(e => e.TrainingDetails)
            .ThenInclude(atd => atd.TrainingImages));
        AddOrderBy(ats => ats.TrainingSessionId);
        if(!string.IsNullOrEmpty(aiTrainingSessionSpecParams.TrainingStatus))
        {
            AddFilter(x => x.TrainingStatus.ToString() == aiTrainingSessionSpecParams.TrainingStatus);
        }

        if (aiTrainingSessionSpecParams.TrainDateRange != null
            && aiTrainingSessionSpecParams.TrainDateRange.Length > 1)
        {
            if (aiTrainingSessionSpecParams.TrainDateRange[0] is null && aiTrainingSessionSpecParams.TrainDateRange[1].HasValue)
            {
                AddFilter(x => x.TrainDate <= aiTrainingSessionSpecParams.TrainDateRange[1]);
            }
            else if (aiTrainingSessionSpecParams.TrainDateRange[0].HasValue && aiTrainingSessionSpecParams.TrainDateRange[1] is null)
            {
                AddFilter(x => x.TrainDate >= aiTrainingSessionSpecParams.TrainDateRange[0]);
            }
            else
            {
                AddFilter(x => 
                               x.TrainDate.Date >= aiTrainingSessionSpecParams.TrainDateRange[0]!.Value.Date
                               && x.TrainDate.Date <= aiTrainingSessionSpecParams.TrainDateRange[1]!.Value.Date);
            }
        }
        if (!string.IsNullOrEmpty(aiTrainingSessionSpecParams.Sort))
        {
            var sortBy = aiTrainingSessionSpecParams.Sort.Trim();
            var isDescending = sortBy.StartsWith("-");
            var propertyName = isDescending ? sortBy.Substring(1) : sortBy;

            ApplySorting(propertyName, isDescending);
        }
        else
        {
            // Default order by create date
            AddOrderByDescending(u => u.TrainDate);
        }
    }
    private void ApplySorting(string propertyName, bool isDescending)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // Use Reflection to dynamically apply sorting
        var parameter = Expression.Parameter(typeof(AITrainingSession), "x");
        var property = Expression.Property(parameter, propertyName);
        var sortExpression =
            Expression.Lambda<Func<AITrainingSession, object>>(Expression.Convert(property, typeof(object)), parameter);

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