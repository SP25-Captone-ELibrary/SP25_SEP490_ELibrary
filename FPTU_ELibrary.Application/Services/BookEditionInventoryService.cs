using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.BookEditions;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class BookEditionInventoryService : 
    GenericService<BookEditionInventory, BookEditionInventoryDto, int>, IBookEditionInventoryService<BookEditionInventoryDto>
{
    public BookEditionInventoryService(
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
    }

    public async Task<IServiceResult> UpdateWithoutSaveChangesAsync(BookEditionInventoryDto dto)
    {
        // Initiate service result
		var serviceResult = new ServiceResult();

		try
		{
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
			var existingEntity = await _unitOfWork.Repository<BookEditionInventory, int>()
				.GetByIdAsync(dto.BookEditionId);
			if (existingEntity == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
					StringUtils.Format(errMsg, typeof(BookEditionInventory).ToString().ToLower()));
			}

			// Process add update entity
			existingEntity.TotalCopies = dto.TotalCopies;
			existingEntity.AvailableCopies = dto.AvailableCopies;
			existingEntity.RequestCopies = dto.RequestCopies;
			existingEntity.ReservedCopies = dto.ReservedCopies;

			// Progress update when all require passed
			await _unitOfWork.Repository<BookEditionInventory, int>().UpdateAsync(existingEntity);

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