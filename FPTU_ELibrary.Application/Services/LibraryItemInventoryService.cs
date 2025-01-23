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
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class LibraryItemInventoryService : 
    GenericService<LibraryItemInventory, LibraryItemInventoryDto, int>, 
    ILibraryItemInventoryService<LibraryItemInventoryDto>
{
    public LibraryItemInventoryService(
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
    }

    public async Task<IServiceResult> UpdateWithoutSaveChangesAsync(LibraryItemInventoryDto dto)
    {
        // Initiate service result
		var serviceResult = new ServiceResult();

		try
		{
			// Determine current system lang 
			var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
				LanguageContext.CurrentLanguage);
			var isEng = lang == SystemLanguage.English;
			
			// Validate inputs using the generic validator
			var validationResult = await ValidatorExtensions.ValidateAsync(dto);
			// Check for valid validations
			if (validationResult != null && !validationResult.IsValid)
			{
				// Convert ValidationResult to ValidationProblemsDetails.Errors
				var errors = validationResult.ToProblemDetails().Errors;
				throw new UnprocessableEntityException("Invalid validations", errors);
			}

			// Retrieve the entity
			var existingEntity = await _unitOfWork.Repository<LibraryItemInventory, int>()
				.GetByIdAsync(dto.LibraryItemId);
			if (existingEntity == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
					StringUtils.Format(errMsg, isEng ? "library item instance" : "tài liệu"));
			}

			// Process add update entity
			existingEntity.TotalUnits = dto.TotalUnits;
			existingEntity.AvailableUnits = dto.AvailableUnits;
			existingEntity.BorrowedUnits = dto.BorrowedUnits;
			existingEntity.RequestUnits = dto.RequestUnits;
			existingEntity.ReservedUnits = dto.ReservedUnits;

			// Progress update when all require passed
			await _unitOfWork.Repository<LibraryItemInventory, int>().UpdateAsync(existingEntity);

			// Mark as update success
			serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
			serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
			serviceResult.Data = true;
		}
		catch (UnprocessableEntityException)
		{
			throw;
		}
		catch (Exception ex)
		{
			_logger.Error(ex.Message);
			throw;
		}

		return serviceResult;
    }
}