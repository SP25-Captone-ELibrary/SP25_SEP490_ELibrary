using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.EntityFrameworkCore;

namespace FPTU_ELibrary.Domain.Specifications;

public class NotificationSpecification :BaseSpecification<Notification>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }

    public NotificationSpecification(
        NotificationSpecParams notificationSpecParams, 
        int pageIndex, 
        int pageSize, 
        string email, 
        int roleId
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
        AddOrderByDescending(n => n.CreateDate);

        if (roleId == 1) // Admin
        {
            AddFilter(x => x.IsPublic);
        }
        else // Regular user
        {
            AddFilter(x => x.IsPublic || x.NotificationRecipients.Any(r => r.Recipient.Email == email));
        }

        // Các điều kiện filter khác theo specParams...
    }

}