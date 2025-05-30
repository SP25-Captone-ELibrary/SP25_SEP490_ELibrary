using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IDigitalBorrowService<TDto> : IGenericService<DigitalBorrow, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetAllWithSpecFromDashboardAsync(ISpecification<DigitalBorrow> spec);
    Task<IServiceResult> GetByIdAsync(int id, string? email = null, Guid? userId = null, bool isCallFromManagement = false);
    Task<IServiceResult> ConfirmDigitalBorrowAsync(string email, string transactionToken);
    Task<IServiceResult> ConfirmDigitalExtensionAsync(string email, string transactionToken);
}