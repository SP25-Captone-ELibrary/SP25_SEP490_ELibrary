using System.Globalization;
using System.Linq;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Hubs;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using MapsterMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Exception = System.Exception;

namespace FPTU_ELibrary.Application.Services;

public class DigitalBorrowService : GenericService<DigitalBorrow, DigitalBorrowDto, int>,
    IDigitalBorrowService<DigitalBorrowDto>
{
    private readonly Lazy<IUserService<UserDto>> _userSvc;
    private readonly Lazy<ITransactionService<TransactionDto>> _transactionSvc;
    private readonly Lazy<ILibraryResourceService<LibraryResourceDto>> _resourceSvc;
	private readonly Lazy<IDigitalBorrowService<DigitalBorrowDto>> _digitalBorrowSvc;

	private readonly IEmailService _emailSvc;

    private readonly AppSettings _appSettings;
    private readonly TokenValidationParameters _tokenValidationParams;
    private readonly ILibraryResourceService<LibraryResourceDto> _libraryResourceService;
    private readonly IServiceProvider _service;

    public DigitalBorrowService(
        // Lazy services
        Lazy<IUserService<UserDto>> userSvc,
        Lazy<ITransactionService<TransactionDto>> transactionSvc,
        Lazy<ILibraryResourceService<LibraryResourceDto>> resourceSvc,
        Lazy<IDigitalBorrowService<DigitalBorrowDto>> digitalBorrowSvc,
        IServiceProvider service,
        IEmailService emailSvc,
        IOptionsMonitor<AppSettings> monitor,
        TokenValidationParameters tokenValidationParams,
        ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILibraryResourceService<LibraryResourceDto> libraryResourceService,
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _emailSvc = emailSvc;
        _userSvc = userSvc;
        _resourceSvc = resourceSvc;
        _transactionSvc = transactionSvc;
        _digitalBorrowSvc = digitalBorrowSvc;
		_appSettings = monitor.CurrentValue;
        _tokenValidationParams = tokenValidationParams;
        _libraryResourceService = libraryResourceService;
        _service = service;
    }

    public override async Task<IServiceResult> GetAllWithSpecAsync(ISpecification<DigitalBorrow> specification,
        bool tracked = true)
    {
        try
        {
            // Try to parse specification to LibraryItemSpecification
            var digitalSpecification = specification as DigitalBorrowSpecification;
            // Check if specification is null
            if (digitalSpecification == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }

            // Count total library items
            var totalDigitalWithSpec =
                await _unitOfWork.Repository<DigitalBorrow, int>().CountAsync(digitalSpecification);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalDigitalWithSpec / digitalSpecification.PageSize);

            // Set pagination to specification after count total library item
            if (digitalSpecification.PageIndex > totalPage
                || digitalSpecification.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
                digitalSpecification.PageIndex = 1; // Set default to first page
            }

            // Apply pagination
            digitalSpecification.ApplyPaging(
                skip: digitalSpecification.PageSize * (digitalSpecification.PageIndex - 1),
                take: digitalSpecification.PageSize);

            var entities = await _unitOfWork.Repository<DigitalBorrow, int>()
                .GetAllWithSpecAsync(digitalSpecification, tracked);
            if (entities.Any())
            {
                // Map to dto
                var dtoList = _mapper.Map<List<DigitalBorrowDto>>(entities);

                // Convert to get digital borrow dto
                var getDigitalBorrowList = dtoList.Select(d => d.ToGetDigitalBorrowDto());

                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<GetDigitalBorrowDto>(getDigitalBorrowList,
                    digitalSpecification.PageIndex, digitalSpecification.PageSize, totalPage, totalDigitalWithSpec);

                // Get data successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }

            // Data not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                new List<GetDigitalBorrowDto>());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get all digital borrows");
        }
    }

    public async Task<IServiceResult> GetAllWithSpecFromDashboardAsync(ISpecification<DigitalBorrow> spec)
    {
        return await base.GetAllWithSpecAsync(spec);
    }

    public async Task<IServiceResult> GetByIdAsync(int id,
        string? email = null, Guid? userId = null, bool isCallFromManagement = false)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Build spec
            var baseSpec = new BaseSpecification<DigitalBorrow>(br => br.DigitalBorrowId == id);
            // Add filter (if any)
            if (!string.IsNullOrWhiteSpace(email))
            {
                baseSpec.AddFilter(db => db.User.Email == email);
            }

            if (userId.HasValue && userId != Guid.Empty)
            {
                baseSpec.AddFilter(db => db.UserId == userId);
            }

            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(db => db.LibraryResource)
                .Include(db => db.DigitalBorrowExtensionHistories)
            );
            // Retrieve with spec
            var existingEntity = await _unitOfWork.Repository<DigitalBorrow, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity != null)
            {
                // Map to dto
                var digitalBorrowDto = _mapper.Map<DigitalBorrowDto>(existingEntity);

                // Try to retrieve all transaction
                var tranSpec = new BaseSpecification<Transaction>(t =>
                    t.ResourceId == existingEntity.ResourceId); // Must equals to specific resource ids
                // Add filter
                if (isCallFromManagement) // Management
                {
                    tranSpec.AddFilter(t => t.TransactionStatus == TransactionStatus.Paid); // Only retrieve paid status
                }
                else // User
                {
                    tranSpec.AddFilter(t =>
                        (!string.IsNullOrEmpty(email) && t.User.Email == email) || // Exist any email match
                        (userId.HasValue && userId != Guid.Empty && t.UserId == userId)); // Exist any userId match
                }

                // Apply include 
                tranSpec.ApplyInclude(q => q.Include(t => t.User));
                // Retrieve all transaction dto with spec
                var transactionDtos =
                    (await _transactionSvc.Value.GetAllWithSpecAsync(tranSpec)).Data as List<TransactionDto>;

                // Covert to get digital borrow dto
                var getDigitalBorrowDto = digitalBorrowDto.ToGetDigitalBorrowDto(transactions: transactionDtos);

                // Get data successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), getDigitalBorrowDto);
            }

            // Not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get library card holder's digital borrow by id");
        }
    }

    public async Task<IServiceResult> ConfirmDigitalBorrowAsync(string email, string transactionToken)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Msg: Failed to register library digital resource as {0}
            var registerFailedMsg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Fail0003);

            // Retrieve user information
            // Build spec
            var userBaseSpec = new BaseSpecification<User>(u => Equals(u.Email, email));
            // Retrieve user with spec
            var userDto = (await _userSvc.Value.GetWithSpecAsync(userBaseSpec)).Data as UserDto;
            if (userDto == null || userDto.LibraryCardId == null) // Not found user
            {
                // Logging
                _logger.Information("Not found user to process confirm digital borrow");
                // Mark as failed to update
                return new ServiceResult(ResultCodeConst.Borrow_Fail0003,
                    StringUtils.Format(registerFailedMsg, isEng ? "not found user" : "không tìm thấy bạn đọc"), false);
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
                _logger.Information("Token is invalid, cannot process confirm digital borrow");
                // Mark as failed to update
                return new ServiceResult(ResultCodeConst.Borrow_Fail0003,
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
                _logger.Information("User's email is not match with token claims to process confirm digital borrow");
                return new ServiceResult(ResultCodeConst.Borrow_Fail0003,
                    StringUtils.Format(registerFailedMsg, isEng
                        ? "not found user's email"
                        : "không tìm thấy bạn đọc"), false);
            }

            // Retrieve all user digital borrows
            var digitalSpec = new BaseSpecification<DigitalBorrow>(d => d.UserId == userDto.UserId);
            var userDigitalBorrowIds = (await _digitalBorrowSvc.Value.GetAllWithSpecAndSelectorAsync(digitalSpec, d => d.ResourceId)).Data as List<int>;
            if (userDigitalBorrowIds == null) userDigitalBorrowIds = new();

            var transCode = tokenExtractedData.TransactionCode;
            var transDate = tokenExtractedData.TransactionDate;
            // Retrieve transaction
            // Build spec
            var transSpec = new BaseSpecification<Transaction>(t =>
                t.TransactionDate != null && // with specific date
                t.UserId == userDto.UserId && // who request
                ( 
                    t.ResourceId != null && // payment for specific resource
                    !userDigitalBorrowIds.Contains(t.ResourceId ?? 1)
                ) &&
                t.TransactionStatus == TransactionStatus.Paid && // must be paid
                t.TransactionType == TransactionType.DigitalBorrow && // transaction type is lib card register
                Equals(t.TransactionCode, transCode)); // transaction code
            // Apply include
            transSpec.ApplyInclude(q => q
                .Include(t => t.LibraryResource)
                .Include(t => t.PaymentMethod!)
            );
            // Retrieve with spec
            var transactionDto = (await _transactionSvc.Value.GetWithSpecAsync(transSpec)).Data as TransactionDto;
            if (transactionDto == null) // Not found any transaction match
            {
                // Logging 
                _logger.Information("Not found transaction information to process confirm digital borrow");
                return new ServiceResult(ResultCodeConst.Borrow_Fail0003,
                    StringUtils.Format(registerFailedMsg, isEng
                        ? "not found payment transaction"
                        : "không tìm thấy phiên thanh toán"), false);
            }
            else if (!Equals(transactionDto.TransactionDate?.Date, transDate.Date))
            {
                // Logging 
                _logger.Information(
                    "Transaction date is not match with token claims while process confirm digital borrow");
                return new ServiceResult(ResultCodeConst.Borrow_Fail0003,
                    StringUtils.Format(registerFailedMsg, isEng
                        ? "transaction date is invalid"
                        : "ngày thanh toán không hợp lệ"), false);
            }

            if (transactionDto.LibraryResource == null)
            {
                // Logging 
                _logger.Information(
                    "Not found library resource in payment transaction to process confirm digital borrow");
                return new ServiceResult(ResultCodeConst.LibraryCard_Fail0006,
                    StringUtils.Format(registerFailedMsg, isEng
                        ? "not found library resource in payment transaction information"
                        : "không tìm thấy thông tin tài liệu điện tử trong thông tin thanh toán để tạo đăng ký mượn"),
                    false);
            }

            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

            // Determine resource type
            Enum.TryParse(transactionDto.LibraryResource.ResourceType, true, out LibraryResourceType validResourceType);
            
            // Initialize digital borrow
            var digitalBorrowDto = new DigitalBorrowDto()
            {
                ResourceId = transactionDto.LibraryResource.ResourceId,
                Status = validResourceType == LibraryResourceType.Ebook ? BorrowDigitalStatus.Active : BorrowDigitalStatus.Prepared,
                UserId = userDto.UserId,
                RegisterDate = currentLocalDateTime,
                ExpiryDate = currentLocalDateTime.AddDays(transactionDto.LibraryResource.DefaultBorrowDurationDays),
                ExtensionCount = 0,
                IsExtended = false,
                S3WatermarkedName = null
            };

            // string? s3WatermarkedName = (await _libraryResourceService.WatermarkAudioAsyncFromAWS(transactionDto.LibraryResource.S3OriginalName,email)).Data! as string;
            // digitalBorrowDto.S3WatermarkedName = s3WatermarkedName;

            // Process add new digital borrow
            await _unitOfWork.Repository<DigitalBorrow, int>().AddAsync(_mapper.Map<DigitalBorrow>(digitalBorrowDto));
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                // Assign resource to digital borrow
                if (digitalBorrowDto.LibraryResource == null!)
                {
                    digitalBorrowDto.LibraryResource = transactionDto.LibraryResource;
                }

                // Send digital resource has been borrowed email
                await SendDigitalBorrowSuccessEmailAsync(
                    userDto: userDto,
                    transactionDto: transactionDto,
                    digitalBorrowDto: digitalBorrowDto,
                    libName: _appSettings.LibraryName,
                    libContact: _appSettings.LibraryContact);

                if (validResourceType == LibraryResourceType.AudioBook &&
                    transactionDto.LibraryResource.S3OriginalName != null)
                {
                    var backgroundTask = Task.Run(() => ProcessWatermarkItemTask(
                        s3OriginName: transactionDto.LibraryResource.S3OriginalName, 
                        email: email,
                        resourceId: transactionDto.LibraryResource.ResourceId));
                    var result = new ServiceResult(ResultCodeConst.Borrow_Success0004,
                        await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Success0004));
                    _ = backgroundTask;
                    return result;
                }

                // Msg: Register library digital resource success
                return new ServiceResult(ResultCodeConst.Borrow_Success0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Success0004));
            }

            // Msg: Failed to register library digital resource
            return new ServiceResult(ResultCodeConst.Borrow_Fail0004,
                await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Fail0004));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process confirm digital borrow");
        }
    }

    public async Task<IServiceResult> ConfirmDigitalExtensionAsync(string email, string transactionToken)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Msg: Failed to extend library digital resource as {0}
            var registerFailedMsg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Fail0005);

            // Retrieve user information
            // Build spec
            var userBaseSpec = new BaseSpecification<User>(u => Equals(u.Email, email));
            // Retrieve user with spec
            var userDto = (await _userSvc.Value.GetWithSpecAsync(userBaseSpec)).Data as UserDto;
            if (userDto == null || userDto.LibraryCardId == null) // Not found user
            {
                // Logging
                _logger.Information("Not found user to process extend digital borrow");
                // Mark as failed to update
                return new ServiceResult(ResultCodeConst.Borrow_Fail0005,
                    StringUtils.Format(registerFailedMsg, isEng ? "not found user" : "không tìm thấy bạn đọc"), false);
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
                _logger.Information("Token is invalid, cannot process extend digital borrow");
                // Mark as failed to update
                return new ServiceResult(ResultCodeConst.Borrow_Fail0005,
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
                _logger.Information("User's email is not match with token claims to process extend digital borrow");
                return new ServiceResult(ResultCodeConst.Borrow_Fail0005,
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
                t.ResourceId != null && // payment for specific resource
                t.TransactionStatus == TransactionStatus.Paid && // must be paid
                t.TransactionType == TransactionType.DigitalExtension && // transaction type is lib card register
                Equals(t.TransactionCode, transCode)); // transaction code
            // Apply include
            transSpec.ApplyInclude(q => q
                .Include(t => t.LibraryResource)
                .Include(t => t.PaymentMethod!)
            );
            // Retrieve with spec
            var transactionDto = (await _transactionSvc.Value.GetWithSpecAsync(transSpec)).Data as TransactionDto;
            if (transactionDto == null) // Not found any transaction match
            {
                // Logging 
                _logger.Information("Not found transaction information to process extend digital borrow");
                return new ServiceResult(ResultCodeConst.Borrow_Fail0005,
                    StringUtils.Format(registerFailedMsg, isEng
                        ? "not found payment transaction"
                        : "không tìm thấy phiên thanh toán"), false);
            }
            else if (!Equals(transactionDto.TransactionDate?.Date, transDate.Date))
            {
                // Logging 
                _logger.Information(
                    "Transaction date is not match with token claims while process extend digital borrow");
                return new ServiceResult(ResultCodeConst.Borrow_Fail0005,
                    StringUtils.Format(registerFailedMsg, isEng
                        ? "transaction date is invalid"
                        : "ngày thanh toán không hợp lệ"), false);
            }

            if (transactionDto.LibraryResource == null)
            {
                // Logging 
                _logger.Information(
                    "Not found library resource in payment transaction to process extend digital borrow");
                return new ServiceResult(ResultCodeConst.LibraryCard_Fail0006,
                    StringUtils.Format(registerFailedMsg, isEng
                        ? "not found library resource in payment transaction information"
                        : "không tìm thấy thông tin tài liệu điện tử trong thông tin thanh toán để tiến hành gia hạn"),
                    false);
            }

            // Check exist digital resource
            var digitalSpec = new BaseSpecification<DigitalBorrow>(db => db.UserId == userDto.UserId &&
                                                                         db.ResourceId == transactionDto.ResourceId);
            // Apply including digital resource
            digitalSpec.ApplyInclude(q => q.Include(db => db.LibraryResource));
            var existingEntity = await _unitOfWork.Repository<DigitalBorrow, int>().GetWithSpecAsync(digitalSpec);
            if (existingEntity == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.Borrow_Warning0017,
                    StringUtils.Format(errMsg, isEng
                        ? "digital borrow history to process extend expiration date"
                        : "lịch sử mượn của tài liệu điện tử này để tiến hành gia hạn"));
            }

            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

            // Extend expiration date
            // Subtract expiration date to now
            var oldExpiryDate = existingEntity.ExpiryDate;
            var remainDays = oldExpiryDate.Subtract(currentLocalDateTime);
            if (remainDays.Days > 0) // If expiry date still exceed than current date
            {
                existingEntity.ExpiryDate =
                    existingEntity.ExpiryDate.AddDays(transactionDto.LibraryResource.DefaultBorrowDurationDays);
            }
            else
            {
                existingEntity.ExpiryDate =
                    currentLocalDateTime.AddDays(transactionDto.LibraryResource.DefaultBorrowDurationDays);
            }

            // Determine resource type
            Enum.TryParse(existingEntity.LibraryResource.ResourceType, true, out LibraryResourceType validResourceType);
            
            // Change status to active
            existingEntity.Status = validResourceType == LibraryResourceType.Ebook ? BorrowDigitalStatus.Active : BorrowDigitalStatus.Prepared;
            // Mark as extend
            existingEntity.IsExtended = true;
            // Increase extension count
            existingEntity.ExtensionCount++;
            // Add extension history
            existingEntity.DigitalBorrowExtensionHistories.Add(new()
            {
                ExtensionDate = oldExpiryDate,
                NewExpiryDate = existingEntity.ExpiryDate,
                ExtensionFee = existingEntity.LibraryResource.BorrowPrice,
                ExtensionNumber = existingEntity.ExtensionCount
            });

            // Process update
            await _unitOfWork.Repository<DigitalBorrow, int>().UpdateAsync(existingEntity);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                // Send digital borrow extension success email
                await SendDigitalExtensionSuccessEmailAsync(
                    userDto: userDto,
                    transactionDto: transactionDto,
                    digitalBorrowDto: _mapper.Map<DigitalBorrowDto>(existingEntity),
                    extendDate: currentLocalDateTime,
                    libName: _appSettings.LibraryName,
                    libContact: _appSettings.LibraryContact);
                
                if (validResourceType == LibraryResourceType.AudioBook && 
                    existingEntity.LibraryResource.S3OriginalName != null)
                {
                    var backgroundTask = Task.Run(() => ProcessWatermarkItemTask(
                        s3OriginName: existingEntity.LibraryResource.S3OriginalName,
                        email: email,
                        resourceId: existingEntity.LibraryResource.ResourceId));
                    var result = new ServiceResult(ResultCodeConst.Borrow_Success0004,
                        await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Success0004));
                    _ = backgroundTask;
                    return result;
                }
                // Msg: Extend library digital resource success
                return new ServiceResult(ResultCodeConst.Borrow_Success0005,
                    await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Success0005));
            }

            // Msg: Failed to extend library digital resource expiration date
            return new ServiceResult(ResultCodeConst.Borrow_Fail0006,
                await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Fail0006));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process confirm digital resource borrow extension");
        }
    }

    private async Task<bool> SendDigitalBorrowSuccessEmailAsync(
        UserDto userDto,
        DigitalBorrowDto digitalBorrowDto, TransactionDto transactionDto,
        string libName, string libContact)
    {
        try
        {
            // Email subject 
            var subject = "[ELIBRARY] Mượn tài liệu điện tử thành công";

            // Progress send confirmation email
            var emailMessageDto = new EmailMessageDto( // Define email message
                // Define Recipient
                to: new List<string>() { userDto.Email },
                // Define subject
                subject: subject,
                // Add email body content
                content: GetDigitalBorrowSuccessEmailBody(
                    userDto: userDto,
                    digitalBorrowDto: digitalBorrowDto,
                    transactionDto: transactionDto,
                    libName: libName,
                    libContact: libContact)
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

    private async Task<bool> SendDigitalExtensionSuccessEmailAsync(
        UserDto userDto,
        DigitalBorrowDto digitalBorrowDto, TransactionDto transactionDto,
        DateTime extendDate, string libName, string libContact)
    {
        try
        {
            // Email subject 
            var subject = "[ELIBRARY] Gia hạn mượn tài liệu điện tử thành công";

            // Progress send confirmation email
            var emailMessageDto = new EmailMessageDto( // Define email message
                // Define Recipient
                to: new List<string>() { userDto.Email },
                // Define subject
                subject: subject,
                // Add email body content
                content: GetExtendDigitalBorrowEmailBody(
                    userDto: userDto,
                    digitalBorrowDto: digitalBorrowDto,
                    transactionDto: transactionDto,
                    extendDate: extendDate,
                    libName: libName,
                    libContact: libContact)
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

    private string GetDigitalBorrowSuccessEmailBody(
        UserDto userDto,
        DigitalBorrowDto digitalBorrowDto,
        TransactionDto transactionDto, string libName, string libContact)
    {
        var culture = new CultureInfo("vi-VN");

        return $$"""
                 <!DOCTYPE html>
                 <html>
                 <head>
                     <meta charset="UTF-8">
                     <title>Thông Báo Mượn Tài Liệu Điện Tử Thành Công</title>
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
                         .important {
                             color: #2980b9;
                             font-weight: bold;
                         }
                         .expiry-date {
                             color: #e74c3c;
                             font-weight: bold;
                         }
                         .status-label {
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
                     <p class="header">Thông Báo Mượn Tài Liệu Điện Tử Thành Công</p>
                     <p>Xin chào {{userDto.FirstName}} {{userDto.LastName}},</p>
                     <p>Bạn đã mượn thành công một tài nguyên số từ thư viện của chúng tôi.</p>
                     
                     <p><strong>Chi Tiết Mượn Tài Liệu Điện Tử:</strong></p>
                     <div class="details">
                         <ul>
                             <li><strong>Tên Tài Nguyên:</strong> {{digitalBorrowDto.LibraryResource.ResourceTitle}}</li>
                             <li><strong>Loại Tài Nguyên:</strong> {{digitalBorrowDto.LibraryResource.ResourceType}}</li>
                             <li><strong>Kích Thước:</strong> {{digitalBorrowDto.LibraryResource.ResourceSize?.ToString("N2")}} MB</li>
                             <li><strong>Định Dạng:</strong> {{digitalBorrowDto.LibraryResource.FileFormat}}</li>
                             <li><span class="important">Ngày Đăng Ký:</span> {{digitalBorrowDto.RegisterDate:dd/MM/yyyy}}</li>
                             <li><span class="expiry-date">Ngày Hết Hạn:</span> {{digitalBorrowDto.ExpiryDate:dd/MM/yyyy}}</li>
                             <li><span class="status-label">Trạng Thái:</span> {{digitalBorrowDto.Status.GetDescription()}}</li>
                         </ul>
                     </div>
                 
                     <p><strong>Chi Tiết Giao Dịch:</strong></p>
                     <div class="details">
                         <ul>
                             <li><strong>Mã Giao Dịch:</strong> {{transactionDto.TransactionCode}}</li>
                             <li><strong>Ngày Giao Dịch:</strong> {{transactionDto.TransactionDate:dd/MM/yyyy}}</li>
                             <li><strong>Số Tiền Đã Thanh Toán:</strong> {{transactionDto.Amount.ToString("C0", culture)}}</li>
                             <li><strong>Phương Thức Thanh Toán:</strong> {{transactionDto.PaymentMethod?.MethodName ?? TransactionMethod.Cash.GetDescription()}}</li>
                             <li><strong>Trạng Thái Giao Dịch:</strong> {{transactionDto.TransactionStatus.GetDescription()}}</li>
                         </ul>
                     </div>
                 
                     <p>Nếu bạn có bất kỳ câu hỏi nào hoặc cần hỗ trợ, vui lòng liên hệ với chúng tôi qua email: <strong>{{libContact}}</strong>.</p>
                     
                     <p><strong>Trân trọng,</strong></p>
                     <p>{{libName}}</p>
                 </body>
                 </html>
                 """;
    }

    private string GetExtendDigitalBorrowEmailBody(
        UserDto userDto,
        DigitalBorrowDto digitalBorrowDto,
        TransactionDto transactionDto,
        DateTime extendDate, string libName, string libContact)
    {
        var culture = new CultureInfo("vi-VN");

        return $$"""
                 <!DOCTYPE html>
                 <html>
                 <head>
                     <meta charset="UTF-8">
                     <title>Thông Báo Gia Hạn Mượn Tài Liệu Điện Tử</title>
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
                         .important {
                             color: #2980b9;
                             font-weight: bold;
                         }
                         .expiry-date {
                             color: #e74c3c;
                             font-weight: bold;
                         }
                         .status-label {
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
                     <p class="header">Thông Báo Gia Hạn Mượn Tài Liệu Điện Tử</p>
                     <p>Xin chào {{userDto.FirstName}} {{userDto.LastName}},</p>
                     <p>Chúng tôi xin thông báo rằng bạn đã gia hạn thành công tài liệu điện tử mà bạn đã mượn từ thư viện.</p>
                 
                     <p><strong>Chi Tiết Gia Hạn Mượn Tài Liệu:</strong></p>
                     <div class="details">
                         <ul>
                             <li><strong>Tên Tài Nguyên:</strong> {{digitalBorrowDto.LibraryResource.ResourceTitle}}</li>
                             <li><strong>Loại Tài Nguyên:</strong> {{digitalBorrowDto.LibraryResource.ResourceType}}</li>
                             <li><strong>Kích Thước:</strong> {{digitalBorrowDto.LibraryResource.ResourceSize?.ToString("N2")}} MB</li>
                             <li><strong>Định Dạng:</strong> {{digitalBorrowDto.LibraryResource.FileFormat}}</li>
                             <li><span class="important">Ngày Gia Hạn:</span> {{extendDate:dd/MM/yyyy}}</li>
                             <li><span class="expiry-date">Ngày Hết Hạn Mới:</span> {{digitalBorrowDto.ExpiryDate:dd/MM/yyyy}}</li>
                             <li><span class="status-label">Trạng Thái:</span> {{digitalBorrowDto.Status.GetDescription()}}</li>
                         </ul>
                     </div>
                 
                     <p><strong>Chi Tiết Giao Dịch Gia Hạn:</strong></p>
                     <div class="details">
                         <ul>
                             <li><strong>Mã Giao Dịch:</strong> {{transactionDto.TransactionCode}}</li>
                             <li><strong>Ngày Giao Dịch:</strong> {{transactionDto.TransactionDate:dd/MM/yyyy}}</li>
                             <li><strong>Số Tiền Thanh Toán:</strong> {{transactionDto.Amount.ToString("C0", culture)}}</li>
                             <li><strong>Phương Thức Thanh Toán:</strong> {{transactionDto.PaymentMethod?.MethodName ?? TransactionMethod.Cash.GetDescription()}}</li>
                             <li><strong>Trạng Thái Giao Dịch:</strong> {{transactionDto.TransactionStatus.GetDescription()}}</li>
                         </ul>
                     </div>
                 
                     <p>Nếu bạn có bất kỳ câu hỏi nào hoặc cần hỗ trợ, vui lòng liên hệ với chúng tôi qua email: <strong>{{libContact}}</strong>.</p>
                     
                     <p><strong>Trân trọng,</strong></p>
                     <p>{{libName}}</p>
                 </body>
                 </html>
                 """;
    }

    private async Task ProcessWatermarkItemTask(string s3OriginName, string email, int resourceId)
    {
        // Initialize scope
        using var scope = _service.CreateScope();
        // Inject library resource service
        var libraryResourceService = scope.ServiceProvider.GetRequiredService<ILibraryResourceService<LibraryResourceDto>>();
        // Inject unit of work
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        // Inject hub service
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<DigitalBorrowHub>>();
        
        // Process applying watermark audio
        var s3WatermarkedName =
            (await libraryResourceService.WatermarkAudioAsyncFromAWS(s3OriginName, email)).Data as string;
        
        // Retrieve library resource
        var baseSpec = new BaseSpecification<LibraryResource>(l =>
            l.ResourceId == resourceId && l.S3OriginalName == s3OriginName);
        var resource = (await libraryResourceService.GetWithSpecAsync(baseSpec)).Data as LibraryResourceDto;
        if (resource is null)
        {
            await hubContext.Clients.User(email).SendAsync("WatermarkAudioResult",
                "Error: Cannot find resource to update watermark audio");
        }
        
        // Retrieve user digital borrow
        var digitalSpec = new BaseSpecification<DigitalBorrow>(db => db.User.Email.Equals(email)
                                                                     && db.LibraryResource.ResourceId == resourceId);
        digitalSpec.ApplyInclude(q => q
            .Include(db => db.LibraryResource)
            .Include(db => db.User)
        );
        var digitalBorrow = await unitOfWork.Repository<DigitalBorrow, int>().GetWithSpecAsync(digitalSpec);
        if (digitalBorrow is null)
        {
            await hubContext.Clients.User(email).SendAsync("WatermarkAudioResult",
                "Error: Cannot find digital borrow to update watermark audio");
        }
        else
        {
            // Assign s3 watermarked name
            digitalBorrow.S3WatermarkedName = s3WatermarkedName;
            digitalBorrow.Status = BorrowDigitalStatus.Active;
            
            // Process update
            await unitOfWork.Repository<DigitalBorrow, int>().UpdateAsync(digitalBorrow);
            var result = await unitOfWork.SaveChangesAsync();
            if (result > 0)
            {
                await hubContext.Clients.User(email).SendAsync("WatermarkAudioResult",
                    "Success: Watermark audio successfully");
            }
            else
            {
                await hubContext.Clients.User(email).SendAsync("WatermarkAudioResult",
                    "Error: Cannot update watermark audio");
            }
        }
    }

    #region Archived Code

    // public async Task<IServiceResult> CreateTransactionForDigitalBorrow(string email, int resourceId)
    // {
    //     // Determine current system lang
    //     var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
    //         LanguageContext.CurrentLanguage);
    //     var isEng = lang == SystemLanguage.English;
    //
    //     // Get User By email
    //     var userBaseSpec = new BaseSpecification<User>(u => u.Email == email);
    //     var user = await _userSvc.Value.GetWithSpecAsync(userBaseSpec);
    //     if (user.Data is null)
    //     {
    //         var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
    //         return new ServiceResult(ResultCodeConst.SYS_Warning0002,
    //             StringUtils.Format(errMsg, isEng ? "user" : "bạn đọc"));
    //     }
    //
    //     var userValue = (UserDto)user.Data!;
    //     //Create Borrow Record
    //     DigitalBorrowDto newRecord = new DigitalBorrowDto()
    //     {
    //         UserId = userValue.UserId,
    //         ExtensionCount = 0,
    //         ResourceId = resourceId,
    //         Status = BorrowDigitalStatus.Active,
    //         RegisterDate = DateTime.Now,
    //         IsExtended = false,
    //         ExpiryDate = DateTime.Now.AddMonths(1)
    //     };
    //     var entity = _mapper.Map<DigitalBorrow>(newRecord);
    //     await _unitOfWork.Repository<DigitalBorrow, int>().AddAsync(entity);
    //     var addStatus = await _unitOfWork.SaveChangesAsync();
    //     if (await _unitOfWork.SaveChangesAsync() <= 0)
    //     {
    //         return new ServiceResult(ResultCodeConst.SYS_Fail0001,
    //             await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
    //     }
    //
    //     TransactionDto transaction = new TransactionDto();
    //     transaction.TransactionCode = Guid.NewGuid().ToString();
    //     transaction.TransactionType = TransactionType.DigitalBorrow;
    //     transaction.UserId = userValue.UserId;
    //     transaction.DigitalBorrowId = entity.DigitalBorrowId;
    //     var transactionEntity = _mapper.Map<Transaction>(transaction);
    //     var result = await _transactionService.Value.CreateAsync(transactionEntity);
    //     if(result.Data is null) return result;
    //
    //     return new ServiceResult(ResultCodeConst.SYS_Success0001,
    //         await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001));
    // }

    #endregion
}