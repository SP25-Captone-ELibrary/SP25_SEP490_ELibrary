using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.WarehouseTrackings;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class SupplementRequestDetailService : 
    GenericService<SupplementRequestDetail, SupplementRequestDetailDto, int>,
    ISupplementRequestDetailService<SupplementRequestDetailDto>
{
    private readonly IWarehouseTrackingService<WarehouseTrackingDto> _trackingSvc;

    public SupplementRequestDetailService(
        IWarehouseTrackingService<WarehouseTrackingDto> trackingSvc,
        ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _trackingSvc = trackingSvc;
    }

    public async Task<IServiceResult> GetAllByTrackingIdAsync(int trackingId, ISpecification<SupplementRequestDetail> spec)
    {
        try
        {
            // Determine current system lang 
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
			   
            // Check existing tracking id 
            var trackingDto = (await _trackingSvc.GetByIdAndIncludeInventoryAsync(trackingId)).Data as WarehouseTrackingDto;
            if (trackingDto == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "warehouse tracking" : "thông tin theo dõi kho"));
            }
            
            // Try to parse specification to SupplementRequestDetailSpecification
            var detailSpec = spec as SupplementRequestDetailSpecification;
            // Check if specification is null
            if (detailSpec == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }
            
            // Add tracking filtering 
            detailSpec.AddFilter(w => w.TrackingId == trackingId);
		    
            // Count total library items
            var totalDetailWithSpec = await _unitOfWork.Repository<SupplementRequestDetail, int>().CountAsync(detailSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalDetailWithSpec / detailSpec.PageSize);

            // Set pagination to specification after count total warehouse tracking detail
            if (detailSpec.PageIndex > totalPage
                || detailSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
                detailSpec.PageIndex = 1; // Set default to first page
            }

            // Apply pagination
            detailSpec.ApplyPaging(
                skip: detailSpec.PageSize * (detailSpec.PageIndex - 1),
                take: detailSpec.PageSize);
            
            // Try to retrieve all data by spec
            var entities = await _unitOfWork.Repository<SupplementRequestDetail, int>()
                .GetAllWithSpecAsync(detailSpec);
            if (entities.Any())
            {
                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<SupplementRequestDetailDto>(
                    _mapper.Map<List<SupplementRequestDetailDto>>(entities),
                    detailSpec.PageIndex, detailSpec.PageSize, totalPage, totalDetailWithSpec);
                
                // Get data successfully
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Success0002,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                    data: paginationResultDto);
            }
            
            // Data not found or empty
            return new ServiceResult(
                resultCode: ResultCodeConst.SYS_Success0002,
                message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                data: _mapper.Map<List<SupplementRequestDetailDto>>(entities));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process supplement request detail service");
        }
    }
}