using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using MapsterMapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml.Drawing.Chart;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class LibraryCardService : GenericService<LibraryCard, LibraryCardDto, Guid>,
    ILibraryCardService<LibraryCardDto>
{
    private readonly ICloudinaryService _cloudSvc;
    private readonly IUserService<UserDto> _userSvc;
    private readonly ITransactionService<TransactionDto> _tranSvc;
    private readonly WebTokenSettings _webTokenSettings;
    private readonly TokenValidationParameters _tokenValidationParams;

    public LibraryCardService(
        IUserService<UserDto> userSvc,
        ITransactionService<TransactionDto> tranSvc,
        IOptionsMonitor<WebTokenSettings> monitor,
        ICloudinaryService cloudSvc,
        TokenValidationParameters tokenValidationParams,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _userSvc = userSvc;
        _tranSvc = tranSvc;
        _cloudSvc = cloudSvc;
        _webTokenSettings = monitor.CurrentValue;
        _tokenValidationParams = tokenValidationParams;
    }

    public override async Task<IServiceResult> GetAllWithSpecAsync(ISpecification<LibraryCard> specification, bool tracked = true)
    {
        try
        {
            // Try to parse specification to LibraryCardSpecification
            var cardSpec = specification as LibraryCardSpecification;
            // Check if specification is null
            if (cardSpec == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }

            // Count total library cards
            var totalCardItemWithSpec = await _unitOfWork.Repository<LibraryCard, Guid>().CountAsync(cardSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalCardItemWithSpec / cardSpec.PageSize);

            // Set pagination to specification after count total library card
            if (cardSpec.PageIndex > totalPage
                || cardSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
                cardSpec.PageIndex = 1; // Set default to first page
            }

            // Apply pagination
            cardSpec.ApplyPaging(skip: cardSpec.PageSize * (cardSpec.PageIndex - 1), take: cardSpec.PageSize);

            var entities = await _unitOfWork.Repository<LibraryCard, Guid>().GetAllWithSpecAsync(cardSpec); 
            if (entities.Any()) // Exist data
            {
                // Convert to dto collection
                var cardDtos = _mapper.Map<List<LibraryCardDto>>(entities);

                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<LibraryCardDto>(cardDtos,
                    cardSpec.PageIndex, cardSpec.PageSize, totalPage, totalCardItemWithSpec);

                // Response with pagination 
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }

            // Not found any data
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get all library card");
        }
    }

    public override async Task<IServiceResult> GetByIdAsync(Guid id)
    {
        try
        {
            // Determine current system lang 
            var lang = (SystemLanguage?) EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Retrieve library card by id 
            var existingEntity = await _unitOfWork.Repository<LibraryCard, Guid>().GetByIdAsync(id);
            if (existingEntity != null)
            {
                // Map to dto 
                var cardDto = _mapper.Map<LibraryCardDto>(existingEntity);
                // Try to retrieve previous user (if any)
                if (cardDto.PreviousUserId != null && cardDto.PreviousUserId != Guid.Empty)
                {
                    var previousUserDto = (await _userSvc.GetByIdAsync(
                        Guid.Parse(cardDto.PreviousUserId.ToString() ?? string.Empty))).Data as UserDto;
                    // Add previous user
                    cardDto.AddPreviousUser(previousUserDto);
                }
                
                // Get data success
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), cardDto);
            }
            
            // Data not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke while process get library card by id");
        }
    }

    public override async Task<IServiceResult> UpdateAsync(Guid id, LibraryCardDto dto)
    {
        try
        {
            // Determine current system lang
		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
			    LanguageContext.CurrentLanguage);
		    var isEng = lang == SystemLanguage.English;

		    // Validate library card
		    var validationResult = await ValidatorExtensions.ValidateAsync(dto);
		    // Check for valid validations
		    if (validationResult != null && !validationResult.IsValid)
		    {
			    // Convert ValidationResult to ValidationProblemsDetails.Errors
			    var errors = validationResult.ToProblemDetails().Errors;
			    throw new UnprocessableEntityException("Invalid Validations", errors);
		    }

		    // Check exist user library card in DB
		    var existingEntity = await _unitOfWork.Repository<LibraryCard, Guid>().GetByIdAsync(id);
		    if (existingEntity == null)
		    {
			    // Not found {0}
			    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
			    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
				    StringUtils.Format(errMsg, isEng ? "library card to update" : "thẻ thư viện để cập nhật"));
		    }

		    // Update properties
		    existingEntity.FullName = dto.FullName;
		    existingEntity.Avatar = dto.Avatar;
		    existingEntity.IssuanceMethod = dto.IssuanceMethod;

		    // Props related to extend the amount of borrow items for user (once only after user borrowed success)
		    existingEntity.IsAllowBorrowMore = dto.IsAllowBorrowMore;
		    existingEntity.MaxItemOnceTime = dto.MaxItemOnceTime;
		    existingEntity.TotalMissedPickUp = dto.TotalMissedPickUp;

		    // Process update entity 
		    await _unitOfWork.Repository<LibraryCard, Guid>().UpdateAsync(existingEntity);
		    // Save DB
		    var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
		    if (isSaved)
		    {
			    // Update success
			    return new ServiceResult(ResultCodeConst.SYS_Success0003,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
		    }

		    // Fail to update
		    return new ServiceResult(ResultCodeConst.SYS_Fail0003,
			    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
	    }
	    catch (UnprocessableEntityException)
	    {
		    throw;
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process update library card holder");
	    }
    }

    public override async Task<IServiceResult> DeleteAsync(Guid id)
    {
        try
        {   
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Retrieve library card with id 
            var existingEntity = await _unitOfWork.Repository<LibraryCard, Guid>().GetByIdAsync(id);
            if (existingEntity == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library card" : "thẻ thư viện"));
            }
            
            // Check whether card has not archived yet 
            if (!existingEntity.IsArchived || existingEntity.PreviousUserId == null)
            {
                // The action cannot be performed, as library card need to change status to archived
                return new ServiceResult(ResultCodeConst.LibraryCard_Warning0007,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0007));
            }
            
            // Process delete library card
            await _unitOfWork.Repository<LibraryCard, Guid>().DeleteAsync(id);
            // Save DB 
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                // Save success
                return new ServiceResult(ResultCodeConst.SYS_Success0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004), true);
            }
            
            // Fail to delete
            return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);
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
            throw new Exception("Error invoke when process delete archived library card");
        }
    }

    public async Task<IServiceResult> ConfirmRegisterAsync(Guid libraryCardId, string transactionToken)
    {
        try
        {
            // Retrieve user information
            // Build spec
            var userBaseSpec = new BaseSpecification<User>(u => Equals(u.LibraryCardId, libraryCardId));
            var userDto = (await _userSvc.GetWithSpecAsync(userBaseSpec)).Data as UserDto;
            if (userDto == null || userDto.LibraryCardId == null) // Not found user or their library card
            {
                // Forbid
                throw new ForbiddenException();
            }
            
            // Initialize payment utils
            var paymentUtils = new PaymentUtils(logger: _logger);
            // Validate transaction token
            var validatedToken = await paymentUtils.ValidateTransactionTokenAsync(
                token: transactionToken,
                tokenValidationParameters: _tokenValidationParams);
            if (validatedToken == null) throw new ForbiddenException(); // Forbid as token is invalid
            
            // Extract transaction data from token
            var tokenExtractedData = paymentUtils.ExtractTransactionDataFromToken(validatedToken);
            
            // Check whether email match (request and payment user is different)
            if(!Equals(userDto.Email, tokenExtractedData.Email)) throw new ForbiddenException(); // Forbid as email is not match
            
            var transCode = tokenExtractedData.TransactionCode; 
            var transDate = tokenExtractedData.TransactionDate; 
            // Retrieve transaction
            // Build spec
            var transSpec = new BaseSpecification<Transaction>(t => 
                t.TransactionDate != null && // with specific date
                t.UserId == userDto.UserId && // who request
                t.LibraryCardPackageId != null && // payment for specific card package
                t.TransactionStatus == TransactionStatus.Paid && // must be paid
                Equals(t.TransactionCode, transCode));  // transaction code
            // Apply include
            transSpec.ApplyInclude(q => q.Include(t => t.LibraryCardPackage!));
            // Retrieve with spec
            var transactionDto = (await _tranSvc.GetWithSpecAsync(transSpec)).Data as TransactionDto;
            if (transactionDto == null) throw new ForbiddenException(); // Not found transaction information  
            
            // Validate transaction date
            if (!Equals(transactionDto.TransactionDate?.Date, transDate.Date)) throw new ForbiddenException();

            // Retrieve library card
            var existingEntity = await _unitOfWork.Repository<LibraryCard, Guid>().GetByIdAsync(
                Guid.Parse(userDto.LibraryCardId!.ToString()));
            if(existingEntity == null) throw new ForbiddenException(); // Not found any card match
            
            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Change card status to Active
            existingEntity.Status = LibraryCardStatus.Active;
            // Add expiry date
            existingEntity.ExpiryDate = currentLocalDateTime.AddMonths(
                // Months defined in specific library card package 
                transactionDto.LibraryCardPackage!.DurationInMonths);
            
            // Process update
            await _unitOfWork.Repository<LibraryCard, Guid>().UpdateAsync(existingEntity);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                // Update successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
            }

            // Fail to update
            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (UnauthorizedException)
        {
            throw new UnauthorizedException("Invalid token or token has expired");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, ex.Message);
            throw new Exception("Error invoke when process confirm register library card");
        }
    }
    
    public async Task<IServiceResult> ConfirmExtendCardAsync(Guid libraryCardId, string transactionToken)
    {
        try
        {
            // Retrieve user information
            // Build spec
            var userBaseSpec = new BaseSpecification<User>(u => Equals(u.LibraryCardId, libraryCardId));
            var userDto = (await _userSvc.GetWithSpecAsync(userBaseSpec)).Data as UserDto;
            if (userDto == null || userDto.LibraryCardId == null) // Not found user or their library card
            {
                // Forbid
                throw new ForbiddenException();
            }

            // Initialize payment utils
            var paymentUtils = new PaymentUtils(logger: _logger);
            // Validate transaction token
            var validatedToken = await paymentUtils.ValidateTransactionTokenAsync(
                token: transactionToken,
                tokenValidationParameters: _tokenValidationParams);
            if (validatedToken == null) throw new ForbiddenException(); // Forbid as token is invalid

            // Extract transaction data from token
            var tokenExtractedData = paymentUtils.ExtractTransactionDataFromToken(validatedToken);

            // Check whether email match (request and payment user is different)
            if (!Equals(userDto.Email, tokenExtractedData.Email))
                throw new ForbiddenException(); // Forbid as email is not match

            var transCode = tokenExtractedData.TransactionCode;
            var transDate = tokenExtractedData.TransactionDate;
            // Retrieve transaction
            // Build spec
            var transSpec = new BaseSpecification<Transaction>(t =>
                t.TransactionDate != null && // with specific date
                t.UserId == userDto.UserId && // who request
                t.LibraryCardPackageId != null && // payment for specific card package
                t.TransactionStatus == TransactionStatus.Paid && // must be paid
                Equals(t.TransactionCode, transCode)); // transaction code
            // Apply include
            transSpec.ApplyInclude(q => q.Include(t => t.LibraryCardPackage!));
            // Retrieve with spec
            var transactionDto = (await _tranSvc.GetWithSpecAsync(transSpec)).Data as TransactionDto;
            if (transactionDto == null) throw new ForbiddenException(); // Not found transaction information  

            // Validate transaction date
            if (!Equals(transactionDto.TransactionDate?.Date, transDate.Date)) throw new ForbiddenException();

            // Retrieve library card
            var existingEntity = await _unitOfWork.Repository<LibraryCard, Guid>().GetByIdAsync(libraryCardId);
            if (existingEntity == null) throw new ForbiddenException(); // Not found any card match

            // Check allow to extend card
            if ((await CheckCardExtensionAsync(libraryCardId)).Data is false)
            {
                // Forbid
                throw new ForbiddenException();
            }

            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

            // Change card status to Active
            existingEntity.Status = LibraryCardStatus.Active;
            // Add expiry date
            existingEntity.ExpiryDate = currentLocalDateTime.AddMonths(
                // Months defined in specific library card package 
                transactionDto.LibraryCardPackage!.DurationInMonths);
            // Change extension status
            existingEntity.IsExtended = true;
            // Increase extend time
            existingEntity.ExtensionCount++;

            // Process update
            await _unitOfWork.Repository<LibraryCard, Guid>().UpdateAsync(existingEntity);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                // Update successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
            }

            // Fail to update
            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (UnauthorizedException)
        {
            throw new UnauthorizedException("Invalid token or token has expired");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, ex.Message);
            throw new Exception("Error invoke when process confirm register library card");
        }
    }
    
    public async Task<IServiceResult> CheckCardExtensionAsync(Guid libraryCardId)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Retrieve library card
            var existingEntity = await _unitOfWork.Repository<LibraryCard, Guid>().GetByIdAsync(libraryCardId);
            if (existingEntity == null)
            {
                // Not found any card match
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library card" : "thẻ thư viện"));
            } 
            
            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Check whether card is not expired yet
            if (existingEntity.Status != LibraryCardStatus.Expired)
            {
                // Msg: Fail to extend library card as {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0006);
                
                // Card status 
                switch (existingEntity.Status)
                {
                    case LibraryCardStatus.Active:
                        if (currentLocalDateTime < existingEntity.ExpiryDate)
                        {
                            return new ServiceResult(ResultCodeConst.LibraryCard_Warning0006,
                                StringUtils.Format(errMsg, isEng 
                                    ? $"library card has not expired until {existingEntity.ExpiryDate.Value:MM/dd/yyyy}" 
                                    : $"thẻ vẫn chưa hết hạn cho đến ngày {existingEntity.ExpiryDate.Value:MM/dd/yyyy}"), false);
                        }
                        break;
                    case LibraryCardStatus.Expired:
                        // Continue to process extend card
                        break;
                    case LibraryCardStatus.Pending:
                        return new ServiceResult(ResultCodeConst.LibraryCard_Warning0006,
                            StringUtils.Format(errMsg, isEng 
                                ? "library card has not activated yet" 
                                : "thẻ vẫn chưa được kích hoạt"), false);
                    case LibraryCardStatus.Suspended:
                        // Check for suspension end date
                        if (existingEntity.SuspensionEndDate != null &&
                            existingEntity.SuspensionEndDate > currentLocalDateTime)
                        {
                            return new ServiceResult(ResultCodeConst.LibraryCard_Warning0006,
                                StringUtils.Format(errMsg, isEng 
                                    ? "library card is suspending" 
                                    : "thẻ đang bị cấm"), false);
                        }
                        break;
                }
            }
            
            // Is allow 
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, ex.Message);
            throw new Exception("Error invoke when process check allow to extend card");
        }
    }
    
    public async Task<IServiceResult> CheckCardValidityAsync(Guid libraryCardId)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Check existing card
            var existingEntity = await _unitOfWork.Repository<LibraryCard, Guid>().GetByIdAsync(libraryCardId);
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library card" : "thẻ thư viện"));
            }
            
            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            	// Vietnam timezone
            	TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Card status 
            switch (existingEntity.Status)
            {
                case LibraryCardStatus.Active:
                    // Continue to check for other information
                    break;
                case LibraryCardStatus.Pending:
                    // Your library card is not activated yet. Please make a payment to activate your card
                    return new ServiceResult(ResultCodeConst.LibraryCard_Warning0001,
                        await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0001));
                case LibraryCardStatus.Expired:
                    // Check for expiry date
                    if (existingEntity.ExpiryDate != null &&
                        existingEntity.ExpiryDate < currentLocalDateTime)
                    {
                        // Your library card has expired. Please renew it to continue using library services
                        return new ServiceResult(ResultCodeConst.LibraryCard_Warning0002,
                            await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0002));
                    }
                    break;
                case LibraryCardStatus.Suspended:
                    // Check for suspension end date
                    if (existingEntity.SuspensionEndDate != null &&
                        existingEntity.SuspensionEndDate > currentLocalDateTime)
                    {
                        var msg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0003);
                        // Your library card has been suspended due to a violation or administrative action. Please contact the library for assistance
                        return new ServiceResult(ResultCodeConst.LibraryCard_Warning0003,
                            StringUtils.Format(msg, existingEntity.SuspensionEndDate.Value.ToString("MM/dd/yyyy")));
                    }
                    break;
            }
            
            // Valid card
            return new ServiceResult(ResultCodeConst.LibraryCard_Success0001,
                await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Success0001));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process check library card validity");
        }
    }

    public async Task<IServiceResult> UpdateBorrowMoreStatusWithoutSaveChangesAsync(Guid libraryCardId)
    {
        try
        {
            // Check exist entity
            var existingEntity = await _unitOfWork.Repository<LibraryCard, Guid>().GetByIdAsync(libraryCardId);
            if (existingEntity == null)
            {
                // Fail to update
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
            }

            // Process update status
            existingEntity.IsAllowBorrowMore = false;
            existingEntity.MaxItemOnceTime = 0;
            
            // Perform update
            await _unitOfWork.Repository<LibraryCard, Guid>().UpdateAsync(existingEntity);
            
            // Update success
            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update borrow more status without saving");
        }
    }

    public async Task<IServiceResult> SuspendCardAsync(Guid libraryCardId, DateTime suspensionEndDate)
    {
        try
        {   
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Check exist library card
            var existingEntity = await _unitOfWork.Repository<LibraryCard, Guid>().GetByIdAsync(libraryCardId);
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library card" : "thẻ thư viện"));
            }
            else if (existingEntity.Status == LibraryCardStatus.Pending)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003);
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    errMsg + (isEng 
                        ? ", cannot suspend pending library card" 
                        : ", không thể chuyển trạng thái sang bị cấm vì thẻ chưa hoạt động"));
            }
            else if (existingEntity.Status == LibraryCardStatus.Suspended)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003);
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    errMsg + (isEng 
                        ? ", library card has already suspended" 
                        : ", thẻ ở trạng trái đã bị cấm"));
            }
            
            // Update props
            existingEntity.Status = LibraryCardStatus.Suspended;
            existingEntity.SuspensionEndDate = suspensionEndDate;
            
            // Process update entity
            await _unitOfWork.Repository<LibraryCard, Guid>().UpdateAsync(existingEntity);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                // Update success
                return new ServiceResult(ResultCodeConst.SYS_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
            }
            
            // Fail to update
            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process suspend card");
        }
    }
    
    public async Task<IServiceResult> UnsuspendCardAsync(Guid libraryCardId)
    {
        try
        {   
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Check exist library card
            var existingEntity = await _unitOfWork.Repository<LibraryCard, Guid>().GetByIdAsync(libraryCardId);
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library card" : "thẻ thư viện"));
            }else if (existingEntity.Status != LibraryCardStatus.Suspended)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003);
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    StringUtils.Format(errMsg, isEng 
                        ? ", library card has not suspended yet" 
                        : ", thẻ đang không ở trạng trái bị cấm"));
            }
            
            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Default update card to active
            existingEntity.Status = LibraryCardStatus.Active;
            // Check whether card is expired
            if (currentLocalDateTime >= existingEntity.ExpiryDate)
            {
                // Update card to expired
                existingEntity.Status = LibraryCardStatus.Expired;
            }
            // Set null suspension end date
            existingEntity.SuspensionEndDate = null;
            
            // Process update entity
            await _unitOfWork.Repository<LibraryCard, Guid>().UpdateAsync(existingEntity);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                // Update success
                return new ServiceResult(ResultCodeConst.SYS_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
            }
            
            // Fail to update
            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process suspend card");
        }
    }

    public async Task<IServiceResult> ArchiveCardAsync(Guid userId, Guid libraryCardId, string archiveReason)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Check exist user 
            var userDto = (await _userSvc.GetByIdAsync(userId)).Data as UserDto;
            if (userDto == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "reader" : "bạn đọc"));
            }else if (userDto.LibraryCardId != libraryCardId)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library card" : "thẻ thư viện của bạn đọc"));
            }
            
            // Retrieve card
            var existingEntity = await _unitOfWork.Repository<LibraryCard, Guid>().GetByIdAsync(libraryCardId);
            if (existingEntity == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library card" : "thẻ thư viện của bạn đọc"));
            }
            
            // Check card has already archived yet
            if (existingEntity.Status == LibraryCardStatus.Suspended ||
                (existingEntity.IsArchived && existingEntity.PreviousUserId != Guid.Empty))
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library card" : "thẻ thư viện của bạn đọc"));
            }
            
            // Process archive card
            existingEntity.PreviousUserId = userDto.UserId;
            existingEntity.IsArchived = true;
            existingEntity.ArchiveReason = archiveReason;
            existingEntity.Status = LibraryCardStatus.Suspended;
            
            // Process delete current user's card without save
            await _userSvc.DeleteLibraryCardWithoutSaveChangesAsync(userId: userDto.UserId);

            // Update
            await _unitOfWork.Repository<LibraryCard, Guid>().UpdateAsync(existingEntity);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesWithTransactionAsync() > 0;
            if (isSaved)
            {
                // Msg: The library card has been archived and is no longer valid
                return new ServiceResult(ResultCodeConst.LibraryCard_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Success0003), true);
            }
            
            // Msg: Failed to archive library card
            return new ServiceResult(ResultCodeConst.LibraryCard_Fail0002,
                await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Fail0002), true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke while process archiving library card");
        }
    }
}