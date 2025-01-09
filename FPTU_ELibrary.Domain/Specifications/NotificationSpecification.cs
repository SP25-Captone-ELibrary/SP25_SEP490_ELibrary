using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.EntityFrameworkCore;

namespace FPTU_ELibrary.Domain.Specifications;

public class NotificationSpecification : BaseSpecification<Notification>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }

    public NotificationSpecification(
        NotificationSpecParams notificationSpecParams,
        int pageIndex,
        int pageSize,
        string email,
        int roleId,
        bool isManagement
    ) : base(n =>
    (
        string.IsNullOrEmpty(notificationSpecParams.Search) ||
        (
            (!string.IsNullOrEmpty(n.Title) && n.Title.Contains(notificationSpecParams.Search)) ||
            (!string.IsNullOrEmpty(n.Message) && n.Message.Contains(notificationSpecParams.Search)) ||
            (!string.IsNullOrEmpty(n.CreatedBy) && n.CreatedBy.Contains(notificationSpecParams.Search)) ||
            (!string.IsNullOrEmpty(n.NotificationType) && n.NotificationType.Contains(notificationSpecParams.Search))
        )
    ))
    {
        PageIndex = pageIndex;
        PageSize = pageSize;

        EnableSplitQuery();
            if (roleId == 1) // Admin
        {
            AddFilter(x => x.IsPublic);
        }
        else if (isManagement)
        {
            AddFilter(x => x.CreatedBy.Equals(email) || x.IsPublic);
        }
        else
        {
            AddFilter(x => x.IsPublic || x.NotificationRecipients.Any(r => r.Recipient.Email.Equals(email)));
        }
        ApplyInclude(q => q.Include(n => n.NotificationRecipients));

        if (notificationSpecParams.Title != null)
        {
            AddFilter(x => x.Title == notificationSpecParams.Title);
        }
        else if (!string.IsNullOrEmpty(notificationSpecParams.Message)) // With gender
        {
            AddFilter(x => x.Message == notificationSpecParams.Message);
        }
        else if (!string.IsNullOrEmpty(notificationSpecParams.CreatedBy)) // With gender
        {
            AddFilter(x => x.CreatedBy == notificationSpecParams.CreatedBy);
        }
        else if (!string.IsNullOrEmpty(notificationSpecParams.NotificationType)) // With gender
        {
            AddFilter(x => x.NotificationType == notificationSpecParams.NotificationType);
        }
        else if (notificationSpecParams.CreateDateRange != null
                 && notificationSpecParams.CreateDateRange.Length > 1) // With range of dob
        {
            AddFilter(x =>
                x.CreateDate.Date >= notificationSpecParams.CreateDateRange[0].Date
                && x.CreateDate.Date <= notificationSpecParams.CreateDateRange[1].Date);
        }

        if (!string.IsNullOrEmpty(notificationSpecParams.Sort))
        {
            var sortBy = notificationSpecParams.Sort.Trim();
            var isDescending = sortBy.StartsWith("-");
            var propertyName = isDescending ? sortBy.Substring(1) : sortBy;

            ApplySorting(propertyName, isDescending);
        }
        else
        {
            AddOrderByDescending(n => n.CreateDate);
        }
    }

    private void ApplySorting(string propertyName, bool isDescending)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // Use Reflection to dynamically apply sorting
        var parameter = Expression.Parameter(typeof(Notification), "x");
        var property = Expression.Property(parameter, propertyName);
        var sortExpression =
            Expression.Lambda<Func<Notification, object>>(Expression.Convert(property, typeof(object)), parameter);

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