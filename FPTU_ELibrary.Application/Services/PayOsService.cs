using System.Text;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Dtos.Payments.PayOS;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Hubs;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Constants;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Net.payOS;
using Net.payOS.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using Transaction = FPTU_ELibrary.Domain.Entities.Transaction;

namespace FPTU_ELibrary.Application.Services;

public class PayOsService : IPayOsService
{
    private const string Hub_Method = "VerifyPaymentStatus";
    
    private readonly ILogger _logger;
    private readonly IFineService<FineDto> _fineService;
    private readonly IUserService<UserDto> _userService;
    private readonly ISystemMessageService _msgService;
    private readonly ITransactionService<TransactionDto> _transactionService;
    
    private readonly PayOSSettings _payOsSettings;
    private readonly WebTokenSettings _webTokenSettings;
    private readonly ILibraryCardService<LibraryCardDto> _libCardService;
    private readonly IDigitalBorrowService<DigitalBorrowDto> _digitalBorrowService;

    private readonly IHubContext<PaymentHub> _hubContext;

    public PayOsService(
        ILogger logger,
        IHubContext<PaymentHub> hubContext,
        ISystemMessageService msgService,
        IFineService<FineDto> fineService,
        IUserService<UserDto> userService,
        ILibraryCardService<LibraryCardDto> libCardService,
        ITransactionService<TransactionDto> transactionService,
        IDigitalBorrowService<DigitalBorrowDto> digitalBorrowService,
        IOptionsMonitor<PayOSSettings> monitor,
        IOptionsMonitor<WebTokenSettings> monitor1)
    {
        _logger = logger;
        _hubContext = hubContext;
        _msgService = msgService;
        _fineService = fineService;
        _userService = userService;
        _libCardService = libCardService;
        _transactionService = transactionService;
        _digitalBorrowService = digitalBorrowService;
        _payOsSettings = monitor.CurrentValue;
        _webTokenSettings = monitor1.CurrentValue;
    }
    
    public async Task<IServiceResult> GetLinkInformationAsync(string paymentLinkId)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Process get payment link information
            var response = await PayOsPaymentRequestExtensions.GetLinkInformationAsync(
                paymentLinkId: paymentLinkId,
                payOsConfig: _payOsSettings);

            if (response.Item1) // Get successfully
            {
                return new ServiceResult(ResultCodeConst.SYS_Success0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001), response.Item3);
            }
            
            // Not found 
            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
            return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                StringUtils.Format(errMsg, isEng ? "payment link" : "phiên thanh toán"));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get payment link information");
        }
    }

    public async Task<IServiceResult> VerifyPaymentWebhookDataAsync(PayOSPaymentLinkInformationResponseDto req)
    {
        // Determine current system language
        var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
            LanguageContext.CurrentLanguage);
        var isEng = lang == SystemLanguage.English;
    
        PayOS payOs = new PayOS(_payOsSettings.ClientId, _payOsSettings.ApiKey, _payOsSettings.ChecksumKey);
    
        // Initiate transaction detail
        string transactionCode = req.Data.OrderCode;
        DateTime transactionDate = DateTime.MinValue;
        string? cancellationReason = null;
        DateTime? cancelledAt = null;
        TransactionStatus status = TransactionStatus.Pending;
        
        // Initialize collection to check existing different user
        var userIds = new List<Guid>();
        // Retrieve all transaction by code
        var transactionDtos =
            (await _transactionService.GetAllByTransactionCodeAsync(transactionCode: transactionCode)).Data as List<TransactionDto>;
        if (transactionDtos == null || !transactionDtos.Any())
        {
            // Msg: Not found any transaction match to verify
            return new ServiceResult(ResultCodeConst.Transaction_Fail0005,
                await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0005));
        }
        else
        {
            foreach (var transaction in transactionDtos)
            {
                // Add user id if not exist
                if(!userIds.Contains(transaction.UserId)) userIds.Add(transaction.UserId);
                
                // Check whether transaction is from other user
                if (userIds.Count > 1)
                {
                    // Msg: Failed to verify payment transaction
                    return new ServiceResult(ResultCodeConst.Transaction_Fail0006,
                        await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0006));
                }
            }
        }
        
        // Determine transaction status in request data
        switch (req.Data.Status)
        {
            case PayOSTransactionStatusConstants.Pending:
                // Update transaction status
                status = TransactionStatus.Pending;
                break;
            case PayOSTransactionStatusConstants.Expired:
                // Update transaction status
                status = TransactionStatus.Expired;
                break;
            case PayOSTransactionStatusConstants.Cancelled:
                // Update transaction status
                status = TransactionStatus.Cancelled;
                cancellationReason = req.Data.CancellationReason;
                cancelledAt = !string.IsNullOrEmpty(req.Data.CanceledAt) ? DateTime.Parse(req.Data.CanceledAt) : null;
                break;
            case PayOSTransactionStatusConstants.Paid:
                try
                {
                    // // Initiate Webhook type
                    // WebhookType webhookType = new(
                    //     req.Code,
                    //     req.Desc,
                    //     new WebhookData(
                    //         orderCode: long.Parse(req.Data.OrderCode),
                    //         amount: (int)req.Data.Transactions[0].Amount,
                    //         description: req.Data.Transactions[0].Description,
                    //         accountNumber: req.Data.Transactions[0].AccountNumber,
                    //         reference: req.Data.Transactions[0].Reference ?? string.Empty,
                    //         transactionDateTime: req.Data.Transactions[0].TransactionDateTime,
                    //         currency: "VND",
                    //         paymentLinkId: req.Data.Id,
                    //         code: req.Code,
                    //         desc: req.Desc,
                    //         counterAccountBankId: req.Data.Transactions[0].CounterAccountBankId,
                    //         counterAccountBankName: req.Data.Transactions[0].CounterAccountBankName,
                    //         counterAccountName: req.Data.Transactions[0].CounterAccountName,
                    //         counterAccountNumber: req.Data.Transactions[0].CounterAccountNumber,
                    //         virtualAccountName: req.Data.Transactions[0].VirtualAccountName,
                    //         virtualAccountNumber: req.Data.Transactions[0].VirtualAccountNumber ?? string.Empty
                    //     ),
                    //     await req.GenerateWebhookSignatureAsync(req.Data.Id, _payOsSettings.ChecksumKey));
                    //
                    // // Verify payment webhook data
                    // WebhookData webhookData = payOs.verifyPaymentWebhookData(webhookType);
                    //
                    // // Update transaction status to DB
                    // if (webhookData != null!) // Success status
                    // {
                    // }
                    
                    // Initialize different datetime format
                    string[] formats = { 
                        "yyyy-MM-dd HH:mm:ss",      
                        "yyyy-MM-ddTHH:mm:sszzz" 
                    };
                    
                    DateTimeOffset parsedDateTimeOffset = DateTimeOffset.ParseExact(
                        req.Data.Transactions[0].TransactionDateTime, formats, null);
                    // Transaction datetime
                    transactionDate = parsedDateTimeOffset.DateTime;
                    // Transaction status
                        status = TransactionStatus.Paid;
                }
                catch (ForbiddenException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message);
                    throw new Exception("Error invoke when verify payment webhook data information");
                }
                break;
        }
        
        // Try to get user by id
        var userDto = (await _userService.GetByIdAsync(userIds.First())).Data as UserDto;
        if (userDto == null)
        {
            // Msg: Failed to verify payment transaction
            return new ServiceResult(ResultCodeConst.Transaction_Fail0006,
                await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0006));
        }
        
        // Process update transaction status by code (include all transactions with the same code) 
        var updateRes = await _transactionService.UpdateStatusByTransactionCodeAsync(
            transactionCode: transactionCode,
            transactionDate: transactionDate,
            cancellationReason: cancellationReason,
            cancelledAt: cancelledAt,
            status: status);
        if (updateRes.Data is false)
        {
            // Msg: Failed to verify payment transaction
            return new ServiceResult(ResultCodeConst.Transaction_Fail0006,
                await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0006));
        }

        if (transactionDate != DateTime.MinValue && status == TransactionStatus.Paid)
        {
            // Process generate payment token
            var paymentToken = await new PaymentUtils(_logger)
                .GenerateTransactionTokenAsync(email: userDto.Email,
                    transactionCode: transactionCode,
                    transactionDate: transactionDate,
                    webTokenSettings: _webTokenSettings);
            
            // Initialize success message
            var successMsg = string.Empty;
            // Iterate each transaction to process confirm payment
            foreach (var transaction in transactionDtos)
            {
                IServiceResult confirmRes = new ServiceResult();
                // Determine transaction type
                switch (transaction.TransactionType)
                {
                    case TransactionType.Fine:
                        confirmRes = await _fineService.ConfirmFineAsync(
                            email: userDto.Email,
                            transactionToken: paymentToken);
                        break;
                    case TransactionType.DigitalBorrow:
                        confirmRes = await _digitalBorrowService.ConfirmDigitalBorrowAsync(
                            email: userDto.Email,
                            transactionToken: paymentToken);
                        
                        // Assign success message
                        successMsg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Success0004);
                        break;
                    case TransactionType.DigitalExtension:
                        confirmRes = await _digitalBorrowService.ConfirmDigitalExtensionAsync(
                            email: userDto.Email,
                            transactionToken: paymentToken);
                        
                        // Assign success message
                        successMsg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Success0005);
                        break;
                    case TransactionType.LibraryCardRegister:
                        confirmRes = await _libCardService.ConfirmCardRegisterAsync(
                            email: userDto.Email,
                            transactionToken: paymentToken);
                        
                        // Assign success message
                        successMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Success0002);
                        if (!userDto.IsEmployeeCreated) // Not add custom message when not created by employee
                        {
                            successMsg += isEng 
                                ? ". Please wait library to confirm your card register" 
                                : ". Vui lòng đợi để được thư viện xác nhận";
                        }
                        break;
                    case TransactionType.LibraryCardExtension:
                        confirmRes = await _libCardService.ConfirmCardExtensionAsync(
                            email: userDto.Email,
                            transactionToken: paymentToken);
                        
                        // Assign success message
                        successMsg = await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Success0005);
                        break;
                }
                
                if (confirmRes.Data is false) return confirmRes;
            }   
       
            // Determine which user to send
            var emailToSend = !string.IsNullOrEmpty(transactionDtos[0].CreatedBy) &&
                              !Equals(userDto.Email, transactionDtos[0].CreatedBy) // Created by is different 
                ? transactionDtos[0].CreatedBy // Prioritize to select created by
                : userDto.Email; // Otherwise get user's email
            // Send payment status to realtime hub
            if (emailToSend != null)
            {
                await _hubContext.Clients.User(emailToSend).SendAsync(Hub_Method, new
                {
                    Message = successMsg,
                    Status = status
                });
            }
            
            // Msg: Verify payment transaction successfully
            return new ServiceResult(ResultCodeConst.Transaction_Success0002, successMsg);
        }
        
        // Msg: Failed to verify payment transaction
        return new ServiceResult(ResultCodeConst.Transaction_Fail0006,
            await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0006));
    }

    public async Task<IServiceResult> CancelPaymentFromWebhookResponseAsync(PayOSPaymentLinkInformationResponseDto req)
    {
        try
        {
            PayOS payOs = new PayOS(_payOsSettings.ClientId, _payOsSettings.ApiKey, _payOsSettings.ChecksumKey);
    
            // Initiate transaction detail
            string transactionCode = req.Data.OrderCode;
            
            // Try to retrieve transaction
            var transactionSpec = new BaseSpecification<Transaction>(t => 
                Equals(t.TransactionCode, transactionCode));
            // Apply include
            transactionSpec.ApplyInclude(q => 
                q.Include(t => t.User)
            );
            var transactionDto = (await _transactionService.GetWithSpecAsync(transactionSpec)).Data as TransactionDto;
            if (transactionDto == null)
            {
                // Msg: Failed to cancel payment transaction
                return new ServiceResult(ResultCodeConst.Transaction_Fail0007,
                    await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0007));
            }
            
            // Initiate Webhook type
            WebhookType webhookType = new(
                req.Code,
                req.Desc,
                new WebhookData(
                    orderCode: long.Parse(req.Data.OrderCode),
                    amount: (int)req.Data.Transactions[0].Amount,
                    description: req.Data.Transactions[0].Description,
                    accountNumber: req.Data.Transactions[0].AccountNumber,
                    reference: req.Data.Transactions[0].Reference ?? string.Empty,
                    transactionDateTime: req.Data.Transactions[0].TransactionDateTime,
                    currency: "VND",
                    paymentLinkId: req.Data.Id,
                    code: req.Code,
                    desc: req.Desc,
                    counterAccountBankId: req.Data.Transactions[0].CounterAccountBankId,
                    counterAccountBankName: req.Data.Transactions[0].CounterAccountBankName,
                    counterAccountName: req.Data.Transactions[0].CounterAccountName,
                    counterAccountNumber: req.Data.Transactions[0].CounterAccountNumber,
                    virtualAccountName: req.Data.Transactions[0].VirtualAccountName,
                    virtualAccountNumber: req.Data.Transactions[0].VirtualAccountNumber ?? string.Empty
                ),
                await req.GenerateWebhookSignatureAsync(req.Data.Id, _payOsSettings.ChecksumKey));

            // Verify payment webhook data
            WebhookData webhookData = payOs.verifyPaymentWebhookData(webhookType);

            // Update transaction status to DB
            if (webhookData != null!) // Success status
            {
                // Process change transaction status to expired by code
                var cancelRes = await _transactionService.CancelTransactionsByCodeAsync(
                    transactionCode: transactionCode,
                    cancellationReason: req.Data.CancellationReason ?? string.Empty);
                // Msg: Cancel payment transaction successfully
                if (cancelRes.ResultCode == ResultCodeConst.Transaction_Success0003)
                {
                    // Send payment status to realtime hub
                    await _hubContext.Clients.User(transactionDto.User.Email).SendAsync(Hub_Method, new
                    {
                        Messsage = cancelRes,
                        Status = TransactionStatus.Cancelled
                    });
                }
                
                return cancelRes;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process cancel payment from webhook response");
        }
        
        // Msg: Failed to cancel payment transaction
        return new ServiceResult(ResultCodeConst.Transaction_Fail0007,
            await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0007));
    }
    
    public async Task<IServiceResult> CancelPaymentAsync(string paymentLinkId, string orderCode, string cancellationReason)
    {
        try
        {
            // Determine current system language
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Retrieve transaction by code
            var transactionSpec = new BaseSpecification<Transaction>(t => t.TransactionCode == orderCode);
            // Apply include user
            transactionSpec.ApplyInclude(q => q.Include(t => t.User));
            var transactionDto = (await _transactionService.GetWithSpecAsync(transactionSpec)).Data as TransactionDto;
            if (transactionDto == null || transactionDto.TransactionStatus == TransactionStatus.Cancelled)
            {
                // Msg: Failed to cancel payment transaction
                return new ServiceResult(ResultCodeConst.Transaction_Fail0007,
                    await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0007));
            }
            
            // Initiate HttpClient
            using HttpClient httpClient = new();
            
            // Add header parameters
            httpClient.DefaultRequestHeaders.Add("x-client-id", _payOsSettings.ClientId);
            httpClient.DefaultRequestHeaders.Add("x-api-key", _payOsSettings.ApiKey);
            
            // Initialize payOS cancellation request
            PayOSCancelPaymentRequestDto cancelReq = new PayOSCancelPaymentRequestDto()
            {
                CancellationReason = cancellationReason
            };
            // Convert request data to type of JSON
            var requestData = JsonConvert.SerializeObject(cancelReq, new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented
            });
            // Initiate string content with serialized request data, encoding and media type
            var requestContent = new StringContent(
                content: requestData,
                encoding: Encoding.UTF8,
                mediaType: "application/json");
            // Add params to Url by formating string
            var cancelPaymentUrl = string.Format(_payOsSettings.CancelPaymentUrl, paymentLinkId);
            // Execute POST request with uri and request content
            var cancelPaymentUrlRes = await httpClient.PostAsync(
                requestUri: cancelPaymentUrl,
                content: requestContent);
            
            // Response content
            var content = cancelPaymentUrlRes.Content.ReadAsStringAsync().Result;
            var responseData = JsonConvert.DeserializeObject<PayOSPaymentLinkInformationResponseDto>(content);
            // Check for response content not found 
            if (responseData == null || responseData.Data == null!)
            {
                // Msg: Failed to cancel payment transaction
                return new ServiceResult(ResultCodeConst.Transaction_Fail0007,
                    await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0007));
            }
            
            if (cancelPaymentUrlRes.IsSuccessStatusCode)
            {
                // Process change transaction status to expired by code
                var cancelRes = await _transactionService.CancelTransactionsByCodeAsync(
                    transactionCode: orderCode,
                    cancellationReason: cancellationReason);
                // Msg: Cancel payment transaction successfully
                if (cancelRes.ResultCode == ResultCodeConst.Transaction_Success0003)
                {
                    // Send payment status to realtime hub
                    await _hubContext.Clients.User(transactionDto.User.Email).SendAsync(Hub_Method, new
                    {
                        Messsage = cancelRes,
                        Status = TransactionStatus.Cancelled
                    });
                }

                return cancelRes;
            }
            
            // Msg: Failed to cancel payment transaction
            return new ServiceResult(ResultCodeConst.Transaction_Fail0007,
                await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0007));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process cancel payment");
        }
    }
}