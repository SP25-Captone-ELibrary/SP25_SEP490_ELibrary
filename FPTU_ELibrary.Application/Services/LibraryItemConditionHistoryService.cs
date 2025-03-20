using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using iTextSharp.text.pdf;
using MapsterMapper;
using Microsoft.Extensions.Azure;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class LibraryItemConditionHistoryService : GenericService<LibraryItemConditionHistory, LibraryItemConditionHistoryDto, int>,
    ILibraryItemConditionHistoryService<LibraryItemConditionHistoryDto>
{
    private readonly Lazy<ILibraryItemConditionService<LibraryItemConditionDto>> _conditionSvc;
    private readonly Lazy<ILibraryItemInstanceService<LibraryItemInstanceDto>> _itemInstanceSvc;

    public LibraryItemConditionHistoryService(
        Lazy<ILibraryItemConditionService<LibraryItemConditionDto>> conditionSvc,
        Lazy<ILibraryItemInstanceService<LibraryItemInstanceDto>> itemInstanceSvc,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _conditionSvc = conditionSvc;
        _itemInstanceSvc = itemInstanceSvc;
    }

    public async Task<IServiceResult> UpdateByConditionIdWithoutSaveChangesAsync(
        int libraryItemInstanceId,
        int conditionId)
    {
        try
        {
            // Retrieve library item instance id
            var itemInstanceDto = (await _itemInstanceSvc.Value.GetByIdAsync(libraryItemInstanceId)).Data as LibraryItemInstanceDto;
            if (itemInstanceDto == null)
            {
                // Mark as failed to update
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
            }
            
            // Retrieve condition by id
            var toUpdateConditionDto = (await _conditionSvc.Value.GetByIdAsync(conditionId)).Data as LibraryItemConditionDto;
            if (toUpdateConditionDto == null)
            {
                // Mark as failed to update
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
            }
            
            // Retrieve latest condition history
            var baseSpec = new BaseSpecification<LibraryItemConditionHistory>(l => 
                l.LibraryItemInstanceId == itemInstanceDto.LibraryItemInstanceId);
            // Order by descending created at
            baseSpec.AddOrderBy(l => l.CreatedAt);
            // Retrieve with spec
            var latestConditionHis = await _unitOfWork.Repository<LibraryItemConditionHistory, int>()
                .GetWithSpecAsync(baseSpec);
            if (latestConditionHis != null)
            {
                // Check whether condition is not change
                if (Equals(latestConditionHis.ConditionId, toUpdateConditionDto.ConditionId))
                {
                    // Mark as update successfully
                    return new ServiceResult(ResultCodeConst.SYS_Success0003,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
                }
            }
            
            // Process add condition
            await _unitOfWork.Repository<LibraryItemConditionHistory, int>()
                .AddAsync(new ()
                {
                    ConditionId = toUpdateConditionDto.ConditionId,
                    LibraryItemInstanceId = libraryItemInstanceId
                });
            
            // Mark as update successfully
            return new ServiceResult(ResultCodeConst.SYS_Success0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update condition by condition id");
        }
    }
    
    public async Task<IServiceResult> UpdateRangeConditionWithoutSaveChangesAsync(
        List<int> libraryItemInstanceIds, LibraryItemConditionStatus status)
    {
        try
        {
            // Retrieve condition by status
            var conditionSpec = new BaseSpecification<LibraryItemCondition>(
                l => l.EnglishName == status.ToString());
            var toUpdateConditionDto = (await _conditionSvc.Value.GetWithSpecAsync(conditionSpec)).Data as LibraryItemConditionDto;
            if (toUpdateConditionDto == null)
            {
                // Mark as failed to update
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
            }
            
            // Iterate each item instance to retrieve its condition histories
            foreach (var instanceId in libraryItemInstanceIds)
            {
                // Retrieve item instance by id 
                var itemInstanceDto = (await _itemInstanceSvc.Value.GetByIdAsync(instanceId)).Data as LibraryItemInstanceDto;
                if (itemInstanceDto == null) continue; // Skip to next instance if not found
                
                // Retrieve latest condition history
                var baseSpec = new BaseSpecification<LibraryItemConditionHistory>(l => 
                    l.LibraryItemInstanceId == itemInstanceDto.LibraryItemInstanceId);
                // Order by descending created at
                baseSpec.AddOrderBy(l => l.CreatedAt);
                // Retrieve with spec
                var latestConditionHis = await _unitOfWork.Repository<LibraryItemConditionHistory, int>()
                    .GetWithSpecAsync(baseSpec);
                if (latestConditionHis != null)
                {
                    // Check whether condition is not change
                    if (Equals(latestConditionHis.ConditionId, toUpdateConditionDto.ConditionId))
                        continue; // Skip to next instance
                }
                
                // Process add condition
                await _unitOfWork.Repository<LibraryItemConditionHistory, int>()
                    .AddAsync(new ()
                    {
                        ConditionId = toUpdateConditionDto.ConditionId,
                        LibraryItemInstanceId = instanceId
                    });
            }
            
            // Mark as update successfully
            return new ServiceResult(ResultCodeConst.SYS_Success0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, ex.Message);
            throw new Exception("Error invoke when process update range item instance condition history");
        }
    }
    
    public async Task<IServiceResult> DeleteWithoutSaveChangesAsync(int id)
    {
        try
        {
            // Retrieve copy history match 
            var copyConditionHisEntity = await _unitOfWork.Repository<LibraryItemConditionHistory, int>()
                .GetByIdAsync(id);
            if (copyConditionHisEntity == null && id > 0) // Not exist any entity match
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }
            
            // Progress delete 
            await _unitOfWork.Repository<LibraryItemConditionHistory, int>().DeleteAsync(id);
            
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