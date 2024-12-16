using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;

namespace FPTU_ELibrary.Domain.Interfaces.Services.Base;

public interface INotificationService<TDto> : IGenericService<Notification, TDto, int>
    where TDto : class
{
    Task<IServiceResult> CreateNotification(TDto notification,string createBy,List<string>? recipients);
    Task<IServiceResult> GetTypes();

    Task<IServiceResult>GetAllWithSpecAsync(NotificationSpecParams specParams,
        string email,
        bool tracked = true
    );

}