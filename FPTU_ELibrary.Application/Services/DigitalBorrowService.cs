using System.Globalization;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class DigitalBorrowService : GenericService<DigitalBorrow, DigitalBorrowDto, int>,
    IDigitalBorrowService<DigitalBorrowDto>
{
    private readonly Lazy<IUserService<UserDto>> _userSvc;
    private readonly Lazy<ITransactionService<TransactionDto>> _transactionSvc;
    
    private readonly IEmailService _emailSvc;
    
    private readonly AppSettings _appSettings;
    private readonly TokenValidationParameters _tokenValidationParams;

    public DigitalBorrowService(
        // Lazy services
        Lazy<IUserService<UserDto>> userSvc,
        Lazy<ITransactionService<TransactionDto>> transactionSvc,
        
        IEmailService emailSvc,
        IOptionsMonitor<AppSettings> monitor,
        TokenValidationParameters tokenValidationParams,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _emailSvc = emailSvc;
        _userSvc = userSvc;
        _transactionSvc = transactionSvc;
        _appSettings = monitor.CurrentValue;
        _tokenValidationParams = tokenValidationParams;
    }

    public async Task<IServiceResult> GetAllCardHolderDigitalBorrowByUserIdAsync(Guid userId, int pageIndex, int pageSize)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<DigitalBorrow>(br => br.UserId == userId);   
            
            // Add default order by
            baseSpec.AddOrderByDescending(br => br.RegisterDate);
            
            // Count total borrow request
            var totalDigitalWithSpec = await _unitOfWork.Repository<DigitalBorrow, int>().CountAsync(baseSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalDigitalWithSpec / pageSize);

            // Set pagination to specification after count total digital borrow
            if (pageIndex > totalPage
                || pageIndex < 1) // Exceed total page or page index smaller than 1
            {
                pageIndex = 1; // Set default to first page
            }
            // Apply pagination
            baseSpec.ApplyPaging(skip: pageSize * (pageIndex - 1), take: pageSize);
            
            // Retrieve data with spec
            var entities = await _unitOfWork.Repository<DigitalBorrow, int>()
                .GetAllWithSpecAsync(baseSpec);
            if (entities.Any())
            {
                var digitalDtos = _mapper.Map<List<DigitalBorrowDto>>(entities);
                
                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<LibraryCardHolderDigitalBorrowDto>(
                    digitalDtos.Select(d => d.ToCardHolderDigitalBorrowDto()),
                    pageIndex, pageSize, totalPage, totalDigitalWithSpec);

                // Response with pagination 
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }
            
            // Data not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                new List<LibraryCardHolderDigitalBorrowDto>());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when get all digital borrow by user id");
        }
    }

    public async Task<IServiceResult> GetCardHolderDigitalBorrowByIdAsync(Guid userId, int digitalBorrowId)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Retrieve user information
            // Build spec
            var userBaseSpec = new BaseSpecification<User>(u => Equals(u.UserId, userId));
            // Apply include
            userBaseSpec.ApplyInclude(q => q
                .Include(u => u.LibraryCard)!
            );
            var userDto = (await _userSvc.Value.GetWithSpecAsync(userBaseSpec)).Data as UserDto;
            if (userDto == null || userDto.LibraryCardId == null) // Not found user
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                // Data not found or empty
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    StringUtils.Format(errMsg, isEng ? "patron" : "bạn đọc"));
            }
            
            // Build spec
            var baseSpec = new BaseSpecification<DigitalBorrow>(br => 
                br.UserId == userDto.UserId && br.DigitalBorrowId == digitalBorrowId);
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(db => db.LibraryResource)
            );
            // Retrieve with spec
            var existingEntity = await _unitOfWork.Repository<DigitalBorrow, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity != null)
            {
                // Convert to dto
                var digitalBorrowDto = _mapper.Map<DigitalBorrowDto>(existingEntity);
                
                // Get data successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                    digitalBorrowDto.ToCardHolderDigitalBorrowDto());
            }
            
            // Not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
        }
        catch (Exception ex)
        {
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
            
            var transCode = tokenExtractedData.TransactionCode;
            var transDate = tokenExtractedData.TransactionDate;
            // Retrieve transaction
            // Build spec
            var transSpec = new BaseSpecification<Transaction>(t =>
                t.TransactionDate != null && // with specific date
                t.UserId == userDto.UserId && // who request
                t.ResourceId != null && // payment for specific resource
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
            else if(!Equals(transactionDto.TransactionDate?.Date, transDate.Date))
            {
                // Logging 
                _logger.Information("Transaction date is not match with token claims while process confirm digital borrow");
                return new ServiceResult(ResultCodeConst.Borrow_Fail0003,
                    StringUtils.Format(registerFailedMsg, isEng 
                        ? "transaction date is invalid" 
                        : "ngày thanh toán không hợp lệ"), false);
            }
            
            if (transactionDto.LibraryResource == null)
            {
                // Logging 
                _logger.Information("Not found library resource in payment transaction to process confirm digital borrow");
                return new ServiceResult(ResultCodeConst.LibraryCard_Fail0006,
                    StringUtils.Format(registerFailedMsg, isEng 
                        ? "not found library resource in payment transaction information" 
                        : "không tìm thấy thông tin tài liệu điện tử trong thông tin thanh toán để tạo đăng ký mượn"), false);
            }
            
            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Initialize digital borrow
            var digitalBorrowDto = new DigitalBorrowDto()
            {
                ResourceId = transactionDto.LibraryResource.ResourceId,
                Status = BorrowDigitalStatus.Active,
                UserId = userDto.UserId,
                RegisterDate = currentLocalDateTime,
                ExpiryDate = currentLocalDateTime.AddDays(transactionDto.LibraryResource.DefaultBorrowDurationDays),
                ExtensionCount = 0,
                IsExtended = false
            };
            
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
            else if(!Equals(transactionDto.TransactionDate?.Date, transDate.Date))
            {
                // Logging 
                _logger.Information("Transaction date is not match with token claims while process extend digital borrow");
                return new ServiceResult(ResultCodeConst.Borrow_Fail0005,
                    StringUtils.Format(registerFailedMsg, isEng 
                        ? "transaction date is invalid" 
                        : "ngày thanh toán không hợp lệ"), false);
            }
            
            if (transactionDto.LibraryResource == null)
            {
                // Logging 
                _logger.Information("Not found library resource in payment transaction to process extend digital borrow");
                return new ServiceResult(ResultCodeConst.LibraryCard_Fail0006,
                    StringUtils.Format(registerFailedMsg, isEng 
                        ? "not found library resource in payment transaction information" 
                        : "không tìm thấy thông tin tài liệu điện tử trong thông tin thanh toán để tiến hành gia hạn"), false);
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
            var remainDays = existingEntity.ExpiryDate.Subtract(currentLocalDateTime);
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
            // Change status to active
            existingEntity.Status = BorrowDigitalStatus.Active;
            // Increase extension count
            existingEntity.ExtensionCount++;
            // Mark as extend
            existingEntity.IsExtended = true;
            
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

                     <p>Nếu bạn có bất kỳ câu hỏi nào hoặc cần hỗ trợ, vui lòng liên hệ với chúng tôi qua email <strong>{{libContact}}</strong>.</p>
                     
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

                     <p>Nếu bạn có bất kỳ câu hỏi nào hoặc cần hỗ trợ, vui lòng liên hệ với chúng tôi qua email <strong>{{libContact}}</strong>.</p>
                     
                     <p><strong>Trân trọng,</strong></p>
                     <p>{{libName}}</p>
                 </body>
                 </html>
                 """;
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
    //             StringUtils.Format(errMsg, isEng ? "user" : "người dùng"));
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