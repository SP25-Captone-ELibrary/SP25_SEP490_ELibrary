using System.Globalization;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Dtos.Payments;
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

public class FineService : GenericService<Fine, FineDto, int>, IFineService<FineDto>
{
    private readonly AppSettings _appSettings;
    
    private readonly IEmailService _emailSvc;
    private readonly IUserService<UserDto> _userService;
    private readonly ITransactionService<TransactionDto> _transactionService;
    
    private readonly TokenValidationParameters _tokenValidationParameters;

    public FineService(
        ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IEmailService emailSvc,
        IUserService<UserDto> userService,
        ITransactionService<TransactionDto>transactionService,
        TokenValidationParameters tokenValidationParameters,
        IOptionsMonitor<AppSettings> monitor,
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _appSettings = monitor.CurrentValue;
        
        _emailSvc = emailSvc;
        _userService = userService;
        _transactionService = transactionService;
        _tokenValidationParameters = tokenValidationParameters;
    }

    public async Task<IServiceResult> ConfirmFineAsync(string email, string transactionToken)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Msg: Failed to pay for fines as {0}
            var confirmFailedMsg = await _msgService.GetMessageAsync(ResultCodeConst.Fine_Fail0001);
            
            // Retrieve user information
            // Build spec
            var userBaseSpec = new BaseSpecification<User>(u => Equals(u.Email, email));
            // Retrieve user with spec
            var userDto = (await _userService.GetWithSpecAsync(userBaseSpec)).Data as UserDto;
            if (userDto == null || userDto.LibraryCardId == null) // Not found user
            {
                // Logging
                _logger.Information("Not found user to process confirm digital borrow");
                // Mark as failed to update
                return new ServiceResult(ResultCodeConst.Fine_Fail0001,
                    StringUtils.Format(confirmFailedMsg, isEng ? "not found user" : "không tìm thấy bạn đọc"), false);
            }
            
            // Initialize payment utils
            var paymentUtils = new PaymentUtils(logger: _logger);
            // Validate transaction token
            var validatedToken = await paymentUtils.ValidateTransactionTokenAsync(
                token: transactionToken,
                tokenValidationParameters: _tokenValidationParameters);
            if (validatedToken == null) 
            {
                // Logging
                _logger.Information("Token is invalid, cannot process confirm digital borrow");
                // Mark as failed to update
                return new ServiceResult(ResultCodeConst.Fine_Fail0001,
                    StringUtils.Format(confirmFailedMsg, isEng 
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
                return new ServiceResult(ResultCodeConst.Fine_Fail0001,
                    StringUtils.Format(confirmFailedMsg, isEng 
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
                t.FineId != null && // payment for specific fine
                t.TransactionStatus == TransactionStatus.Paid && // must be paid
                t.TransactionType == TransactionType.Fine && // transaction type is Fine
                Equals(t.TransactionCode, transCode)); // transaction code
            // Retrieve all with spec
            var transactions = (await _transactionService.GetAllWithSpecAndSelectorAsync(
                transSpec, selector: s => new Transaction()
                {
                    TransactionId = s.TransactionId,
                    TransactionCode = s.TransactionCode,
                    UserId = s.UserId,
                    Amount = s.Amount,
                    Description = s.Description,
                    TransactionStatus = s.TransactionStatus,
                    TransactionType = s.TransactionType,
                    TransactionDate = s.TransactionDate,
                    ExpiredAt = s.ExpiredAt,
                    CreatedAt = s.CreatedAt,
                    CreatedBy = s.CreatedBy,
                    CancelledAt = s.CancelledAt,
                    CancellationReason = s.CancellationReason,
                    FineId = s.FineId,
                    ResourceId = s.ResourceId,
                    LibraryCardPackageId = s.LibraryCardPackageId,
                    TransactionMethod = s.TransactionMethod,
                    PaymentMethodId = s.PaymentMethodId,
                    QrCode = s.QrCode,
                    Fine = s.Fine,
                })).Data as List<Transaction>;
            if (transactions == null || !transactions.Any())
            {
                // Logging 
                _logger.Information("Not found transaction information to process confirm digital borrow");
                return new ServiceResult(ResultCodeConst.Fine_Fail0001,
                    StringUtils.Format(confirmFailedMsg, isEng 
                        ? "not found payment transaction" 
                        : "không tìm thấy phiên thanh toán"), false);
            }
            
            // Iterate each transaction
            foreach (var trans in transactions)
            {
                if(!Equals(trans.TransactionDate?.Date, transDate.Date))
                {
                    // Logging 
                    _logger.Information("Transaction date is not match with token claims while process confirm digital borrow");
                    return new ServiceResult(ResultCodeConst.Fine_Fail0001,
                        StringUtils.Format(confirmFailedMsg, isEng 
                            ? "transaction date is invalid" 
                            : "ngày thanh toán không hợp lệ"), false);
                }
                else if (trans.Fine == null || 
                         trans.FineId == null ||
                         trans.FineId == 0)
                {
                    // Logging 
                    _logger.Information("Not found library resource in payment transaction to process confirm digital borrow");
                    return new ServiceResult(ResultCodeConst.LibraryCard_Fail0006,
                        StringUtils.Format(confirmFailedMsg, isEng 
                            ? "not found fine in payment transaction information" 
                            : "không tìm thấy thông tin phí phạt trong thông tin thanh toán"), false);
                }
            }
            
            // Initialize list of fine
            var fineToSendEmailList = new List<Fine>();
            // Extract all ids in transaction
            var fineIds = transactions
                .Select(t => t.FineId).ToList();
            // Process update fines' status
            foreach (var fineId in fineIds)
            {
                // Build spec
                var fineSpec = new BaseSpecification<Fine>(f => f.FineId == fineId);
                // Apply include
                fineSpec.ApplyInclude(q => q.Include(q => q.FinePolicy));
                if (int.TryParse(fineId.ToString(), out var validId) && 
                    await _unitOfWork.Repository<Fine, int>().GetWithSpecAsync(fineSpec) is Fine existingEntity)
                {
                    // Change status
                    existingEntity.Status = FineStatus.Paid;
                    // Process update status
                    await _unitOfWork.Repository<Fine, int>().UpdateAsync(existingEntity);
                    // Add to list fine to handle sending email
                    fineToSendEmailList.Add(existingEntity);
                }
            }
            
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
            if (isSaved)
            {
                // Process send email
                await SendFinePaymentSuccessEmailAsync(
                    user: userDto,
                    fines: _mapper.Map<List<FineDto>>(fineToSendEmailList),
                    libName: _appSettings.LibraryName,
                    libContact: _appSettings.LibraryContact);
                
                // Msg: Paid for {0} fines successfully
                var msg = await _msgService.GetMessageAsync(ResultCodeConst.Fine_Success0001);
                return new ServiceResult(ResultCodeConst.Fine_Success0001,
                    StringUtils.Format(msg, fineIds.Count.ToString()));
            }
            
            // Msg: Failed to pay for fines
            return new ServiceResult(ResultCodeConst.Fine_Fail0002,
                await _msgService.GetMessageAsync(ResultCodeConst.Fine_Fail0002));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process confirm fine payment");
        }
    }
    
    private async Task<bool> SendFinePaymentSuccessEmailAsync(
        UserDto user, List<FineDto> fines,
        string libName, string libContact)
    {
        try
        {
            // Email subject
            var subject = "[ELIBRARY] Thông báo thanh toán phí phạt thành công";
        
            // Process send email
            var emailMessageDto = new EmailMessageDto( // Define email message
                // Define Recipient
                to: new List<string>() { user.Email },
                // Define subject
                subject: subject,
                // Add email body content
                content: GetFinePaymentSuccessEmailBody(
                    user: user,
                    fines: fines,
                    libName: libName,
                    libContact:libContact)
            );
			
            // Process send email
            return await _emailSvc.SendEmailAsync(emailMessageDto, isBodyHtml: true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process send email for fine payment success");
        }
    }
    
    private string GetFinePaymentSuccessEmailBody(UserDto user, List<FineDto> fines, string libName, string libContact)
    {
        // Initialize vietnamese culture info
        var culture = new CultureInfo("vi-VN");
        
        var fineDetails = string.Join("", fines.Select(fine => 
           $"""
           <li>
               <p><strong>Số Tiền Phạt:</strong> <span class="status-text">{fine.FineAmount.ToString("C0", culture)}</span></p>
               <p><strong>Nội Dung Phí Phạt:</strong> <span class="title">{fine.FinePolicy.FinePolicyTitle}</span></p>
               <p><strong>Loại Phí Phạt:</strong> {fine.FinePolicy.ConditionType.GetDescription()}</p>
               <p><strong>Ghi Chú:</strong> {(string.IsNullOrEmpty(fine.FineNote) ? "Không có" : fine.FineNote)}</p>
               <p><strong>Trạng Thái:</strong> <span class="paid-status">{fine.Status.GetDescription()}</span></p>
           </li>
           """));

        return $$"""
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset="UTF-8">
                    <title>Thông Báo Thanh Toán Phí Phạt Thành Công</title>
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
                        .details ul {
                            list-style-type: disc;
                            padding-left: 20px;
                        }
                        .details li {
                            margin: 5px 0;
                        }
                        .status-text {
                            color: #c0392b;
                            font-weight: bold;
                        }
                        .footer {
                            margin-top: 20px;
                            font-size: 14px;
                            color: #7f8c8d;
                        }
                        .title {
                            color: #2980b9;
                            font-weight: bold;
                        }
                        .paid-status {
                            color: #27ae60;
                            font-weight: bold;
                        }
                    </style>
                </head>
                <body>
                    <p class="header">Thông Báo Thanh Toán Phí Phạt Thành Công</p>
                    <p>Xin chào {{user.LastName}} {{user.FirstName}},</p>
                    <p>Chúng tôi xin thông báo rằng việc thanh toán các khoản phí phạt của bạn đã được xử lý thành công.</p>
                    
                    <p><strong>Chi Tiết Phí Phạt:</strong></p>
                    <div class="details">
                        <ul>
                            {{fineDetails}}
                        </ul>
                    </div>
                    
                    <p>Nếu bạn có bất kỳ thắc mắc nào, xin vui lòng liên hệ với thư viện qua: <strong>{{libContact}}</strong>.</p>
                    
                    <p>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>
                    
                    <p class="footer"><strong>Trân trọng,</strong></p>
                    <p class="footer">{{libName}}</p>
                </body>
                </html>
                """;
    }
    
    #region Archived Code
    // public async Task<IServiceResult> CreateFineForBorrowRecord(int finePolicyId, int borrowRecordId,string email)
    // {
    //     try
    //     {
    //         // Determine current system language
    //         var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
    //             LanguageContext.CurrentLanguage);
    //         var isEng = lang == SystemLanguage.English;
    //         
    //         var employeeBaseSpec = new BaseSpecification<Employee>(u => u.Email.Equals(email));
    //         //get user
    //         var employee = await _employeeService.GetWithSpecAsync(employeeBaseSpec);
    //         if (employee.Data is null)
    //             return new ServiceResult(ResultCodeConst.SYS_Warning0002,
    //                     StringUtils.Format(ResultCodeConst.SYS_Warning0002, isEng ? "employee" : "nhân viên"));
    //         var employeeValue = (EmployeeDto)employee.Data!;
    //         FineDto dto = new FineDto()
    //         {
    //             FinePolicyId = finePolicyId,
    //             BorrowRecordId = borrowRecordId,
    //             CreatedAt = DateTime.Now,
    //             CreatedBy = employeeValue.EmployeeId,
    //             ExpiryAt = DateTime.Now.AddDays(1),
    //             Status = TransactionStatus.Pending.ToString()
    //         };
    //         var entity = _mapper.Map<Fine>(dto);
    //         await _unitOfWork.Repository<Fine, int>().AddAsync(entity);
    //         if (await _unitOfWork.SaveChangesAsync() <= 0)
    //         {
    //             return new ServiceResult(ResultCodeConst.SYS_Fail0001,
    //                 await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
    //         }
    //
    //         var extendEntity = new BaseSpecification<Fine>(f => f.FineId == entity.FineId);
    //         extendEntity.EnableSplitQuery();
    //         extendEntity.ApplyInclude(q => q.Include(f => f.FinePolicy));
    //         var borrowRecordSpecBase = new BaseSpecification<BorrowRecord>(br => br.BorrowRecordId == borrowRecordId);
    //         borrowRecordSpecBase.EnableSplitQuery();
    //         borrowRecordSpecBase.ApplyInclude(q =>
    //             q.Include(br => br.LibraryCard)
    //                 .ThenInclude(li => li.Users)
    //                 .Include(br => br.BorrowRecordDetails)
    //                 .ThenInclude(brd => brd.LibraryItemInstance)
    //                 .ThenInclude(lii => lii.LibraryItem));
    //         var borrow = await _borrowRecordService.Value.GetWithSpecAsync(borrowRecordSpecBase);
    //         if (borrow.Data is null)
    //             return new ServiceResult(ResultCodeConst.SYS_Warning0002,
    //                 StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002),
    //                     isEng ? "borrow record" : "lịch sử mượn trả"));
    //         var borrowValue = (BorrowRecordDto)borrow.Data!;
    //         TransactionDto response = new TransactionDto();
    //         response.TransactionCode = Guid.NewGuid().ToString();
    //         // fine caused by damaged or lost would base on the amount of item
    //         response.Amount =
    //             borrowValue.BorrowRecordDetails.Sum(brd => brd.LibraryItemInstance.LibraryItem.EstimatedPrice) ?? 0;
    //         response.TransactionType = TransactionType.Fine;
    //         response.UserId = borrowValue.LibraryCard.Users.First().UserId;
    //         response.TransactionStatus = TransactionStatus.Pending;
    //         response.FineId = entity.FineId;
    //         response.CreatedAt = DateTime.Now;
    //         // response.PaymentMethodId = 1;
    //         var transactionEntity = _mapper.Map<Transaction>(response);
    //         var result = await _transactionService.CreateAsync(transactionEntity);
    //         if(result.Data is null) return result;
    //
    //         return new ServiceResult(ResultCodeConst.SYS_Success0001,
    //             await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001));
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.Error(ex.Message);
    //         throw new Exception("Error invoke when process create fine");
    //     }
    // }
    #endregion
}