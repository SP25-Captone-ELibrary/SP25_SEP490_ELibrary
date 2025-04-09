using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class BorrowRecordDetailService : GenericService<BorrowRecordDetail, BorrowRecordDetailDto, int>,
    IBorrowRecordDetailService<BorrowRecordDetailDto>
{
    public BorrowRecordDetailService(
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
    }

    public async Task<IServiceResult> GetAllWithSpecFromDashboardAsync(ISpecification<BorrowRecordDetail> spec)
    {
        return await base.GetAllWithSpecAsync(spec);
    }
}