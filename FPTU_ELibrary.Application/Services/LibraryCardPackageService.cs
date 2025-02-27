using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.Payments;
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
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class LibraryCardPackageService : GenericService<LibraryCardPackage, LibraryCardPackageDto, int>,
	ILibraryCardPackageService<LibraryCardPackageDto>
{
	public LibraryCardPackageService(
		ISystemMessageService msgService,
		IUserService<UserDto> userService,
		IUnitOfWork unitOfWork,
		ITransactionService<TransactionDto> transactionService,
		IMapper mapper,
		ILogger logger) : base(msgService, unitOfWork, mapper, logger)
	{
	}

	public override async Task<IServiceResult> CreateAsync(LibraryCardPackageDto dto)
	{
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
				throw new UnprocessableEntityException("Invalid Validations", errors);
			}

			// Initialize custom errors
			var customErrors = new Dictionary<string, string[]>();
			// Check exist library card package name
			var isPackageNameExist = await _unitOfWork.Repository<LibraryCardPackage, int>()
				.AnyAsync(p => Equals(p.PackageName.ToLower(), dto.PackageName.ToLower()));
			if (isPackageNameExist)
			{
				// Add error
				customErrors = DictionaryUtils.AddOrUpdate(customErrors,
					key: StringUtils.ToCamelCase(nameof(LibraryCardPackage.PackageName)),
					msg: isEng ? "Package name has already existed" : "Tên gói đã tồn tại");
			}

			// Check exist duration in months
			var isDurationExist = await _unitOfWork.Repository<LibraryCardPackage, int>()
				.AnyAsync(p => Equals(p.DurationInMonths, dto.DurationInMonths));
			if (isDurationExist)
			{
				// Add error
				customErrors = DictionaryUtils.AddOrUpdate(customErrors,
					key: StringUtils.ToCamelCase(nameof(LibraryCardPackage.DurationInMonths)),
					msg: isEng ? "Duration in months has already existed" : "Thời hạn gói theo tháng đã tồn tại");
			}
			
			// Check exist price 
			var isExistPrice = await _unitOfWork.Repository<LibraryCardPackage, int>()
				.AnyAsync(p => Equals(p.Price, dto.Price));
			if (isExistPrice)
			{
				// Add error
				customErrors = DictionaryUtils.AddOrUpdate(customErrors,
					key: StringUtils.ToCamelCase(nameof(LibraryCardPackage.Price)),
					msg: isEng ? "Price has already existed" : "Giá gói đã tồn tại");
			}

			// Check if any errors invoke 
			if (customErrors.Any()) throw new UnprocessableEntityException("Invalid Validations", customErrors);
			
			// Process add package
			await _unitOfWork.Repository<LibraryCardPackage, int>().AddAsync(_mapper.Map<LibraryCardPackage>(dto));
			// Save DB
			var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
			if (isSaved)
			{
				// Create successfully
				return new ServiceResult(ResultCodeConst.SYS_Success0001,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001));
			}
			
			// Fail to create
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
			throw new Exception("Error invoke when process create library card package");
		}
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
					StringUtils.Format(errMsg, isEng ? "package" : "gói thẻ thư viện"));
			}

			// Initialize custom errors
			var customErrors = new Dictionary<string, string[]>();
			// Check exist library card package name
			var isPackageNameExist = await _unitOfWork.Repository<LibraryCardPackage, int>()
				.AnyAsync(p => Equals(p.PackageName.ToLower(), dto.PackageName.ToLower()) &&
				          	p.LibraryCardPackageId != existingEntity.LibraryCardPackageId);
			if (isPackageNameExist)
			{
				// Add error
				customErrors = DictionaryUtils.AddOrUpdate(customErrors,
					key: StringUtils.ToCamelCase(nameof(LibraryCardPackage.PackageName)),
					msg: isEng ? "Package name has already existed" : "Tên gói đã tồn tại");
			}

			// Check exist duration in months
			var isDurationExist = await _unitOfWork.Repository<LibraryCardPackage, int>()
				.AnyAsync(p => Equals(p.DurationInMonths, dto.DurationInMonths) &&
				          	p.LibraryCardPackageId != existingEntity.LibraryCardPackageId);
			if (isDurationExist)
			{
				// Add error
				customErrors = DictionaryUtils.AddOrUpdate(customErrors,
					key: StringUtils.ToCamelCase(nameof(LibraryCardPackage.DurationInMonths)),
					msg: isEng ? "Duration in months has already existed" : "Thời hạn gói theo tháng đã tồn tại");
			}
			
			// Check exist price 
			var isExistPrice = await _unitOfWork.Repository<LibraryCardPackage, int>() 
				.AnyAsync(p => Equals(p.Price, dto.Price) &&
				          	p.LibraryCardPackageId != existingEntity.LibraryCardPackageId);
			if (isExistPrice)
			{
				// Add error
				customErrors = DictionaryUtils.AddOrUpdate(customErrors,
					key: StringUtils.ToCamelCase(nameof(LibraryCardPackage.Price)),
					msg: isEng ? "Price has already existed" : "Giá gói đã tồn tại");
			}

			// Check if any errors invoke 
			if (customErrors.Any()) throw new UnprocessableEntityException("Invalid Validations", customErrors);
			
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

	public override async Task<IServiceResult> DeleteAsync(int id)
	{
		try
		{
			// Determine current system lang
			var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
				LanguageContext.CurrentLanguage);
			var isEng = lang == SystemLanguage.English;
			
			// Check exist entity
			var existingEntity = await _unitOfWork.Repository<LibraryCardPackage, int>().GetByIdAsync(id);
			if (existingEntity == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMsg, isEng ? "package" : "gói thẻ thư viện"));
			}
			
			// Process delete 
			await _unitOfWork.Repository<LibraryCardPackage, int>().DeleteAsync(id);
			// Save DB
			var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
			if (isSaved)
			{
				// Delete success
				return new ServiceResult(ResultCodeConst.SYS_Success0004,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004));
			}
			
			// Fail to delete 
			return new ServiceResult(ResultCodeConst.SYS_Fail0004,
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
		}
		catch (DbUpdateException ex)
		{
			if (ex.InnerException is SqlException sqlEx)
			{
				switch (sqlEx.Number)
				{
					case 547: // Foreign key constraint violation
						return new ServiceResult(ResultCodeConst.SYS_Fail0007,
							await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0007));
				}
			}
				
			// Throw if other issues
			throw;
		}
		catch (Exception ex)
		{
			_logger.Error(ex.Message);
			throw new Exception("Error invoke when process delete library card package");
		}
	}

	#region Archived Code
	// public async Task<IServiceResult> CreateTransactionForLibraryCardPackage(string email, int id)
	// {
	// 	// Determine current system lang
	// 	var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
	// 		LanguageContext.CurrentLanguage);
	// 	var isEng = lang == SystemLanguage.English;
	// 	
	// 	// Get User By email
	// 	var userBaseSpec = new BaseSpecification<User>(u => u.Email == email);
	// 	var user = await _userService.GetWithSpecAsync(userBaseSpec);
	// 	if (user.Data is null)
	// 	{
	// 		var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
	// 		return new ServiceResult(ResultCodeConst.SYS_Warning0002,
	// 			StringUtils.Format(errMsg, isEng ? "user" : "bạn đọc"));
	// 	}
	// 	
	// 	//get package by id
	// 	var package = await _unitOfWork.Repository<LibraryCardPackage, int>().GetByIdAsync(id);
	// 	if (package is null)
	// 	{
	// 		var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
	// 		return new ServiceResult(ResultCodeConst.SYS_Warning0002,
	// 			StringUtils.Format(errMsg, isEng ? "package" : "gói thẻ thư viện"));
	// 	}
	// 	
	// 	TransactionDto response = new TransactionDto();
	// 	response.TransactionCode = Guid.NewGuid().ToString();
	// 	// fine caused by damaged or lost would base on the amount of item
	// 	response.Amount = package.Price;
	// 	response.UserId = (user.Data as UserDto)!.UserId;
	// 	response.TransactionStatus = TransactionStatus.Pending;
	// 	response.LibraryCardPackageId = package.LibraryCardPackageId;
	// 	response.CreatedAt = DateTime.Now;
	// 	// response.PaymentMethodId = 1;
	// 	var transactionEntity = _mapper.Map<Transaction>(response);
	// 	var result = await _transactionService.CreateAsync(transactionEntity);
	// 	if(result.Data is null) return result;
	//
	// 	return new ServiceResult(ResultCodeConst.SYS_Success0001,
	// 		await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001));
	//
	// }
	
	// public async Task<IServiceResult> CreateTransactionForLibraryCardPackage(string email, int id)
	// {
	// 	// Determine current system lang
	// 	var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
	// 		LanguageContext.CurrentLanguage);
	// 	var isEng = lang == SystemLanguage.English;
	// 	
	// 	// Get User By email
	// 	var userBaseSpec = new BaseSpecification<User>(u => u.Email == email);
	// 	var user = await _userService.Value.GetWithSpecAsync(userBaseSpec);
	// 	if (user.Data is null)
	// 	{
	// 		var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
	// 		return new ServiceResult(ResultCodeConst.SYS_Warning0002,
	// 			StringUtils.Format(errMsg, isEng ? "user" : "bạn đọc"));
	// 	}
	// 	
	// 	//get package by id
	// 	var package = await _unitOfWork.Repository<LibraryCardPackage, int>().GetByIdAsync(id);
	// 	if (package is null)
	// 	{
	// 		var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
	// 		return new ServiceResult(ResultCodeConst.SYS_Warning0002,
	// 			StringUtils.Format(errMsg, isEng ? "package" : "gói thẻ thư viện"));
	// 	}
	// 	
	// 	TransactionDto response = new TransactionDto();
	// 	response.TransactionCode = Guid.NewGuid().ToString();
	// 	// fine caused by damaged or lost would base on the amount of item
	// 	response.Amount = package.Price;
	// 	response.UserId = (user.Data as UserDto)!.UserId;
	// 	response.TransactionStatus = TransactionStatus.Pending;
	// 	response.LibraryCardPackageId = package.LibraryCardPackageId;
	// 	response.CreatedAt = DateTime.Now;
	// 	// response.PaymentMethodId = 1;
	// 	var transactionEntity = _mapper.Map<Transaction>(response);
	// 	var result = await _transactionService.Value.CreateAsync(transactionEntity);
	// 	if(result.Data is null) return result;
	//
	// 	return new ServiceResult(ResultCodeConst.SYS_Success0001,
	// 		await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001));
	//
	// }
	#endregion
}