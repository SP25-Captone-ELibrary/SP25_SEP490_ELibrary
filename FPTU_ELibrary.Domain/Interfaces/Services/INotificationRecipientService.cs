using FPTU_ELibrary.Domain.Entities;

namespace FPTU_ELibrary.Domain.Interfaces.Services.Base;

public interface INotificationRecipientService<TDto> :IGenericService<NotificationRecipient,TDto,Guid> 
    where TDto: class
{
    Task<IServiceResult> CreatePrivateNotification(TDto notification);
    Task<IServiceResult> GetNumberOfUnreadNotifications(string email);
    Task<IServiceResult> UpdateReadStatus(string email);
    
}