using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Application.Services.IServices;

public interface IBorrowRequestResourceService<TDto> : IGenericService<BorrowRequestResource, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetAllByRequestIdToConfirmCreateTransactionAsync(string email, int borrowRequestId);
    Task<IServiceResult> AddRangeResourceTransactionsWithoutSaveChangesAsync(List<TDto> borrowResources);
}