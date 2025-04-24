using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class LibraryClosureDayService : GenericService<LibraryClosureDay, LibraryClosureDayDto, int>,
    ILibraryClosureDayService<LibraryClosureDayDto>
{
    public LibraryClosureDayService(
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
    }

    public override async Task<IServiceResult> CreateAsync(LibraryClosureDayDto dto)
    {
        try
        {
            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(dto);
            // Check for valid validations
            if (validationResult != null && !validationResult.IsValid)
            {
                // Convert ValidationResult to ValidationProblemsDetails.Errors
                var errors = validationResult.ToProblemDetails().Errors;
                throw new UnprocessableEntityException("Invalid Validations", errors);
            }
            
            // Check exist specific day, month, year
            var isTimeExist = await _unitOfWork.Repository<LibraryClosureDay, int>().AnyAsync(
                c => c.Day == dto.Day && c.Month == dto.Month);
            if (isTimeExist)
            {
                // Msg: The time already exists in the library's closure days list
                return new ServiceResult(ResultCodeConst.LibraryClosureDay_Warning0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryClosureDay_Warning0001));
            }
            
            // Validate date range
            if (!IsValidDate(dto.Year, dto.Month, dto.Day))
            {
                // Msg: Closure datetime is invalid
                return new ServiceResult(ResultCodeConst.LibraryClosureDay_Warning0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryClosureDay_Warning0002));
            }
            
            // Process create new closure day
            await _unitOfWork.Repository<LibraryClosureDay, int>().AddAsync(_mapper.Map<LibraryClosureDay>(dto));
            // Save to DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                // Msg: Create success
                return new ServiceResult(ResultCodeConst.SYS_Success0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001));
            }
            
            // Msg: Create fail
            return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process create library closure day");
        }
    }
    
    public override async Task<IServiceResult> UpdateAsync(int id, LibraryClosureDayDto dto)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Check exist closure day
            var existingEntity = await _unitOfWork.Repository<LibraryClosureDay, int>().GetByIdAsync(id);
            if (existingEntity == null)
            {
                // Msg: Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "closure day" : "ngày nghỉ"));
            }
            
            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(dto);
            // Check for valid validations
            if (validationResult != null && !validationResult.IsValid)
            {
                // Convert ValidationResult to ValidationProblemsDetails.Errors
                var errors = validationResult.ToProblemDetails().Errors;
                throw new UnprocessableEntityException("Invalid Validations", errors);
            }
            
            // Check exist specific day, month, year
            var isTimeExist = await _unitOfWork.Repository<LibraryClosureDay, int>().AnyAsync(c => 
                    c.ClosureDayId != id &&
                    c.Day == dto.Day && c.Month == dto.Month);
            if (isTimeExist)
            {
                // Msg: The time already exists in the library's closure days list
                return new ServiceResult(ResultCodeConst.LibraryClosureDay_Warning0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryClosureDay_Warning0001));
            }
            
            // Validate date range
            if (!IsValidDate(dto.Year, dto.Month, dto.Day))
            {
                // Msg: Closure datetime is invalid
                return new ServiceResult(ResultCodeConst.LibraryClosureDay_Warning0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryClosureDay_Warning0002));
            }
            
            // Process update
            existingEntity.Day = dto.Day;
            existingEntity.Month = dto.Month;
            existingEntity.Year = dto.Year;
            existingEntity.VieDescription = dto.VieDescription;
            existingEntity.EngDescription = dto.EngDescription;
            
            // Process create new closure day
            await _unitOfWork.Repository<LibraryClosureDay, int>().UpdateAsync(existingEntity);
            // Save to DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                // Msg: Update success
                return new ServiceResult(ResultCodeConst.SYS_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
            }
            
            // Msg: Update fail
            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process create library closure day");
        }
    }

    public async Task<IServiceResult> DeleteRangeAsync(int[] ids)
    {
        try
        {
            // Extract all existing entities that match ids
            var baseSpec = new BaseSpecification<LibraryClosureDay>(c => ids.Contains(c.ClosureDayId));
            var entities = (await _unitOfWork.Repository<LibraryClosureDay, int>().GetAllWithSpecAsync(baseSpec)).ToList();
            if (!entities.Any())
            {
                // Mark as delete failed
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }
            
            // Process delete
            await _unitOfWork.Repository<LibraryClosureDay, int>().DeleteRangeAsync(entities.Select(e => e.ClosureDayId).ToArray());
            // Save to DB
            if (await _unitOfWork.SaveChangesAsync() > 0)
            {
                var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0008);
                return new ServiceResult(ResultCodeConst.SYS_Success0008,
                    StringUtils.Format(msg, entities.Count.ToString()), true);
            }
    
            // Fail to delete
            return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process delete range of closure days");
        }
    }
    
    private bool IsValidDate(int? year, int month, int day)
    {
        // Initialize year
        var validYear = year ?? DateTime.Now.Year;

        if (month < 1 || month > 12)
            return false;

        // handle leap years automatically
        var daysInMonth = DateTime.DaysInMonth(validYear, month);
        return day >= 1 && day <= daysInMonth;
    }
}