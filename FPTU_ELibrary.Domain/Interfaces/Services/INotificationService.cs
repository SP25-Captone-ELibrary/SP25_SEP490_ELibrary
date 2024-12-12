using FPTU_ELibrary.Domain.Entities;

namespace FPTU_ELibrary.Domain.Interfaces.Services.Base;

public interface INotificationService<TDto> : IGenericService<Notification, TDto, Guid>
    where TDto : class
{
    Task<IServiceResult> CreateNotification(TDto notification,string createBy,List<string>? recipients);
    Task<IServiceResult> GetTypes();
    
}