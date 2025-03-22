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
using SkiaSharp;

namespace FPTU_ELibrary.Application.Services;

public class LibraryItemConditionService : GenericService<LibraryItemCondition, LibraryItemConditionDto, int>,
    ILibraryItemConditionService<LibraryItemConditionDto>
{
    public LibraryItemConditionService(
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
    }

    public override async Task<IServiceResult> CreateAsync(LibraryItemConditionDto dto)
    {
    	// Initiate service result
    	var serviceResult = new ServiceResult();

    	try
    	{
            // Determine current system lang 
            var lang = (SystemLanguage?) EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
    		// Validate inputs using the generic validator
    		var validationResult = await ValidatorExtensions.ValidateAsync(dto);
    		// Check for valid validations
    		if (validationResult != null && !validationResult.IsValid)
    		{
    			// Convert ValidationResult to ValidationProblemsDetails.Errors
    			var errors = validationResult.ToProblemDetails().Errors;
    			throw new UnprocessableEntityException("Invalid Validations", errors);
    		}

            // Custom errors
            var customErrs = new Dictionary<string, string[]>();
            // Check exist condition name
            var isEngExist = await _unitOfWork.Repository<LibraryItemCondition, int>()
                .AnyAsync(l => Equals(l.EnglishName, dto.EnglishName));
            if (isEngExist)
            {
                // Add error
                customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                    key: StringUtils.ToCamelCase(nameof(LibraryItemCondition.EnglishName)),
                    msg: isEng ? "English name has already existed" : "Tên tiếng Anh đã tồn tại");
            }
            var isVieExist = await _unitOfWork.Repository<LibraryItemCondition, int>()
                .AnyAsync(l => Equals(l.VietnameseName, dto.VietnameseName));
            if (isVieExist)
            {
                // Add error
                customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                    key: StringUtils.ToCamelCase(nameof(LibraryItemCondition.VietnameseName)),
                    msg: isEng ? "Vietnamese name has already existed" : "Tên tiếng Việt đã tồn tại");
            }
            
            // Check whether invoke any errors
            if(customErrs.Any()) throw new UnprocessableEntityException("Invalid Validations", customErrs);
            
    		// Process add new entity
    		await _unitOfWork.Repository<LibraryItemCondition, int>().AddAsync(_mapper.Map<LibraryItemCondition>(dto));
    		// Save to DB
    		if (await _unitOfWork.SaveChangesAsync() > 0)
    		{
                // Msg: Create successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001));
    		}
    		
            // Msg: Failed to create
            return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
    	}
    	catch (UnprocessableEntityException)
    	{
    		throw;
    	}
    	catch(Exception ex)
        {
    	    _logger.Error(ex.Message);
    	    throw;
        }
    	
    	return serviceResult;
    }
    
    public override async Task<IServiceResult> UpdateAsync(int id, LibraryItemConditionDto dto)
    {
        try
        {
            // Determine current system lang 
            var lang = (SystemLanguage?) EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(dto);
            // Check for valid validations
            if (validationResult != null && !validationResult.IsValid)
            {
                // Convert ValidationResult to ValidationProblemsDetails.Errors
                var errors = validationResult.ToProblemDetails().Errors;
                throw new UnprocessableEntityException("Invalid Validations", errors);
            }
            
            // Retrieve item condition by id
            var existingEntity = await _unitOfWork.Repository<LibraryItemCondition, int>().GetByIdAsync(id);
            if (existingEntity == null)
            {
                // Msg: Not Found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "item condition" : "tình trạng tài liệu"));
            }
            
            // Custom errors
            var customErrs = new Dictionary<string, string[]>();
            // Check exist condition name
            var isEngExist = await _unitOfWork.Repository<LibraryItemCondition, int>()
                .AnyAsync(l => Equals(l.EnglishName, dto.EnglishName));
            if (isEngExist)
            {
                // Add error
                customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                    key: StringUtils.ToCamelCase(nameof(LibraryItemCondition.EnglishName)),
                    msg: isEng ? "English name has already existed" : "Tên tiếng Anh đã tồn tại");
            }
            var isVieExist = await _unitOfWork.Repository<LibraryItemCondition, int>()
                .AnyAsync(l => Equals(l.VietnameseName, dto.VietnameseName));
            if (isVieExist)
            {
                // Add error
                customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                    key: StringUtils.ToCamelCase(nameof(LibraryItemCondition.VietnameseName)),
                    msg: isEng ? "Vietnamese name has already existed" : "Tên tiếng Việt đã tồn tại");
            }
            
            // Check whether invoke any errors
            if(customErrs.Any()) throw new UnprocessableEntityException("Invalid Validations", customErrs);
            
            // Check whether item condition has constraint with other data
            var hasConstraint = await _unitOfWork.Repository<LibraryItemCondition, int>().AnyAsync(l =>
                l.ConditionId == id && (
                    l.BorrowRecordDetails.Any() ||
                    l.BorrowRecordDetailsReturn.Any() || 
                    l.WarehouseTrackingDetails.Any() || 
                    l.LibraryItemConditionHistories.Any()));
            if (hasConstraint)
            {
                // Msg: Unable to edit because it is bound to other data
                return new ServiceResult(ResultCodeConst.SYS_Warning0008,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0008));
            }
            
            // Update fields
            existingEntity.EnglishName = dto.EnglishName;
            existingEntity.VietnameseName = dto.VietnameseName;
            // Process update entity
            await _unitOfWork.Repository<LibraryItemCondition, int>().UpdateAsync(existingEntity);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                // Msg: Update success
                return new ServiceResult(ResultCodeConst.SYS_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
            }
            
            // Msg: Failed to update
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
            throw new Exception("Error invoke when process create new library item condition");
        }
    }

    public override async Task<IServiceResult> DeleteAsync(int id)
    {
        try
        {
            // Determine current system lang 
            var lang = (SystemLanguage?) EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Retrieve item condition by id
            var existingEntity = await _unitOfWork.Repository<LibraryItemCondition, int>().GetByIdAsync(id);
            if (existingEntity == null)
            {
                // Msg: Not Found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "item condition" : "tình trạng tài liệu"));
            }
            
            // Check whether item condition has constraint with other data
            var hasConstraint = await _unitOfWork.Repository<LibraryItemCondition, int>().AnyAsync(l =>
                l.ConditionId == id && (
                    l.BorrowRecordDetails.Any() ||
                    l.BorrowRecordDetailsReturn.Any() || 
                    l.WarehouseTrackingDetails.Any() || 
                    l.LibraryItemConditionHistories.Any()));
            if (hasConstraint)
            {
                // Msg: Unable to edit because it is bound to other data
                return new ServiceResult(ResultCodeConst.SYS_Fail0007,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0007));
            }
            
            // Process delete by id
            await _unitOfWork.Repository<LibraryItemCondition, int>().DeleteAsync(id);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                // Msg: Delete successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004));
            }
            
            // Msg: Failed to delete
            return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process delete library item condition");
        }
    }

    public async Task<IServiceResult> GetAllForStockInWarehouseAsync(TrackingType trackingType)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<LibraryItemCondition>();
            
            // Determine tracking type
            switch (trackingType)
            {
                case TrackingType.StockIn or TrackingType.Transfer:
                    // Add filter lost condition
                    baseSpec.AddFilter(l => l.EnglishName != nameof(LibraryItemConditionStatus.Lost));
                    break;
                case TrackingType.StockOut:
                    break;
            }
            
            // Retrieve all with spec
            var entities = await _unitOfWork.Repository<LibraryItemCondition, int>()
                .GetAllWithSpecAsync(baseSpec);
            if (entities.Any())
            {
                // Get successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                    _mapper.Map<List<LibraryItemConditionDto>>(entities));
            }
            
            // Data not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                _mapper.Map<List<LibraryItemConditionDto>>(entities));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get all condition for stock in warehouse");
        }
    }
}