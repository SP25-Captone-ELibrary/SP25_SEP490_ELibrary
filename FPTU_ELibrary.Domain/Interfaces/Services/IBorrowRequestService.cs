using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IBorrowRequestService<TDto> : IGenericService<BorrowRequest, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetByIdAsync(string email, int id);
    Task<IServiceResult> GetAllByEmailAsync(string email, ISpecification<BorrowRequest> spec);
    Task<IServiceResult> CreateAsync(string email, TDto dto);
    Task<IServiceResult> CancelAsync(string email, int id, string? cancellationReason);
    Task<IServiceResult> UpdateStatusWithoutSaveChangesAsync(int id, BorrowRequestStatus status);
    Task<IServiceResult> CheckExistBarcodeInRequestAsync(int id, string barcode);
}