using System.Globalization;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Dtos.Payments.PayOS;
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
using Microsoft.AspNetCore.Routing;
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
    private readonly IEmployeeService<EmployeeDto> _employeeSvc;
    private readonly ITransactionService<TransactionDto> _tranSvc;
    private readonly IPaymentMethodService<PaymentMethodDto> _paymentMethodSvc;
    private readonly ILibraryCardPackageService<LibraryCardPackageDto> _cardPackageSvc;

    private readonly IEmailService _emailSvc;
    private readonly AppSettings _appSettings;
    private readonly BorrowSettings _borrowSettings;
    private readonly PayOSSettings _payOsSettings;
    private readonly PaymentSettings _paymentSettings;
    private readonly TokenValidationParameters _tokenValidationParams;

    public LibraryCardService(
        IUserService<UserDto> userSvc,
        IEmployeeService<EmployeeDto> employeeSvc,
        ITransactionService<TransactionDto> tranSvc,
        IPaymentMethodService<PaymentMethodDto> paymentMethodSvc,
        ILibraryCardPackageService<LibraryCardPackageDto> cardPackageSvc,
        IOptionsMonitor<AppSettings> monitor,
        IOptionsMonitor<BorrowSettings> monitor1,
        IOptionsMonitor<PaymentSettings> monitor2,
        IOptionsMonitor<PayOSSettings> monitor3,
        IEmailService emailSvc,
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
        _emailSvc = emailSvc;
        _employeeSvc = employeeSvc;
        _cardPackageSvc = cardPackageSvc;
        _paymentMethodSvc = paymentMethodSvc;
        _tokenValidationParams = tokenValidationParams;
        _appSettings = monitor.CurrentValue;
        _borrowSettings = monitor1.CurrentValue;
        _payOsSettings = monitor3.CurrentValue;
        _paymentSettings = monitor2.CurrentValue;
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
            existingEntity.AllowBorrowMoreReason = dto.AllowBorrowMoreReason;

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

    public async Task<IServiceResult> RegisterCardAsync(string email, LibraryCardDto dto)
	{
		try
		{
			// Determine current lang context
			var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
				LanguageContext.CurrentLanguage);
			var isEng = lang == SystemLanguage.English;

			// Retrieve user information
			// Build spec
			var userBaseSpec = new BaseSpecification<User>(u => Equals(u.Email, email));
			var user = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(userBaseSpec);
			if (user == null) 
			{
				// Not found {0}
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMsg, isEng ? "user" : "bạn đọc"));
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

			// Check exist avatar
			if (!string.IsNullOrEmpty(dto.Avatar))
			{
				// Initialize field
				var isImageOnCloud = true;

				// Extract provider public id
				var publicId = StringUtils.GetPublicIdFromUrl(dto.Avatar);
				if (publicId != null) // Found
				{
					// Process check exist on cloud			
					isImageOnCloud = (await _cloudSvc.IsExistAsync(publicId, FileType.Image)).Data is true;
				}

				if (!isImageOnCloud || publicId == null) // Not found image or public id
				{
					// Not found image resource
					return new ServiceResult(ResultCodeConst.Cloud_Warning0001,
						await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Warning0001));
				}
			}

			// Check whether user has already registered library card
			if (user.LibraryCardId != null) // Card found 
			{
				// Cannot progress {0} as {1} already exist
				var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0003);
				return new ServiceResult(ResultCodeConst.SYS_Warning0003,
					StringUtils.Format(msg,
						isEng ? "register" : "đăng ký",
						isEng ? "library card" : "thẻ thư viện"));
			}

			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

			// Add necessary props
			dto.Barcode = LibraryCardUtils.GenerateBarcode(_appSettings.LibraryCardBarcodePrefix);
			dto.IssuanceMethod = LibraryCardIssuanceMethod.Online;
			dto.IsReminderSent = false;
			dto.IsExtended = false;
			dto.ExtensionCount = 0;
			dto.IssueDate = currentLocalDateTime;

			// Extend borrow default
			dto.IsAllowBorrowMore = false;
			dto.MaxItemOnceTime = 0;

			// Total missed default 
			dto.TotalMissedPickUp = 0;

			// Library card has not paid yet
			dto.Status = LibraryCardStatus.UnPaid;
			
			// Process update
			await _unitOfWork.Repository<LibraryCard, Guid>().UpdateAsync(_mapper.Map<LibraryCard>(dto));

			// Save DB
			var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
			if (isSaved)
			{
				// Msg: Register library card success
				return new ServiceResult(ResultCodeConst.LibraryCard_Success0002,
					await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Success0002));
			}

			// Msg: Register library card failed
			return new ServiceResult(ResultCodeConst.LibraryCard_Fail0001,
				await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Fail0001));
		}
		catch (UnprocessableEntityException)
		{
			throw;
		}
	    catch (Exception ex)
	    {
	        _logger.Error(ex.Message);
	        throw new Exception("Error invoke when process register library card");
	    }
	}
    
    public async Task<IServiceResult> RegisterCardByEmployeeAsync(
        string processedByEmail, Guid userId, 
        LibraryCardDto dto, TransactionMethod transactionMethod, 
        int? paymentMethodId, int libraryCardPackageId)
	{
	    try
	    {
	        // Determine current lang context
	        var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
	            LanguageContext.CurrentLanguage);
	        var isEng = lang == SystemLanguage.English;
	        
	        // Check exist process by information
	        var isEmailExist = (await _employeeSvc.AnyAsync(e => Equals(e.Email, processedByEmail))).Data is true;
	        if (!isEmailExist) // not found
	        {
		        throw new ForbiddenException(); 
	        }
	        
	        // Retrieve user information
	        // Build spec
	        var userBaseSpec = new BaseSpecification<User>(u => Equals(u.UserId, userId));
	        var user = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(userBaseSpec);
	        if (user == null) // Not found user 
	        {
		        // Not found {0}
		        var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
		        return new ServiceResult(ResultCodeConst.SYS_Warning0002,
			        StringUtils.Format(errMsg, isEng ? "user" : "người dùng"));
	        }
            
            // Check whether user has registered card yet
            if (user.LibraryCardId != null || user.LibraryCardId != Guid.Empty)
            {
                // Msg: Cannot progress {0} as {1} already exist
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0003);
                return new ServiceResult(ResultCodeConst.SYS_Warning0003,
                    StringUtils.Format(errMsg,
                        isEng ? "register" : "library card",
                        isEng ? "đăng ký" : "thẻ thư viện"));
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
	        
	        // Current local datetime
	        var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
		        // Vietnam timezone
		        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
	        
	        // Try to retrieve library card package 
	        var libCardPackageDto = (await _cardPackageSvc.GetByIdAsync(id: libraryCardPackageId)
		        ).Data as LibraryCardPackageDto;
	        if (libCardPackageDto == null)
	        {
		        // Msg: Payment object does not exist. Please try again
		        return new ServiceResult(ResultCodeConst.Transaction_Fail0002,
			        await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0002));
	        }
	        
            // Initialize payOS response
            PayOSPaymentResponseDto? payOsResp = null; 
	        // Initialize transaction 
	        TransactionDto? transactionDto = null;
	        // Generate transaction code
            var transactionCode = PaymentUtils.GenerateRandomOrderCodeDigits(_paymentSettings.TransactionCodeLength);
			// Determine transaction method
			switch (transactionMethod)
			{
				// Cash
				case TransactionMethod.Cash:
					// Create transaction with PAID status
					transactionDto = new TransactionDto
					{
						TransactionCode = transactionCode.ToString(),
						Amount = libCardPackageDto.Price,
						TransactionMethod = TransactionMethod.Cash,
						TransactionStatus = TransactionStatus.Paid,
						TransactionType = TransactionType.LibraryCardRegister,
						CreatedAt = currentLocalDateTime,
						TransactionDate = currentLocalDateTime,
						LibraryCardPackageId = libCardPackageDto.LibraryCardPackageId,
						UserId = user.UserId,
						CreatedBy = processedByEmail
					};
					
					// Update card status
                    dto.Status = LibraryCardStatus.Active;
                    // Set expiry date
                    dto.ExpiryDate = currentLocalDateTime.AddMonths(
                        // Months defined in specific library card package 
                        libCardPackageDto.DurationInMonths);
					break;
				// Digital payment
				case TransactionMethod.DigitalPayment:
					// Check exist payment method id 
                    var isExistPaymentMethod = (await _paymentMethodSvc.AnyAsync(p => 
	                    Equals(p.PaymentMethodId, paymentMethodId))).Data is true;
					if (!isExistPaymentMethod)
					{
						// Not found {0}
						var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
						return new ServiceResult(ResultCodeConst.SYS_Warning0002,
							StringUtils.Format(errMsg, isEng ? "payment method" : "phương thức thanh toán"));
					}
					
                    // Check whether existing any transaction has pending status
                    var isExistPendingStatus = await _unitOfWork.Repository<Transaction, int>()
                        .AnyAsync(t => t.TransactionType == TransactionType.LibraryCardRegister &&
                                       t.TransactionStatus == TransactionStatus.Pending);
                    if (isExistPendingStatus)
                    {
                        // Msg: Failed to create payment transaction as existing transaction with pending status
                        return new ServiceResult(ResultCodeConst.Transaction_Warning0003,
                            await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Warning0003));
                    }
                    
					// Create transaction with PENDING status (digital payment)
                    transactionDto = new TransactionDto
                    {
                        TransactionCode = transactionCode.ToString(),
                        Amount = libCardPackageDto.Price,
                        TransactionMethod = TransactionMethod.DigitalPayment,
                        TransactionStatus = TransactionStatus.Pending,
                        TransactionType = TransactionType.LibraryCardRegister,
                        CreatedAt = currentLocalDateTime,
                        ExpiredAt = currentLocalDateTime.AddMinutes(_paymentSettings.TransactionExpiredInMinutes),
                        LibraryCardPackageId = libCardPackageDto.LibraryCardPackageId,
                        PaymentMethodId = paymentMethodId,
                        UserId = user.UserId,
                        CreatedBy = processedByEmail
                    };
                    
                    // Generate payment link
                    var payOsPaymentRequest = new PayOSPaymentRequestDto()
                    {
                        OrderCode = transactionCode,
                        Amount = (int) transactionDto.Amount,
                        Description = isEng ? "Library card register"  : "Dang ky the thu vien",
                        BuyerName = $"{user.FirstName} {user.LastName}".ToUpper(),
                        BuyerEmail = user.Email,
                        BuyerPhone = user.Phone ?? string.Empty,
                        BuyerAddress = user.Address ?? string.Empty,
                        Items = [
                            new
                            {
                                Name = isEng ? transactionDto.TransactionType.ToString() : transactionDto.TransactionType.GetDescription(),
                                Quantity = 1,
                                Price = transactionDto.Amount
                            }
                        ],
                        CancelUrl = _payOsSettings.CancelUrl,
                        ReturnUrl = _payOsSettings.ReturnUrl,
                        ExpiredAt = (int)((DateTimeOffset) transactionDto.ExpiredAt).ToUnixTimeSeconds()
                    };
                    
                    // Generate signature
                    await payOsPaymentRequest.GenerateSignatureAsync(transactionCode, _payOsSettings);
                    var payOsPaymentResp = await payOsPaymentRequest.GetUrlAsync(_payOsSettings);
                    
                    // Create Payment status
                    bool isCreatePaymentSuccess = payOsPaymentResp.Item1; // Is created success
                    if (isCreatePaymentSuccess && payOsPaymentResp.Item3 != null)
                    {
                        // Assign payOs response
                        payOsResp = payOsPaymentResp.Item3;
                        
                        // Set library card default status
                        dto.Status = LibraryCardStatus.UnPaid;
                        // Assign transaction code
                        dto.TransactionCode = transactionCode.ToString();
                        // Assign payment URL
                        transactionDto.PaymentUrl = payOsResp.Data.CheckoutUrl;
                    }
                    else
                    {
                        // Msg: Failed to create payment transaction. Please try again
                        return new ServiceResult(ResultCodeConst.Transaction_Fail0001,
                            await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0001));
                    }
                    break;
			}

			if (transactionDto == null)
			{
				// Mark as fail to register
				return new ServiceResult(ResultCodeConst.SYS_Fail0001,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
			}
			
			// Check exist avatar
	        if (!string.IsNullOrEmpty(dto.Avatar))
	        {
	            // Initialize field
	            var isImageOnCloud = true;

	            // Extract provider public id
	            var publicId = StringUtils.GetPublicIdFromUrl(dto.Avatar);
	            if (publicId != null) // Found
	            {
	                // Process check exist on cloud			
	                isImageOnCloud = (await _cloudSvc.IsExistAsync(publicId, FileType.Image)).Data is true;
	            }

	            if (!isImageOnCloud || publicId == null) // Not found image or public id
	            {
	                // Not found image resource
	                return new ServiceResult(ResultCodeConst.Cloud_Warning0001,
	                    await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Warning0001));
	            }
	        }
            
	        // Add issuance method to lib card
            dto.IssuanceMethod = LibraryCardIssuanceMethod.InPerson;
            // Add issue date to lib card
            dto.IssueDate = currentLocalDateTime;
            
            // Process create transaction without save changes
            await _tranSvc.CreateWithoutSaveChangesAsync(transactionDto);
            // Process create library card 
            await _unitOfWork.Repository<LibraryCard, Guid>().AddAsync(_mapper.Map<LibraryCard>(dto));
            
	        // Save DB
	        var isSaved = await _unitOfWork.SaveChangesWithTransactionAsync() > 0;
	        if (isSaved)
	        {
                // Determine transaction method
                switch (transactionMethod)
                {
                    case TransactionMethod.Cash:
                        // Send card has been activated email
                        var isSent = await SendActivatedEmailAsync(
                        	email: user.Email,
                        	cardDto: dto,
                        	transactionDto: transactionDto,
                        	libName: _appSettings.LibraryName,
                        	libContact: _appSettings.LibraryContact);
                        
                        if (isSent)
                        {
                        	var successMsg = isEng ? "Announcement email has sent to reader" : "Email thông báo đã gửi đến độc giả"; 
                        	// Msg: Register library card success
                        	return new ServiceResult(ResultCodeConst.LibraryCard_Success0002,
                        		await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Success0002) + $". {successMsg}");
                        }
                        
                        var failMsg = isEng ? ", but fail to send email" : ", nhưng gửi email thông báo thất bại";
                        // Msg: Register library card success
                        return new ServiceResult(ResultCodeConst.LibraryCard_Success0002,
                        	await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Success0002) + failMsg);
                    case TransactionMethod.DigitalPayment:
                        // Msg: Register library card success combined with payment information
                        return new ServiceResult(ResultCodeConst.LibraryCard_Success0002,
                            await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Success0002), payOsResp);
                }
	        }
	        
	        // Msg: Register library card failed
	        return new ServiceResult(ResultCodeConst.LibraryCard_Fail0001,
	            await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Fail0001));
	    }
		catch(UnauthorizedException)
		{
			throw new Exception("Token has expired");
		}
	    catch (ForbiddenException)
	    {
	        throw;
	    }
	    catch (Exception ex)
	    {
	        _logger.Error(ex.Message);
	        throw new Exception("Error invoke when process add library card by employee");
	    }
	}
    
    public async Task<IServiceResult> SendRequireToConfirmCardAsync(string userEmail)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Retrieve user information
            // Build spec
            var userBaseSpec = new BaseSpecification<User>(u => Equals(u.Email, userEmail));
            // Apply include
            userBaseSpec.ApplyInclude(q => q
                .Include(u => u.LibraryCard)!
            );
            var userDto = (await _userSvc.GetWithSpecAsync(userBaseSpec)).Data as UserDto;
            if (userDto == null) throw new ForbiddenException(); // Not found user 
            
            // Try parse card id to Guid type 
            Guid.TryParse(userDto.LibraryCardId.ToString(), out var validCardId);
            // Check exist library card
            if (validCardId == Guid.Empty)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.LibraryCard_Warning0004,
                    StringUtils.Format(errMsg, isEng ? "library card" : "thẻ thư viện"));
            }
            
            // Retrieve library card information
            var existingEntity = await _unitOfWork.Repository<LibraryCard, Guid>().GetByIdAsync(validCardId);
            if (existingEntity == null)
            {
                // Unknown error
                return new ServiceResult(ResultCodeConst.SYS_Warning0006,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0006));
            }
            
            // Check whether card has not been rejected
            if (existingEntity.Status != LibraryCardStatus.Rejected)
            {
                // Msg: Cannot process send card reconfirmation when card status is not rejected
                return new ServiceResult(ResultCodeConst.LibraryCard_Warning0013,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0013));
            }
            
            // Update card status to pending
            existingEntity.Status = LibraryCardStatus.Pending;
            
            // Process update 
            await _unitOfWork.Repository<LibraryCard, Guid>().UpdateAsync(existingEntity);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                // Msg: Send card reconfirmation successfully
                return new ServiceResult(ResultCodeConst.LibraryCard_Success0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Success0004));
            }
            
            // Msg: Failed to send reconfirmation library card
            return new ServiceResult(ResultCodeConst.LibraryCard_Fail0003,
                await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Fail0003));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process send require to confirm card");
        }
    }
    
    public async Task<IServiceResult> ConfirmCardRegisterWithoutSaveChangesAsync(string email, string transactionToken)
	{
		try
		{
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Initialize msg failed to register card as {0}
            var registerFailedMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Fail0005);
            
			// Retrieve user information
			// Build spec
			var userBaseSpec = new BaseSpecification<User>(u => Equals(u.Email, email));
			// Retrieve user with spec
			var userDto = (await _userSvc.GetWithSpecAsync(userBaseSpec)).Data as UserDto;
			if (userDto == null || userDto.LibraryCardId == null) // Not found user
			{
				// Logging
				_logger.Information("Not found user to process confirm card register");
				// Mark as failed to update
                return new ServiceResult(ResultCodeConst.LibraryCard_Fail0005,
                    StringUtils.Format(registerFailedMsg, isEng ? "not found user" : "không tìm thấy bạn đọc"), false);
			}
            
            // Retrieve library card entity 
            var libCardEntity = await _unitOfWork.Repository<LibraryCard, Guid>()
                .GetByIdAsync(Guid.Parse(userDto.LibraryCardId.ToString() ?? string.Empty));
            if (libCardEntity == null) // not found library card
            {
                // Logging
                _logger.Information("Not found user's library card to process confirm card register");
                // Mark as failed to update
                return new ServiceResult(ResultCodeConst.LibraryCard_Fail0005,
                    StringUtils.Format(registerFailedMsg, isEng 
                    ? "not found user's library card" 
                    : "không tìm thấy thẻ thư viện của bạn đọc"), false);
            }
            
            // Check whether card's status is not unpaid
            if (libCardEntity.Status != LibraryCardStatus.UnPaid)
            {
                // Mark as failed to update
                return new ServiceResult(ResultCodeConst.LibraryCard_Fail0005,
                    StringUtils.Format(registerFailedMsg, isEng 
                        ? "library card has been paid" 
                        : "thẻ thư viện đã thanh toán"), false);
            }
			
            // Initialize payment utils
			var paymentUtils = new PaymentUtils(logger: _logger);
			// Validate transaction token
			var validatedToken = await paymentUtils.ValidateTransactionTokenAsync(
				token: transactionToken,
				tokenValidationParameters: _tokenValidationParams);
			if (validatedToken == null) 
			{
				// Logging
				_logger.Information("Token is invalid, cannot process confirm card register");
                // Mark as failed to update
                return new ServiceResult(ResultCodeConst.LibraryCard_Fail0005,
                    StringUtils.Format(registerFailedMsg, isEng 
                        ? "payment transaction information is not found" 
                        : "thông tin thanh toán không tồn tại"), false);
			}	
            
			// Extract transaction data from token
            var tokenExtractedData = paymentUtils.ExtractTransactionDataFromToken(validatedToken);

            // Check whether email match (request and payment user is different)
            if (!Equals(userDto.Email, tokenExtractedData.Email))
            {
	            // Logging
	            _logger.Information("User's email is not match with token claims to process confirm card");
                return new ServiceResult(ResultCodeConst.LibraryCard_Fail0005,
                    StringUtils.Format(registerFailedMsg, isEng 
                    ? "not found user's email" 
                    : "không tìm thấy bạn đọc"), false);
            }

            var transCode = tokenExtractedData.TransactionCode;
            var transDate = tokenExtractedData.TransactionDate;
            // Retrieve transaction
            // Build spec
            var transSpec = new BaseSpecification<Transaction>(t =>
                t.TransactionDate != null && // with specific date
                t.UserId == userDto.UserId && // who request
                t.LibraryCardPackageId != null && // payment for specific card package
                t.TransactionStatus == TransactionStatus.Paid && // must be paid
                t.TransactionType == TransactionType.LibraryCardRegister && // transaction type is lib card register
                Equals(t.TransactionCode, transCode)); // transaction code
            // Apply include
            transSpec.ApplyInclude(q => q
                .Include(t => t.LibraryCardPackage)
                .Include(t => t.PaymentMethod!)
            );
            // Retrieve with spec
            var transactionDto = (await _tranSvc.GetWithSpecAsync(transSpec)).Data as TransactionDto;
            if (transactionDto == null) // Not found any transaction match
            {
	            // Logging 
	            _logger.Information("Not found transaction information to process confirm card");
                return new ServiceResult(ResultCodeConst.LibraryCard_Fail0005,
                    StringUtils.Format(registerFailedMsg, isEng 
                    ? "not found payment transaction" 
                    : "không tìm thấy phiên thanh toán"), false);
            }
            else if(!Equals(transactionDto.TransactionDate?.Date, transDate.Date))
            {
	            // Logging 
	            _logger.Information("Transaction date is not match with token claims while process confirm card");
                return new ServiceResult(ResultCodeConst.LibraryCard_Fail0005,
                    StringUtils.Format(registerFailedMsg, isEng 
                    ? "transaction date is invalid" 
                    : "ngày thanh toán không hợp lệ"), false);
            }
			
			// Process change library card status to pending (waiting to be confirmed)
			libCardEntity.Status = LibraryCardStatus.Pending;
			// Assign transaction code 
			libCardEntity.TransactionCode = tokenExtractedData.TransactionCode;
			
            // Process update
            await _unitOfWork.Repository<LibraryCard, Guid>().UpdateAsync(libCardEntity);
            
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
            	// Send card has been activated email
            	await SendActivatedEmailAsync(
            		email: userDto.Email,
            		cardDto: _mapper.Map<LibraryCardDto>(libCardEntity),
            		transactionDto: transactionDto,
            		libName: _appSettings.LibraryName,
            		libContact: _appSettings.LibraryContact);
            	
            	// Msg: Register library card success
            	return new ServiceResult(ResultCodeConst.LibraryCard_Success0002,
            		await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Success0002));
            }
            
            // Register library card failed
            return new ServiceResult(ResultCodeConst.LibraryCard_Fail0001,
                await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Fail0001), false);
		}
        catch (UnauthorizedException)
        {
            _logger.Error("Failed to confirm library card for user with email {0} as token has been expired", email);
            throw new UnauthorizedException("Invalid token or token has expired");
        }
		catch (Exception ex)
		{
			_logger.Error(ex.Message);
			throw new Exception("Error invoke when process confirm library card register");
		}
	}

    public async Task<IServiceResult> ConfirmCardExtensionWithoutSaveChangesAsync(string email, string transactionToken)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Initialize msg fail to extend library card as {0}
            var registerFailedMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Fail0006);
            
            // Retrieve user information
            // Build spec
            var userBaseSpec = new BaseSpecification<User>(u => Equals(u.Email, email));
            // Retrieve user with spec
            var userDto = (await _userSvc.GetWithSpecAsync(userBaseSpec)).Data as UserDto;
            if (userDto == null || userDto.LibraryCardId == null) // Not found user
            {
            	// Logging
            	_logger.Information("Not found user to process confirm card extension");
            	// Mark as failed to update
                return new ServiceResult(ResultCodeConst.LibraryCard_Fail0006,
                    StringUtils.Format(registerFailedMsg, isEng ? "not found user" : "không tìm thấy bạn đọc"), false);
            }
            
            // Retrieve library card entity 
            var libCardEntity = await _unitOfWork.Repository<LibraryCard, Guid>()
                .GetByIdAsync(Guid.Parse(userDto.LibraryCardId.ToString() ?? string.Empty));
            if (libCardEntity == null) // not found library card
            {
                // Logging
                _logger.Information("Not found user's library card to process confirm card extension");
                // Mark as failed to update
                return new ServiceResult(ResultCodeConst.LibraryCard_Fail0006,
                    StringUtils.Format(registerFailedMsg, isEng 
                        ? "not found user's library card" 
                        : "không tìm thấy thẻ thư viện của bạn đọc"), false);
            }
            
            // Check whether card's status is not unpaid
            if (libCardEntity.Status != LibraryCardStatus.Expired)
            {
                // Mark as failed to update
                return new ServiceResult(ResultCodeConst.LibraryCard_Fail0006,
                    StringUtils.Format(registerFailedMsg, isEng 
                    ? "library card has not in expired status" 
                    : "thẻ thư viện đang không ở trạng thái hết hạn"), false);
            }
            
            // Initialize payment utils
            var paymentUtils = new PaymentUtils(logger: _logger);
            // Validate transaction token
            var validatedToken = await paymentUtils.ValidateTransactionTokenAsync(
                token: transactionToken,
                tokenValidationParameters: _tokenValidationParams);
            if (validatedToken == null) 
            {
                // Logging
                _logger.Information("Token is invalid, cannot process confirm card extension");
                // Mark as failed to update
                return new ServiceResult(ResultCodeConst.LibraryCard_Fail0006,
                    StringUtils.Format(registerFailedMsg, isEng 
                        ? "payment transaction information is not found" 
                        : "thông tin thanh toán không tồn tại"), false);
            }	
            
            // Extract transaction data from token
            var tokenExtractedData = paymentUtils.ExtractTransactionDataFromToken(validatedToken);

            // Check whether email match (request and payment user is different)
            if (!Equals(userDto.Email, tokenExtractedData.Email))
            {
                // Logging
                _logger.Information("User's email is not match with token claims to process extend card");
                return new ServiceResult(ResultCodeConst.LibraryCard_Fail0006,
                    StringUtils.Format(registerFailedMsg, isEng 
                        ? "not found user's email" 
                        : "không tìm thấy bạn đọc"), false);
            }
            
            var transCode = tokenExtractedData.TransactionCode;
            var transDate = tokenExtractedData.TransactionDate;
            // Retrieve transaction
            // Build spec
            var transSpec = new BaseSpecification<Transaction>(t =>
                t.TransactionDate != null && // with specific date
                t.UserId == userDto.UserId && // who request
                t.LibraryCardPackageId != null && // payment for specific card package
                t.TransactionStatus == TransactionStatus.Paid && // must be paid
                t.TransactionType == TransactionType.LibraryCardExtension && // transaction type is lib card register
                Equals(t.TransactionCode, transCode)); // transaction code
            // Apply include
            transSpec.ApplyInclude(q => q
                .Include(t => t.LibraryCardPackage)
                .Include(t => t.PaymentMethod!)
            );
            // Retrieve with spec
            var transactionDto = (await _tranSvc.GetWithSpecAsync(transSpec)).Data as TransactionDto;
            if (transactionDto == null) // Not found any transaction match
            {
                // Logging 
                _logger.Information("Not found transaction information to process extend card");
                return new ServiceResult(ResultCodeConst.LibraryCard_Fail0006,
                    StringUtils.Format(registerFailedMsg, isEng 
                        ? "not found payment transaction" 
                        : "không tìm thấy phiên thanh toán"), false);
            }
            else if(!Equals(transactionDto.TransactionDate?.Date, transDate.Date))
            {
                // Logging 
                _logger.Information("Transaction date is not match with token claims while process extend card");
                return new ServiceResult(ResultCodeConst.LibraryCard_Fail0006,
                    StringUtils.Format(registerFailedMsg, isEng 
                        ? "transaction date is invalid" 
                        : "ngày thanh toán không hợp lệ"), false);
            }
            
            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            if (transactionDto.LibraryCardPackage == null)
            {
                // Logging 
                _logger.Information("Not found library card package in payment transaction to process extend card");
                return new ServiceResult(ResultCodeConst.LibraryCard_Fail0006,
                    StringUtils.Format(registerFailedMsg, isEng 
                        ? "not found library card package in payment transaction information" 
                        : "không tìm thấy gói thẻ thư viện trong thông tin thanh toán để tiến hành gia hạn thẻ"), false);
            }
            
            // Change card status to Active
            libCardEntity.Status = LibraryCardStatus.Active;
            // Add expiry date
            libCardEntity.ExpiryDate = currentLocalDateTime.AddMonths(
                // Months defined in specific library card package 
                transactionDto.LibraryCardPackage.DurationInMonths);
            // Change extension status
            libCardEntity.IsExtended = true;
            // Increase extend time
            libCardEntity.ExtensionCount++;
            
            // Process update
            await _unitOfWork.Repository<LibraryCard, Guid>().UpdateAsync(libCardEntity);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if(isSaved)
            {
                // Send card has been activated email
                await SendCardExtensionSuccessEmailAsync(
                    email: userDto.Email,
                    cardDto: _mapper.Map<LibraryCardDto>(libCardEntity),
                    transactionDto: transactionDto,
                    libName: _appSettings.LibraryName,
                    libContact: _appSettings.LibraryContact);
                
                // Msg: Extend library card expiration successfully
                return new ServiceResult(ResultCodeConst.LibraryCard_Success0005,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Success0005));
            }
            
            // Fail to extend library card
            return new ServiceResult(ResultCodeConst.LibraryCard_Fail0004,
                await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Fail0004));
        }
        catch (UnauthorizedException)
        {
            _logger.Error("Failed to confirm library card extension for user with email {0} as token has been expired", email);
            throw new UnauthorizedException("Invalid token or token has expired");
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process confirm library card extension");
        }
    }
    
    public async Task<IServiceResult> ConfirmCardAsync(Guid libraryCardId)
    {
        try
        {
            // Determine current system lang 
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Build spec
            var cardSpec = new BaseSpecification<LibraryCard>(c => Equals(c.LibraryCardId, libraryCardId));
            // Apply including user
            cardSpec.ApplyInclude(q => q.Include(c => c.Users));
            // Retrieve library card
            var existingEntity = await _unitOfWork.Repository<LibraryCard, Guid>().GetWithSpecAsync(cardSpec);
            if (existingEntity == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library card" : "thẻ thư viện"));
            }
            
            // Build transaction spec
            var tranSpec = new BaseSpecification<Transaction>(t => Equals(t.TransactionCode, existingEntity.TransactionCode));
            // Apply including for specific registered card package
            tranSpec.ApplyInclude(q => q
                .Include(t => t.LibraryCardPackage!)
            );
            // Retrieve with spec
            var transactionDto = (await _tranSvc.GetWithSpecAsync(tranSpec)).Data as TransactionDto;
            if (transactionDto == null)
            {
                // Msg: Cannot process confirm card as not found payment information
                return new ServiceResult(ResultCodeConst.LibraryCard_Warning0010,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0010));
            }
            
            // Check whether card has been activated yet
            if (existingEntity.Status != LibraryCardStatus.Pending &&
                existingEntity.Status != LibraryCardStatus.Rejected)
            {
                // Msg: Fail to confirm card as library card has been confirmed
                return new ServiceResult(ResultCodeConst.LibraryCard_Warning0011,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0011));
            }
            
            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Change card status to active while processing confirm
            existingEntity.Status = LibraryCardStatus.Active;
            // Set expiry date
            existingEntity.ExpiryDate = currentLocalDateTime.AddMonths(
                // Months defined in specific library card package 
                transactionDto.LibraryCardPackage!.DurationInMonths);
            
            // Process update 
            await _unitOfWork.Repository<LibraryCard, Guid>().UpdateAsync(existingEntity);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                if (existingEntity.Users.Any())
                {
                    // Send card has been activated email
                    var isSent = await SendAcceptedEmailAsync(
                        email: existingEntity.Users.First().Email,
                        cardDto: _mapper.Map<LibraryCardDto>(existingEntity),
                        libName: _appSettings.LibraryName,
                        libContact: _appSettings.LibraryContact);
                    if (isSent)
                    {
                        var successMsg = isEng ? "Announcement email has sent to reader" : "Email thông báo đã gửi đến độc giả"; 
                        // Msg: Update successfully
                        return new ServiceResult(ResultCodeConst.SYS_Success0003,
                            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003) + $". {successMsg}");
                    }
                }
                
                var failMsg = isEng ? ", but fail to send email" : ", nhưng gửi email thông báo thất bại";
                // Msg: Update successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003) + failMsg);
            }

            // Msg: Fail to update
            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke whe process confirm card");
        }
    }
    
    public async Task<IServiceResult> RejectCardAsync(Guid libraryCardId, string rejectReason)
    {
        try
        {
            // Determine current system lang 
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Build spec
            var cardSpec = new BaseSpecification<LibraryCard>(c => Equals(c.LibraryCardId, libraryCardId));
            // Apply including user
            cardSpec.ApplyInclude(q => q.Include(c => c.Users));
            // Retrieve library card
            var existingEntity = await _unitOfWork.Repository<LibraryCard, Guid>().GetWithSpecAsync(cardSpec);
            if (existingEntity == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library card" : "thẻ thư viện"));
            }
            
            // Check whether card has been activated yet
            if (existingEntity.Status != LibraryCardStatus.Pending)
            {
                if (existingEntity.Status == LibraryCardStatus.Rejected)
                {
                    // Msg: Fail to reject library card as it has been rejected
                    return new ServiceResult(ResultCodeConst.LibraryCard_Warning0012,
                        await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0012));
                }
                                
                // Msg: Cannot update library card status to <0> as <1>
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0009);
                return new ServiceResult(ResultCodeConst.LibraryCard_Warning0009,
                    StringUtils.Format(errMsg, 
                    isEng ? "rejected" : "từ chối", 
                    isEng ? "card has been in used" : "thẻ đang được sử dụng"));
            }
            
            // Change card status to rejected 
            existingEntity.Status = LibraryCardStatus.Rejected;
            // Add reason
            existingEntity.RejectReason = rejectReason;
            
            // Process update 
            await _unitOfWork.Repository<LibraryCard, Guid>().UpdateAsync(existingEntity);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                if (existingEntity.Users.Any())
                {
                    // Send card has been rejected email
                    var isSent = await SendRejectEmailAsync(
                        email: existingEntity.Users.First().Email,
                        cardDto: _mapper.Map<LibraryCardDto>(existingEntity),
                        rejectReason: rejectReason,
                        libName: _appSettings.LibraryName,
                        libContact: _appSettings.LibraryContact);
                    if (isSent)
                    {
                        var successMsg = isEng ? "Announcement email has sent to reader" : "Email thông báo đã gửi đến độc giả"; 
                        // Msg: Update successfully
                        return new ServiceResult(ResultCodeConst.SYS_Success0003,
                            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003) + $". {successMsg}");
                    }
                }
                
                var failMsg = isEng ? ", but fail to send email" : ", nhưng gửi email thông báo thất bại";
                // Msg: Update successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003) + failMsg);
            }
            
            // Msg: Fail to update
            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process reject library card");
        }
    }
    
    // TODO: Reimplement extend card
    public async Task<IServiceResult> ExtendCardAsync(Guid libraryCardId, 
        string? transactionToken, // Use when register with online payment
        int? libraryCardPackageId, // Use when register with cash payment
        int? paymentMethodId) // Use when register with cash payment
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Build spec
            var userBaseSpec = new BaseSpecification<User>(u => Equals(u.LibraryCardId, libraryCardId));
            // Apply include
            userBaseSpec.ApplyInclude(q => q.Include(u => u.LibraryCard!));
            // Retrieve user information
            var user = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(userBaseSpec);
            
            if (user == null || user.LibraryCard == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library card" : "thẻ thư viện"));
            }
            
            // Check allow to extend card
            var checkRes = await CheckCardExtensionAsync(
                Guid.Parse(user.LibraryCardId.ToString() ?? string.Empty));
            if (checkRes.Data is false) return checkRes;

            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

            // Initialize empty transaction dto
            TransactionDto? transactionDto = null;
            // Initialize empty lib package dto
            LibraryCardPackageDto? libPackageDto = null;
            // Initialize payment utils
            var paymentUtils = new PaymentUtils(_logger);
            // Determine payment method
            if (transactionToken != null && // Online payment
                libraryCardPackageId == null && paymentMethodId == null) // Not include information of cash payment
            {
                // Validate transaction token
                var validatedToken = await paymentUtils.ValidateTransactionTokenAsync(
                    token: transactionToken,
                    tokenValidationParameters: _tokenValidationParams);
                if (validatedToken == null) throw new ForbiddenException(); // Forbid as token is invalid

                // Extract transaction data from token
                var tokenExtractedData = paymentUtils.ExtractTransactionDataFromToken(validatedToken);

                var transCode = tokenExtractedData.TransactionCode;
                var transDate = tokenExtractedData.TransactionDate;
                // Retrieve transaction
                // Build spec
                var transSpec = new BaseSpecification<Transaction>(t =>
                    t.TransactionDate != null && // with specific date
                    t.UserId == user.UserId && // with specific user
                    t.LibraryCardPackageId != null && // payment for specific card package
                    t.TransactionStatus == TransactionStatus.Paid && // must be paid
                    t.TransactionType ==
                    TransactionType.LibraryCardExtension && // transaction type is lib card register
                    Equals(t.TransactionCode, transCode)); // transaction code
                // Apply include
                transSpec.ApplyInclude(q => q
                    .Include(t => t.LibraryCardPackage!)
                );
                // Retrieve with spec
                transactionDto = (await _tranSvc.GetWithSpecAsync(transSpec)).Data as TransactionDto;
                if (transactionDto == null || !Equals(transactionDto.TransactionDate?.Date, transDate.Date))
                {
                    // Register library card failed as payment information is not found
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Fail0001);
                    return new ServiceResult(ResultCodeConst.LibraryCard_Fail0001,
                        errMsg + (isEng
                            ? "as not found payment information"
                            : "vì không tìm thấy thông tin thanh toán"));
                }

                // Assign lib package 
                libPackageDto = transactionDto.LibraryCardPackage;
            }
            else if (transactionToken == null && // Not include transaction token
                     paymentMethodId != null &&
                     int.TryParse(libraryCardPackageId.ToString(), out var validPackageId)) // Cash payment
            {
                // Check exist payment method 
                var isExistPaymentMethod = (await _paymentMethodSvc.AnyAsync(p =>
                    p.PaymentMethodId == paymentMethodId)).Data is true;
                if (!isExistPaymentMethod)
                {
                    // Not found {0}
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                        StringUtils.Format(errMsg, isEng ? "cash payment method" : "phương thức thanh toán tiền mặt"));
                }

                // Retrieve library card package by id 
                libPackageDto =
                    (await _cardPackageSvc.GetByIdAsync(validPackageId)).Data as LibraryCardPackageDto;
                if (libPackageDto == null)
                {
                    // Not found {0}
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                        StringUtils.Format(errMsg, isEng ? "library card package" : "gói thẻ thư viện"));
                }

                // Generate transaction code
                var transactionCode = PaymentUtils.GenerateRandomOrderCodeDigits(8);
                // Create transaction with PAID status
                transactionDto = new TransactionDto()
                {
                    TransactionCode = transactionCode.ToString(),
                    Amount = libPackageDto.Price,
                    TransactionStatus = TransactionStatus.Paid,
                    TransactionType = TransactionType.LibraryCardExtension,
                    CreatedAt = currentLocalDateTime,
                    TransactionDate = currentLocalDateTime,
                    LibraryCardPackageId = libPackageDto.LibraryCardPackageId,
                    UserId = user.UserId
                };

                // Assign transaction
                user.Transactions.Add(_mapper.Map<Transaction>(transactionDto));
            }

            if (transactionDto == null || libPackageDto == null)
            {
                // Mark as fail to register
                return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
            }
            
            // Change card status to Active
            user.LibraryCard.Status = LibraryCardStatus.Active;
            // Add expiry date
            user.LibraryCard.ExpiryDate = currentLocalDateTime.AddMonths(
                // Months defined in specific library card package 
                libPackageDto.DurationInMonths);
            // Change extension status
            user.LibraryCard.IsExtended = true;
            // Increase extend time
            user.LibraryCard.ExtensionCount++;
				
            // Process update
            await _unitOfWork.Repository<User, Guid>().UpdateAsync(user);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                if (transactionDto.LibraryCardPackage == null)
                {
                    transactionDto.LibraryCardPackage = libPackageDto;
                }
                
                // Send card has been activated email
                var isSent = await SendCardExtensionSuccessEmailAsync(
                    email: user.Email,
                    cardDto: _mapper.Map<LibraryCardDto>(user.LibraryCard),
                    transactionDto: transactionDto,
                    libName: _appSettings.LibraryName,
                    libContact: _appSettings.LibraryContact);
                if (isSent)
                {
                    var successMsg = isEng ? "Announcement email has sent to reader" : "Email thông báo đã gửi đến độc giả"; 
                    // Msg: // Extend library card expiration successfully
                    return new ServiceResult(ResultCodeConst.LibraryCard_Success0005,
                        await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Success0005) + $". {successMsg}");
                }
		            
                var failMsg = isEng ? ", but fail to send email" : ", nhưng gửi email thông báo thất bại";
                // Msg: Register library card success
                return new ServiceResult(ResultCodeConst.LibraryCard_Success0005,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Success0005) + failMsg);
            }
				
            // Fail to extend library card
            return new ServiceResult(ResultCodeConst.LibraryCard_Fail0004,
                await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Fail0004));
        }
        catch(UnauthorizedException)
        {
            throw new Exception("Token has expired");
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process extend card");
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
                    case LibraryCardStatus.Pending or LibraryCardStatus.Rejected:
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
                case LibraryCardStatus.UnPaid:
                    // Library card has not paid yet
                    return new ServiceResult(ResultCodeConst.LibraryCard_Warning0014,
                        await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0014));
                case LibraryCardStatus.Pending or LibraryCardStatus.Rejected:
                    // Library card is not activated yet. Please contact library to activate your card
                    return new ServiceResult(ResultCodeConst.LibraryCard_Warning0001,
                        await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0001));
                case LibraryCardStatus.Expired:
                    // Check for expiry date
                    if (existingEntity.ExpiryDate != null &&
                        existingEntity.ExpiryDate < currentLocalDateTime)
                    {
                        // Library card has expired. Please renew it to continue using library services
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
                        // Library card has been suspended due to a violation or administrative action. Please contact the library for assistance
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

    public async Task<IServiceResult> ExtendBorrowAmountAsync(Guid libraryCardId, int maxItemOnceTime, string reason)
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
            
            // Validate card
            var checkValidRes = await CheckCardValidityAsync(libraryCardId);
            if (checkValidRes.ResultCode != ResultCodeConst.LibraryCard_Success0001) return checkValidRes;

            if (maxItemOnceTime < _borrowSettings.BorrowAmountOnceTime)
            {
                // Fail to update. Total borrow amount threshold is not smaller than {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0008); 
                return new ServiceResult(ResultCodeConst.LibraryCard_Warning0008,
                    StringUtils.Format(errMsg, _borrowSettings.BorrowAmountOnceTime.ToString()));
            }
            
            // Validate suspension end date
            var customErrs = new Dictionary<string, string[]>();
            if (reason.Length > 250)
            {
                customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                    key: StringUtils.ToCamelCase(nameof(LibraryCard.SuspensionReason)),
                    msg: isEng 
                        ? "Suspension reason cannot exceed than 250 characters" 
                        : "Nguyên nhân không được quá 250 ký tự");
            }
            
            if (customErrs.Any()) throw new UnprocessableEntityException("Invalid data", customErrs);
            
            // Update extending total borrow amount
            existingEntity.IsAllowBorrowMore = true;
            existingEntity.MaxItemOnceTime = maxItemOnceTime;
            existingEntity.AllowBorrowMoreReason = reason;
            
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
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process extend borrow amount for library card");
        }
    }
    
    public async Task<IServiceResult> SuspendCardAsync(Guid libraryCardId, DateTime suspensionEndDate, string reason)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

            // Validate suspension end date
            var customErrs = new Dictionary<string, string[]>();
            // Validate suspension errs
            if (suspensionEndDate <= currentLocalDateTime)
            {
                customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                    key: StringUtils.ToCamelCase(nameof(LibraryCard.SuspensionEndDate)),
                    msg: isEng ? "Suspension end date must be in future" : "Ngày kết thúc lớn hơn hiện tại");
            }
            if (reason.Length > 250)
            {
                customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                    key: StringUtils.ToCamelCase(nameof(LibraryCard.SuspensionReason)),
                    msg: isEng 
                        ? "Suspension reason cannot exceed than 250 characters" 
                        : "Nguyên nhân không được quá 250 ký tự");
            }

            if (customErrs.Any()) throw new UnprocessableEntityException("Invalid data", customErrs);

            // Check exist library card
            var existingEntity = await _unitOfWork.Repository<LibraryCard, Guid>().GetByIdAsync(libraryCardId);
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "library card" : "thẻ thư viện"));
            }
            if (existingEntity.Status == LibraryCardStatus.Pending ||
                existingEntity.Status == LibraryCardStatus.Rejected)
            {
                // Cannot update library card status to {0} as {1}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0009);
                return new ServiceResult(ResultCodeConst.LibraryCard_Warning0009,
                    StringUtils.Format(errMsg, isEng ? "suspended" : "bị cấm",
                        isEng ? "card has not activated yet" : "thẻ thư viện chưa được kích hoạt"));
            }
            if (existingEntity.Status == LibraryCardStatus.Suspended)
            {
                // Cannot update library card status to {0} as {1}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0009);
                return new ServiceResult(ResultCodeConst.LibraryCard_Warning0009,
                    StringUtils.Format(errMsg, isEng ? "suspended" : "bị cấm",
                        isEng ? "this card has already suspended" : "thẻ ở trạng trái đã bị cấm"));
            }

            // Update props
            existingEntity.Status = LibraryCardStatus.Suspended;
            existingEntity.SuspensionEndDate = suspensionEndDate;
            existingEntity.SuspensionReason = reason;

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
                // Cannot update library card status to {0} as {1}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0009);
                return new ServiceResult(ResultCodeConst.LibraryCard_Warning0009,
                    StringUtils.Format(errMsg, isEng ? "active" : "hoạt động",
                        isEng ? "this card has not suspended yet" : "thẻ không ở trạng trái đã bị cấm"));
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

    public async Task<IServiceResult> DeleteCardWithoutSaveChangesAsync(Guid libraryCardId)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<LibraryCard>(u => u.LibraryCardId == libraryCardId);
            // Retrieve with spec
            var existingEntity = await _unitOfWork.Repository<LibraryCard, Guid>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);	
            }
				
            // Do not allow to remove card when status is not as pending or rejected, and cardholder is not created from employee
            if (existingEntity.Status != LibraryCardStatus.Pending && 
                existingEntity.Status != LibraryCardStatus.Rejected &&
                existingEntity.Users.Any(u => !u.IsEmployeeCreated))
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);	
            }   
            
            // Process update
            await _unitOfWork.Repository<LibraryCard, Guid>().DeleteAsync(existingEntity.LibraryCardId);
				
            // Mark as delete success
            return new ServiceResult(ResultCodeConst.SYS_Success0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004), true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when delete card without save changes");
        }
    }
    
    public async Task<IServiceResult> DeleteRangeCardWithoutSaveChangesAsync(Guid[] libraryCardIds)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<LibraryCard>(u => libraryCardIds.Contains(u.LibraryCardId));
            // Retrieve with spec
            var entities = await _unitOfWork.Repository<LibraryCard, Guid>().GetAllWithSpecAsync(baseSpec);
            // Convert to list
            var cardList = entities.ToList();

            foreach (var card in cardList)
            {
                // Do not allow to remove card when status is not as pending or rejected, cardholder is not created from employee
                if (card.Status != LibraryCardStatus.Pending && 
                    card.Status != LibraryCardStatus.Rejected && 
                    card.Users.Any(u => !u.IsEmployeeCreated))
                {
                    return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);	
                }
            }   
            
            // Process update
            await _unitOfWork.Repository<LibraryCard, Guid>().DeleteRangeAsync(
                cardList.Select(c => c.LibraryCardId).ToArray());
				
            // Mark as delete success
            return new ServiceResult(ResultCodeConst.SYS_Success0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004), true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when delete card without save changes");
        }
    }

    private async Task<bool> SendAcceptedEmailAsync(string email, LibraryCardDto cardDto, 
        string libName, string libContact)
    {
        try
        {
            // Email subject 
            var subject = "[ELIBRARY] Thẻ Thư Viện Đã Được Duyệt";
                            
            // Progress send confirmation email
            var emailMessageDto = new EmailMessageDto( // Define email message
                // Define Recipient
                to: new List<string>() { email },
                // Define subject
                subject: subject,
                // Add email body content
                content: GetLibraryCardAcceptedEmailBody(
                    cardDto: cardDto,
                    libName: libName,
                    libContact:libContact)
            );
                            
            // Process send email
            return await _emailSvc.SendEmailAsync(emailMessageDto, isBodyHtml: true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process send library card activated email");
        }
    }
    
    private async Task<bool> SendActivatedEmailAsync(string email, LibraryCardDto cardDto,
        TransactionDto transactionDto, string libName, string libContact)
    {
        try
        {
            // Email subject 
            var subject = "[ELIBRARY] Thẻ Thư Viện Đã Kích Hoạt";
                            
            // Progress send confirmation email
            var emailMessageDto = new EmailMessageDto( // Define email message
                // Define Recipient
                to: new List<string>() { email },
                // Define subject
                subject: subject,
                // Add email body content
                content: GetLibraryCardActivatedEmailBody(
                    cardDto: cardDto,
                    transactionDto: transactionDto,
                    libName: libName,
                    libContact:libContact)
            );
                            
            // Process send email
            return await _emailSvc.SendEmailAsync(emailMessageDto, isBodyHtml: true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process send library card activated email");
        }
    }

    private async Task<bool> SendRejectEmailAsync(string email, LibraryCardDto cardDto,
        string rejectReason, string libName, string libContact)
    {
        try
        {
            // Email subject 
            var subject = "[ELIBRARY] Thẻ Thư Viện Bị Từ Chối";
                            
            // Progress send confirmation email
            var emailMessageDto = new EmailMessageDto( // Define email message
                // Define Recipient
                to: new List<string>() { email },
                // Define subject
                subject: subject,
                // Add email body content
                content: GetLibraryCardRejectEmailBody(
                    cardDto: cardDto,
                    rejectReason: rejectReason,
                    libName: libName,
                    libContact:libContact)
            );
                            
            // Process send email
            return await _emailSvc.SendEmailAsync(emailMessageDto, isBodyHtml: true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process send library card reject email");
        }
    }
    
    private string GetLibraryCardActivatedEmailBody(
         LibraryCardDto cardDto, TransactionDto transactionDto, string libName, string libContact)
     {
         var culture = new CultureInfo("vi-VN");
         
         return $$"""
                  <!DOCTYPE html>
                  <html>
                  <head>
                      <meta charset="UTF-8">
                      <title>Thông Báo Kích Hoạt Thẻ Thư Viện</title>
                      <style>
                          body {
                              font-family: Arial, sans-serif;
                              line-height: 1.6;
                              color: #333;
                          }
                          .header {
                              font-size: 18px;
                              color: #2c3e50;
                              font-weight: bold;
                          }
                          .details {
                              margin: 15px 0;
                              padding: 10px;
                              background-color: #f9f9f9;
                              border-left: 4px solid #27ae60;
                          }
                          .details li {
                              margin: 5px 0;
                          }
                          .barcode {
                              color: #2980b9;
                              font-weight: bold;
                          }
                          .expiry-date {
                              color: #27ae60;
                              font-weight: bold;
                          }
                          .status-label {
                              color: #e74c3c;
                              font-weight: bold;
                          }
                          .status-text {
                              color: #f39c12;
                              font-weight: bold;
                          }
                          .footer {
                              margin-top: 20px;
                              font-size: 14px;
                              color: #7f8c8d;
                          }
                      </style>
                  </head>
                  <body>
                      <p class="header">Thông Báo Kích Hoạt Thẻ Thư Viện</p>
                      <p>Xin chào {{cardDto.FullName}},</p>
                      <p>Thẻ thư viện của bạn đã được kích hoạt thành công. Vui lòng chờ để được xét duyệt.</p>
                      
                      <p><strong>Chi Tiết Thẻ Thư Viện:</strong></p>
                      <div class="details">
                          <ul>
                              <li><span class="barcode">Mã Thẻ Thư Viện:</span> {{cardDto.Barcode}}</li>
                              <li><span class="expiry-date">Ngày Hết Hạn:</span> {{cardDto.ExpiryDate:MM/dd/yyyy}}</li>
                              <li><span class="status-label">Trạng Thái Hiện Tại:</span> <span class="status-text">{{cardDto.Status.GetDescription()}}</span></li>
                          </ul>
                      </div>
                      
                      <p><strong>Chi Tiết Giao Dịch:</strong></p>
                      <div class="details">
                          <ul>
                              <li><strong>Mã Giao Dịch:</strong> {{transactionDto.TransactionCode}}</li>
                              <li><strong>Ngày Giao Dịch:</strong> {{transactionDto.TransactionDate:MM/dd/yyyy}}</li>
                              <li><strong>Số Tiền Đã Thanh Toán:</strong> {{transactionDto.Amount.ToString("C0", culture)}}</li>
                              <li><strong>Phương Thức Thanh Toán:</strong> {{transactionDto.PaymentMethod?.MethodName ?? TransactionMethod.Cash.GetDescription()}}</li>
                              <li><strong>Trạng Thái Giao Dịch:</strong> {{transactionDto.TransactionStatus.GetDescription()}}</li>
                          </ul>
                      </div>
                      
                      <p><strong>Chi Tiết Gói Thẻ Thư Viện:</strong></p>
                      <div class="details">
                          <ul>
                              <li><strong>Tên Gói:</strong> {{transactionDto.LibraryCardPackage?.PackageName}}</li>
                              <li><strong>Thời Gian Hiệu Lực:</strong> {{transactionDto.LibraryCardPackage?.DurationInMonths}} tháng</li>
                              <li><strong>Giá:</strong> {{transactionDto.LibraryCardPackage?.Price.ToString("C0", culture)}}</li>
                              <li><strong>Mô Tả:</strong> {{transactionDto.LibraryCardPackage?.Description}}</li>
                          </ul>
                      </div>
                      
                      <p>Nếu bạn có bất kỳ câu hỏi nào hoặc cần hỗ trợ, vui lòng liên hệ với chúng tôi qua số <strong>{{libContact}}</strong>.</p>
                      
                      <p><strong>Trân trọng,</strong></p>
                      <p>{{libName}}</p>
                  </body>
                  </html>
                  """;
     }
    
    private string GetLibraryCardRejectEmailBody(
        LibraryCardDto cardDto, string rejectReason, string libName, string libContact)
    {
        return $$"""
                 <!DOCTYPE html>
                 <html>
                 <head>
                     <meta charset="UTF-8">
                     <title>Thông Báo Từ Chối Đăng Ký Thẻ Thư Viện</title>
                     <style>
                         body {
                             font-family: Arial, sans-serif;
                             line-height: 1.6;
                             color: #333;
                         }
                         .header {
                             font-size: 18px;
                             color: #c0392b;
                             font-weight: bold;
                         }
                         .details {
                             margin: 15px 0;
                             padding: 10px;
                             background-color: #f9f9f9;
                             border-left: 4px solid #e74c3c;
                         }
                         .details li {
                             margin: 5px 0;
                         }
                         .reason {
                             color: #e74c3c;
                             font-weight: bold;
                         }
                         .footer {
                             margin-top: 20px;
                             font-size: 14px;
                             color: #7f8c8d;
                         }
                     </style>
                 </head>
                 <body>
                     <p class="header">Thông Báo Từ Chối Đăng Ký Thẻ Thư Viện</p>
                     <p>Xin chào {{cardDto.FullName}},</p>
                     <p>Chúng tôi rất tiếc thông báo rằng đơn đăng ký thẻ thư viện của bạn không thể được chấp nhận vào thời điểm này.</p>
                     
                     <p><strong>Lý Do Từ Chối:</strong></p>
                     <div class="details">
                         <p class="reason">{{rejectReason}}</p>
                     </div>
                     
                     <p>Vui lòng sửa chữa thông tin và gửi lại yêu cầu xác thực thẻ và đợi thư viện phản hồi<p>
                     
                     <p>Nếu bạn cần thêm thông tin hoặc có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi qua số <strong>{{libContact}}</strong>.</p>
                     
                     <p><strong>Trân trọng,</strong></p>
                     <p>{{libName}}</p>
                 </body>
                 </html>
                 """;
    }
    
    private string GetLibraryCardAcceptedEmailBody(
         LibraryCardDto cardDto, string libName, string libContact)
     {
         return $$"""
                  <!DOCTYPE html>
                  <html>
                  <head>
                      <meta charset="UTF-8">
                      <title>Thông Báo Thẻ Thư Viện Đã Được Duyệt</title>
                      <style>
                          body {
                              font-family: Arial, sans-serif;
                              line-height: 1.6;
                              color: #333;
                          }
                          .header {
                              font-size: 18px;
                              color: #2c3e50;
                              font-weight: bold;
                          }
                          .details {
                              margin: 15px 0;
                              padding: 10px;
                              background-color: #f9f9f9;
                              border-left: 4px solid #27ae60;
                          }
                          .details li {
                              margin: 5px 0;
                          }
                          .barcode {
                              color: #2980b9;
                              font-weight: bold;
                          }
                          .expiry-date {
                              color: #27ae60;
                              font-weight: bold;
                          }
                          .status-label {
                              color: #e74c3c;
                              font-weight: bold;
                          }
                          .status-text {
                              color: #f39c12;
                              font-weight: bold;
                          }
                          .footer {
                              margin-top: 20px;
                              font-size: 14px;
                              color: #7f8c8d;
                          }
                      </style>
                  </head>
                  <body>
                      <p class="header">Thông Báo Thẻ Thư Viện Đã Được Duyệt</p>
                      <p>Xin chào {{cardDto.FullName}},</p>
                      <p>Thẻ thư viện của bạn đã được duyệt thành công. Bạn có thể sử dụng tất cả các dịch vụ của thư viện mà không bị gián đoạn.</p>
                      
                      <p><strong>Chi Tiết Thẻ Thư Viện:</strong></p>
                      <div class="details">
                          <ul>
                              <li><span class="barcode">Mã Thẻ Thư Viện:</span> {{cardDto.Barcode}}</li>
                              <li><span class="expiry-date">Ngày Hết Hạn:</span> {{cardDto.ExpiryDate:MM/dd/yyyy}}</li>
                              <li><span class="status-label">Trạng Thái Hiện Tại:</span> <span class="status-text">{{cardDto.Status.GetDescription()}}</span></li>
                          </ul>
                      </div>
                      
                      <p>Nếu bạn có bất kỳ câu hỏi nào hoặc cần hỗ trợ, vui lòng liên hệ với chúng tôi qua số <strong>{{libContact}}</strong>.</p>
                      
                      <p><strong>Trân trọng,</strong></p>
                      <p>{{libName}}</p>
                  </body>
                  </html>
                  """;
     }
    
    private async Task<bool> SendCardExtensionSuccessEmailAsync(string email, LibraryCardDto cardDto,
			TransactionDto transactionDto, string libName, string libContact)
	{
		try
		{
			// Email subject 
			var subject = "[ELIBRARY] Gia hạn thẻ thư viện thành công";
	                        
			// Progress send confirmation email
			var emailMessageDto = new EmailMessageDto( // Define email message
				// Define Recipient
				to: new List<string>() { email },
				// Define subject
				subject: subject,
				// Add email body content
				content: GetLibraryCardExtensionEmailBody(
					cardDto: cardDto,
					transactionDto: transactionDto,
					libName: libName,
					libContact:libContact)
			);
	                        
			// Process send email
			return await _emailSvc.SendEmailAsync(emailMessageDto, isBodyHtml: true);
		}
		catch (Exception ex)
		{
			_logger.Error(ex.Message);
			throw new Exception("Error invoke when process send library card activated email");
		}
	}
		
	private string GetLibraryCardExtensionEmailBody(LibraryCardDto cardDto, TransactionDto transactionDto, string libName, string libContact)
	{
		var culture = new CultureInfo("vi-VN");

		return $$"""
		         <!DOCTYPE html>
		         <html>
		         <head>
		             <meta charset="UTF-8">
		             <title>Thông Báo Gia Hạn Thẻ Thư Viện</title>
		             <style>
		                 body {
		                     font-family: Arial, sans-serif;
		                     line-height: 1.6;
		                     color: #333;
		                 }
		                 .header {
		                     font-size: 18px;
		                     color: #2c3e50;
		                     font-weight: bold;
		                 }
		                 .details {
		                     margin: 15px 0;
		                     padding: 10px;
		                     background-color: #f9f9f9;
		                     border-left: 4px solid #27ae60;
		                 }
		                 .details li {
		                     margin: 5px 0;
		                 }
		                 .barcode {
		                     color: #2980b9;
		                     font-weight: bold;
		                 }
		                 .expiry-date {
		                     color: #27ae60;
		                     font-weight: bold;
		                 }
		                 .status-label {
		                     color: #e74c3c;
		                     font-weight: bold;
		                 }
		                 .status-text {
		                     color: #f39c12;
		                     font-weight: bold;
		                 }
		                 .footer {
		                     margin-top: 20px;
		                     font-size: 14px;
		                     color: #7f8c8d;
		                 }
		             </style>
		         </head>
		         <body>
		             <p class="header">Thông Báo Gia Hạn Thẻ Thư Viện</p>
		             <p>Xin chào {{cardDto.FullName}},</p>
		             <p>Chúc mừng! Thẻ thư viện của bạn đã được gia hạn thành công. Bạn có thể tiếp tục sử dụng tất cả các dịch vụ của thư viện.</p>
		             
		             <p><strong>Thông Tin Thẻ Thư Viện:</strong></p>
		             <div class="details">
		                 <ul>
		                     <li><span class="barcode">Mã Thẻ Thư Viện:</span> {{cardDto.Barcode}}</li>
		                     <li><span class="expiry-date">Ngày Hết Hạn Mới:</span> {{cardDto.ExpiryDate:MM/dd/yyyy}}</li>
		                     <li><span class="status-label">Trạng Thái:</span> <span class="status-text">{{cardDto.Status.GetDescription()}}</span></li>
		                 </ul>
		             </div>
		             
		             <p><strong>Chi Tiết Giao Dịch Gia Hạn:</strong></p>
		             <div class="details">
		                 <ul>
		                     <li><strong>Mã Giao Dịch:</strong> {{transactionDto.TransactionCode}}</li>
		                     <li><strong>Ngày Giao Dịch:</strong> {{transactionDto.TransactionDate:MM/dd/yyyy}}</li>
		                     <li><strong>Số Tiền Đã Thanh Toán:</strong> {{transactionDto.Amount.ToString("C0", culture)}}</li>
		                     <li><strong>Phương Thức Thanh Toán:</strong> {{transactionDto.PaymentMethod?.MethodName ?? TransactionMethod.Cash.GetDescription()}}</li>
		                     <li><strong>Trạng Thái Giao Dịch:</strong> {{transactionDto.TransactionStatus.GetDescription()}}</li>
		                 </ul>
		             </div>
		             
		             <p>Nếu bạn có bất kỳ câu hỏi nào hoặc cần hỗ trợ, vui lòng liên hệ với chúng tôi qua số <strong>{{libContact}}</strong>.</p>
		             
		             <p><strong>Trân trọng,</strong></p>
		             <p>{{libName}}</p>
		         </body>
		         </html>
		         """;
	}
}