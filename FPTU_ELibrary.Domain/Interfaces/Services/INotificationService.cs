using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using FPTU_ELibrary.Domain.Specifications.Params;

namespace FPTU_ELibrary.Domain.Interfaces.Services.Base;

public interface INotificationService<TDto> : IGenericService<Notification, TDto, int>
    where TDto : class
{
    Task<IServiceResult> CreateNotificationAsync(string createdByEmail, TDto dto, List<string>? recipients);
    Task<IServiceResult> GetAllPrivacyNotificationAsync(string email, ISpecification<Notification> spec);
    Task<IServiceResult> GetPrivacyNotificationAsync(int id, string email);
    Task<IServiceResult> GetAllCardHolderNotificationByUserIdAsync(Guid userId, int pageIndex, int pageSize);
}