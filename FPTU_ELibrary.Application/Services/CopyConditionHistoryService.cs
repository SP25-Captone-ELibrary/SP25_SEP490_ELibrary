using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.BookEditions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class CopyConditionHistoryService : GenericService<CopyConditionHistory, CopyConditionHistoryDto, int>,
    ICopyConditionHistoryService<CopyConditionHistoryDto>
{
    public CopyConditionHistoryService(
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
    }

    public async Task<IServiceResult> DeleteWithoutSaveChangesAsync(int id)
    {
        try
        {
            // Retrieve copy history match 
            var copyConditionHisEntity = await _unitOfWork.Repository<CopyConditionHistory, int>()
                .GetByIdAsync(id);
            if (copyConditionHisEntity == null && id > 0) // Not exist any entity match
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }
            
            // Progress delete 
            await _unitOfWork.Repository<CopyConditionHistory, int>().DeleteAsync(id);
            
            // Mark as delete without save success
            return new ServiceResult(ResultCodeConst.SYS_Success0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process delete range copy condition history");
        }
    }
}