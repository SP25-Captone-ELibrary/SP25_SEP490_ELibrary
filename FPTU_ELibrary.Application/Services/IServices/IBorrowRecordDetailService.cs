using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;

namespace FPTU_ELibrary.Application.Services.IServices;

public interface IBorrowRecordDetailService<TDto> : IGenericService<BorrowRecordDetail, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetAllWithSpecFromDashboardAsync(ISpecification<BorrowRecordDetail> spec);
}