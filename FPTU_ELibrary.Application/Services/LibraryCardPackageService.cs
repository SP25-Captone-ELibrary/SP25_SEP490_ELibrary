using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
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

public class LibraryCardPackageService : GenericService<LibraryCardPackage, LibraryCardPackageDto, int>,
    ILibraryCardPackageService<LibraryCardPackageDto>
{
    public LibraryCardPackageService(
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
    }

    public override async Task<IServiceResult> UpdateAsync(int id, LibraryCardPackageDto dto)
    {
		// Initiate service result
		var serviceResult = new ServiceResult();

		try
		{
			// Determine current lang context
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
			var existingEntity = await _unitOfWork.Repository<LibraryCardPackage, int>().GetByIdAsync(id);
			if (existingEntity == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
					StringUtils.Format(errMsg, isEng ? "package" : "gói"));
			}

			existingEntity.PackageName = dto.PackageName;
			existingEntity.Price = dto.Price;
			existingEntity.DurationInMonths = dto.DurationInMonths;
			existingEntity.Description = dto.Description;

			// Check if there are any differences between the original and the updated entity
			if (!_unitOfWork.Repository<LibraryCardPackage, int>().HasChanges(existingEntity))
			{
				serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
				serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
				serviceResult.Data = true;
				return serviceResult;
			}

			// Progress update when all require passed
			await _unitOfWork.Repository<LibraryCardPackage, int>().UpdateAsync(existingEntity);

			// Save changes to DB
			var rowsAffected = await _unitOfWork.SaveChangesAsync();
			if (rowsAffected == 0)
			{
				serviceResult.ResultCode = ResultCodeConst.SYS_Fail0003;
				serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003);
				serviceResult.Data = false;
			}

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