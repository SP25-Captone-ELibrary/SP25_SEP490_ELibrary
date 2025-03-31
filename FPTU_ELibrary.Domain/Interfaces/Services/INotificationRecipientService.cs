using FPTU_ELibrary.Domain.Entities;

namespace FPTU_ELibrary.Domain.Interfaces.Services.Base;

public interface INotificationRecipientService<TDto> :IGenericService<NotificationRecipient,TDto,int> 
    where TDto: class
{
    Task<IServiceResult> GetNumberOfUnreadNotificationsAsync(string email);
    Task<IServiceResult> UpdateRangeReadStatusAsync(string email, List<int> notificationIds);
    Task<IServiceResult> MarkAsReadAllAsync(string email);
}