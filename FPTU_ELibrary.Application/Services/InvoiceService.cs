using System.Text;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Dtos.Payments.PayOS;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Application.Dtos.Payments.PayOS;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Net.payOS;
using Net.payOS.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using Transaction = FPTU_ELibrary.Domain.Entities.Transaction;

namespace FPTU_ELibrary.Application.Services;

public class InvoiceService : GenericService<Invoice, InvoiceDto, int>,
    IInvoiceService<InvoiceDto>
{
    private readonly IUserService<UserDto> _userSvc;
    private readonly ITransactionService<TransactionDto> _transactionService;
    private readonly IFineService<FineDto> _fineService;
    private readonly WebTokenSettings _webMonitor;
    private readonly IDigitalBorrowService<DigitalBorrowDto> _digitalBorrowService;
    private readonly ILibraryCardPackageService<LibraryCardPackageDto> _libraryCardPackageService;
    private readonly PayOSSettings _monitor;

    public InvoiceService(
        IUserService<UserDto> userSvc,
        ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        ITransactionService<TransactionDto> transactionService,
        IFineService<FineDto> fineService,
        IOptionsMonitor<WebTokenSettings> webMonitor,
        IDigitalBorrowService<DigitalBorrowDto> digitalBorrowService,
        ILibraryCardPackageService<LibraryCardPackageDto> libraryCardPackageService,
        IMapper mapper, IOptionsMonitor<PayOSSettings> monitor,
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _userSvc = userSvc;
        _transactionService = transactionService;
        _fineService = fineService;
        _webMonitor = webMonitor.CurrentValue;
        _digitalBorrowService = digitalBorrowService;
        _libraryCardPackageService = libraryCardPackageService;
        _monitor = monitor.CurrentValue;
    }

    public async Task<IServiceResult> GetAllCardHolderInvoiceByUserIdAsync(Guid userId, int pageIndex, int pageSize)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<Invoice>(br => br.UserId == userId);

            // Add default order by
            baseSpec.AddOrderByDescending(i => i.CreatedAt);

            // Count total borrow request
            var totalInvoiceWithSpec = await _unitOfWork.Repository<Invoice, int>().CountAsync(baseSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalInvoiceWithSpec / pageSize);

            // Set pagination to specification after count total invoice 
            if (pageIndex > totalPage
                || pageIndex < 1) // Exceed total page or page index smaller than 1
            {
                pageIndex = 1; // Set default to first page
            }

            // Apply pagination
            baseSpec.ApplyPaging(skip: pageSize * (pageIndex - 1), take: pageSize);

            // Retrieve data with spec
            var entities = await _unitOfWork.Repository<Invoice, int>()
                .GetAllWithSpecAsync(baseSpec);
            if (entities.Any())
            {
                // Convert to dto collection
                var invoiceDtos = _mapper.Map<List<InvoiceDto>>(entities);

                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<LibraryCardHolderInvoiceDto>(
                    invoiceDtos.Select(br => br.ToCardHolderInvoiceDto()),
                    pageIndex, pageSize, totalPage, totalInvoiceWithSpec);

                // Response with pagination 
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }

            // Not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                new List<LibraryCardHolderInvoiceDto>());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get all invoice by library card id");
        }
    }

    public async Task<IServiceResult> GetCardHolderInvoiceByIdAsync(Guid userId, int invoiceId)
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
            var userDto = (await _userSvc.GetWithSpecAsync(userBaseSpec)).Data as UserDto;
            if (userDto == null || userDto.LibraryCardId == null) // Not found user
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                // Data not found or empty
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    StringUtils.Format(errMsg, isEng ? "reader" : "bạn đọc"));
            }

            // Build spec
            var baseSpec = new BaseSpecification<Invoice>(br =>
                br.UserId == userDto.UserId && br.InvoiceId == invoiceId);
            // Apply include 
            baseSpec.ApplyInclude(q => q
                .Include(i => i.Transactions)
                // .ThenInclude(t => t.PaymentMethod)
            );
            // Retrieve data with spec
            var existingEntity = await _unitOfWork.Repository<Invoice, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity != null)
            {
                // Convert to dto
                var invoiceDto = _mapper.Map<InvoiceDto>(existingEntity);

                // Get data successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                    invoiceDto.ToCardHolderInvoiceDto());
            }

            // Data empty or not found 
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get card holder invoice by id");
        }
    }

    public async Task<IServiceResult> CreatePayment(List<int> transactionIds, string email)
    {
        try
        {
            var userBaseSpec = new BaseSpecification<User>(u => u.Email.Equals(email));
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            //get user
            var user = await _userSvc.GetWithSpecAsync(userBaseSpec);
            if (user.Data is null)
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                        StringUtils.Format(ResultCodeConst.SYS_Warning0002,
                            isEng ? "user" : "người dùng"))
                    ;
            var userValue = (UserDto)user.Data!;

            InvoiceDto response = new InvoiceDto();
            // Generate requestId and orderCode
            var orderCode = PaymentUtils.GenerateRandomOrderCodeDigits(8);
            var paymentExpireAt = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow.AddHours(_monitor.PaymentExpireDuration),
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            PayOSPaymentRequestDto payOsPaymentRequest = new()
            {
                OrderCode = orderCode,
                // Amount = (int) premiumPackageDto.Price,
                Description = "Elibrary Payment",
                BuyerName = $"{userValue.FirstName} {userValue.LastName}".ToUpper(),
                BuyerEmail = userValue.Email,
                // Items =
                // [
                //     new
                //     {
                //         Name = "Premium Package",
                //         Quantity = 1,
                //         Price = premiumPackageDto.Price
                //     }
                // ],
                CancelUrl = _monitor.CancelUrl,
                ReturnUrl = _monitor.ReturnUrl,
                ExpiredAt = (int)((DateTimeOffset)paymentExpireAt).ToUnixTimeSeconds()
            };
            // get transaction to get amount
            // type and number of type of the payment
            Dictionary<string, object> transactionType = new Dictionary<string, object>()
            {
                { TransactionType.Fine.ToString(), new List<FineDto>() },
                { TransactionType.DigitalBorrow.ToString(), new List<DigitalBorrowDto>() },
                { TransactionType.LibraryCardRegister.ToString(), new List<LibraryCardPackage>() },
            };
            // total amount
            List<TransactionDto> transactionDtos = new List<TransactionDto>();
            int finePrice = 0;
            int digitalBorrowPrice = 0;
            int cardPackagePrice = 0;
            foreach (var transactionId in transactionIds)
            {
                var transaction = await _transactionService.GetByIdAsync(transactionId);
                if (transaction.Data is null)
                {
                    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                        StringUtils.Format(ResultCodeConst.SYS_Warning0002,
                            isEng ? "transaction" : "lịch sử thanh toán"));
                }

                var transactionValue = (TransactionDto)transaction.Data!;
                // if(!transactionValue.TransactionCode.IsNullOrEmpty())
                // {
                //     return new ServiceResult(ResultCodeConst.Payment_Warning0001,
                //         await _msgService.GetMessageAsync(ResultCodeConst.Payment_Warning0001));
                // }
                transactionDtos.Add(transactionValue);
                payOsPaymentRequest.Amount += (int)Math.Ceiling(transactionValue.Amount);
                if (transactionValue.FineId is not null)
                {
                    var fineBaseSpec = new BaseSpecification<Fine>(f => f.FineId == transactionValue.FineId);
                    fineBaseSpec.EnableSplitQuery();
                    fineBaseSpec.ApplyInclude(q => q.Include(f => f.FinePolicy));
                    var fine = await _fineService.GetWithSpecAsync(fineBaseSpec);
                    if (fine.Data is null)
                        return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                            StringUtils.Format(ResultCodeConst.SYS_Warning0002, "transaction"));
                    ((List<FineDto>)transactionType[TransactionType.Fine.ToString()]).Add((FineDto)fine.Data!);
                    finePrice += (int)transactionValue.Amount;
                }
                else if (transactionValue.DigitalBorrowId is not null)
                {
                    var digitalBorrow = await _digitalBorrowService
                        .GetByIdAsync(transactionValue.DigitalBorrowId ?? 0);
                    if (digitalBorrow.Data is null)
                        return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                            StringUtils.Format(ResultCodeConst.SYS_Warning0002, "digital-borrow"));
                    ((List<DigitalBorrowDto>)transactionType[TransactionType.DigitalBorrow.ToString()]).Add(
                        (DigitalBorrowDto)digitalBorrow.Data!);
                    digitalBorrowPrice += (int)transactionValue.Amount;
                }
                else if (transactionValue.LibraryCardPackageId is not null)
                {
                    var libraryCardPackage = await _libraryCardPackageService
                        .GetByIdAsync(transactionValue.LibraryCardPackageId ?? 0);
                    if (libraryCardPackage.Data is null)
                        return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                            StringUtils.Format(ResultCodeConst.SYS_Warning0002, "library-card-package"));
                    ((List<LibraryCardPackageDto>)transactionType[TransactionType.LibraryCardRegister.ToString()])
                        .Add((LibraryCardPackageDto)libraryCardPackage.Data!);
                    cardPackagePrice += (int)transactionValue.Amount;
                }
            }

            foreach (var key in transactionType.Keys)
            {
                if (key.Equals(TransactionType.Fine.ToString()))
                {
                    int quantity = ((List<FineDto>)transactionType[key]).Count;
                    if (quantity == 0) break;
                    payOsPaymentRequest.Items.Add(new
                    {
                        Name = key,
                        Quantity = quantity,
                        Price = finePrice
                    });
                }
                else if (key.Equals(TransactionType.DigitalBorrow.ToString()))
                {
                    int quantity = ((List<DigitalBorrowDto>)transactionType[key]).Count;
                    if (quantity == 0) break;
                    payOsPaymentRequest.Items.Add(new
                    {
                        Name = key,
                        Quantity = quantity,
                        Price = digitalBorrowPrice
                    });
                }
                else if (key.Equals(TransactionType.LibraryCardRegister.ToString()))
                {
                    int quantity = ((List<DigitalBorrowDto>)transactionType[key]).Count;
                    if (quantity == 0) break;
                    payOsPaymentRequest.Items.Add(new
                    {
                        Name = key,
                        Quantity = ((List<LibraryCardPackageDto>)transactionType[key]).Count,
                        Price = cardPackagePrice
                    });
                }
            }

            // Generate signature
            await payOsPaymentRequest.GenerateSignatureAsync(orderCode, _monitor);
            var payOsPaymentResp = await payOsPaymentRequest.GetUrlAsync(_monitor);

            // Create Payment status
            bool isCreatePaymentSuccess = payOsPaymentResp.Item1;
            if (isCreatePaymentSuccess && payOsPaymentResp.Item3 != null)
            {
                // response.UserId = userValue.UserId;
                // response.InvoiceCode = "OD" + orderCode;
                // response.TotalAmount = payOsPaymentRequest.Amount;
                // response.CreatedAt = DateTime.Now;
                //
                // var invoiceEntity = _mapper.Map<Invoice>(response);
                // await _unitOfWork.Repository<Invoice, int>().AddAsync(invoiceEntity);
                //
                // if (await _unitOfWork.SaveChangesAsync() <= 0)
                // {
                //     return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                //         await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
                // }

                foreach (var transactionDto in transactionDtos)
                {
                    transactionDto.TransactionCode = orderCode.ToString();
                    var updateTransaction =
                        await _transactionService.UpdateAsync(transactionDto.TransactionId, transactionDto);
                    if (updateTransaction.ResultCode != ResultCodeConst.SYS_Success0003) return updateTransaction;
                }
            }

            return new ServiceResult(ResultCodeConst.Payment_Success0001,
                await _msgService.GetMessageAsync(ResultCodeConst.Payment_Success0001), payOsPaymentResp.Item3);
        }

        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process create invoice");
        }
    }

    public async Task<IServiceResult> GetLinkInformationAsync(string paymentLinkId)
    {
        var response = await PayOsPaymentRequestExtensions.GetLinkInformationAsync(paymentLinkId, _monitor);
        if (response.Item1)
        {
            return new ServiceResult(ResultCodeConst.SYS_Success0002
                , await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), response.Item3);
        }

        return new ServiceResult(ResultCodeConst.Payment_Warning0002,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), response.Item2);
    }

    public async Task<IServiceResult> CancelPayOsPaymentAsync(string paymentLinkId, string cancellationReason)
    {
        // Initiate HttpClient
        using HttpClient httpClient = new();

        // Add header parameters
        httpClient.DefaultRequestHeaders.Add("x-client-id", _monitor.ClientId);
        httpClient.DefaultRequestHeaders.Add("x-api-key", _monitor.ApiKey);

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
        var cancelPaymentUrl = string.Format(_monitor.CancelPaymentUrl, paymentLinkId);
        // Execute POST request with uri and request content
        var cancelPaymentUrlRes = await httpClient.PostAsync(
            requestUri: cancelPaymentUrl,
            content: requestContent);

        // Response content
        var content = cancelPaymentUrlRes.Content.ReadAsStringAsync().Result;
        var responseData = JsonConvert.DeserializeObject<PayOSPaymentLinkInformationResponseDto>(content);
        // Check for response content not found 
        if (responseData == null)
            return new ServiceResult(ResultCodeConst.Payment_Warning0003,
                await _msgService.GetMessageAsync(ResultCodeConst.Payment_Warning0003));

        if (cancelPaymentUrlRes.IsSuccessStatusCode)
        {
            return new ServiceResult(ResultCodeConst.Payment_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.Payment_Success0002), responseData);
        }

        // 400: Webhook Url invalid
        // 401: Missing API Key & Client Key
        // 5xx: Sever error
        return new ServiceResult(ResultCodeConst.Payment_Fail0001,
            await _msgService.GetMessageAsync(ResultCodeConst.Payment_Warning0003));
    }

    public async Task<IServiceResult> VerifyPaymentWebhookDataAsync(PayOSPaymentLinkInformationResponse req)
    {
        var paymentResponse = _mapper.Map<PayOSPaymentLinkInformationResponseDto>(req);
        //System lang
        // Determine current system language
        var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
            LanguageContext.CurrentLanguage);
        var isEng = lang == SystemLanguage.English;

        //response 
        string paymentToken = string.Empty;

        PayOS payOs = new PayOS(_monitor.ClientId, _monitor.ApiKey, _monitor.ChecksumKey);

        // Initiate transaction detail
        string transactionCode = req.Data.OrderCode;
        DateTime? transactionDate = null;
        decimal? paymentAmount = null;
        string? cancellationReason = null;
        DateTime? cancelledAt = null;
        TransactionStatus status = TransactionStatus.Pending;
        bool isPremiumPackageActivated = false;

        // Get all transaction that have transaction code same as req ordercode
        var transactionBaseSpec = new BaseSpecification<Transaction>(t =>
            t.TransactionCode!.Equals(req.Data.OrderCode));
        transactionBaseSpec.EnableSplitQuery();
        transactionBaseSpec.ApplyInclude(q => q
            .Include(t => t.User)
            .Include(t => t.Fine)
            .Include(t => t.DigitalBorrow)
            .Include(t => t.LibraryCardPackage)
            .Include(t => t.Invoice)!
        );
        var transactions = await _transactionService.GetAllWithSpecAsync(transactionBaseSpec);
        if (transactions.Data is null)
        {
            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
            return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                StringUtils.Format(errMsg,
                    isEng
                        ? "There is no transaction match with given data"
                        : "Không có đơn thanh toán nào trùng hợp với dữ liệu đã đưa"
                ));
        }

        var transactionsValue = (List<TransactionDto>)transactions.Data!;

        switch (req.Data.Status.ToLower())
        {
            case "pending":
                status = TransactionStatus.Pending;
                break;
            case "cancelled":
                status = TransactionStatus.Cancelled;
                cancellationReason = req.Data.CancellationReason;
                cancelledAt = !string.IsNullOrEmpty(req.Data.CanceledAt) ? DateTime.Parse(req.Data.CanceledAt) : null;
                break;
            case "paid":
                try
                {
                    // Get signature
                    string signature =
                        await GenerateWebhookSignatureAsync(paymentResponse, req.Data.Id, _monitor.ChecksumKey);

                    // Initiate Webhook type
                    WebhookType webhookType = new(
                        req.Code,
                        req.Desc,
                        req.Success,
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
                        ), signature
                    );
                    // Verify payment webhook data
                    WebhookData webhookData = payOs.verifyPaymentWebhookData(webhookType);

                    // Update transaction status to DB
                    if (webhookData != null!) // Success status
                    {
                        DateTimeOffset parsedDateTimeOffset = DateTimeOffset.ParseExact(
                            webhookData.transactionDateTime, "yyyy-MM-ddTHH:mm:ssK", null);
                        // Transaction datetime
                        transactionDate = parsedDateTimeOffset.DateTime;
                        // Payment amount
                        paymentAmount = webhookData.amount;
                        // Transaction status
                        status = TransactionStatus.Paid;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message);
                    throw new Exception("Error invoke when process verify payment");
                }

                break;
        }

        foreach (var dto in transactionsValue)
        {
            if (!status.Equals(TransactionStatus.Pending))
            {
                dto.TransactionDate = transactionDate;
                dto.CancellationReason = cancellationReason;
                dto.CancelledAt = cancelledAt;
                dto.TransactionStatus = status;
            }

            var updatedTransaction = await _transactionService.UpdateAsync(dto.TransactionId, dto);
            if (updatedTransaction.ResultCode.Equals(ResultCodeConst.SYS_Fail0003))
            {
                return updatedTransaction;
            }
        }

        var userSpec =
            new BaseSpecification<User>(u => u.UserId.ToString().Equals(transactionsValue[0].UserId.ToString()));
        var user = await _userSvc.GetWithSpecAsync(userSpec);
        if (user.Data is null)
        {
            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
            return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                StringUtils.Format(errMsg,
                    isEng
                        ? "There is no user match with given data"
                        : "Không có người dùng nào trùng hợp với dữ liệu đã đưa"
                ));
        }

        var userValue = (UserDto)user.Data!;

        paymentToken = await new PaymentUtils(_logger)
            .GenerateTransactionTokenAsync(email: userValue.Email,
                transactionCode: transactionsValue[0].TransactionCode!,
                transactionDate: transactionsValue[0].TransactionDate!.Value,
                webTokenSettings: _webMonitor);

        var invoice = new InvoiceDto()
        {
            UserId = userValue.UserId,
            InvoiceCode = "OD" + req.Data.OrderCode,
            TotalAmount = transactionsValue.Sum(t => t.Amount),
            CreatedAt = DateTime.Now,
            PaidAt = DateTime.Now,
        };
        var invoiceEntity = _mapper.Map<Invoice>(invoice);
        await _unitOfWork.Repository<Invoice, int>().AddAsync(invoiceEntity);
        if (await _unitOfWork.SaveChangesAsync() <= 0)
        {
            return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
        }
        foreach (var transactionDto in transactionsValue)
        {
            transactionDto.InvoiceId = invoiceEntity.InvoiceId;
            var updateTransaction =
                await _transactionService.UpdateAsync(transactionDto.TransactionId, transactionDto);
            if (updateTransaction.ResultCode != ResultCodeConst.SYS_Success0003) return updateTransaction;
        }
        return new ServiceResult(ResultCodeConst.Payment_Success0003,
            await _msgService.GetMessageAsync(ResultCodeConst.Payment_Success0003), paymentToken);
    }

    private async Task<string> GenerateWebhookSignatureAsync(PayOSPaymentLinkInformationResponseDto resp,
        string paymentLinkId, string checksumKey)
    {
        var rawSignature =
            $"orderCode={resp.Data.OrderCode}&amount={resp.Data.Amount}&description={resp.Data.Transactions[0].Description}" +
            $"&accountNumber={resp.Data.Transactions[0].AccountNumber}&reference={resp.Data.Transactions[0].Reference}&transactionDateTime={resp.Data.Transactions[0].TransactionDateTime}" +
            $"&currency=VND&paymentLinkId={paymentLinkId}&code={resp.Code}&desc={resp.Desc}" +
            $"&counterAccountBankId={resp.Data.Transactions[0].CounterAccountBankId}&counterAccountBankName={resp.Data.Transactions[0].CounterAccountBankName}" +
            $"&counterAccountName={resp.Data.Transactions[0].CounterAccountName}&counterAccountNumber={resp.Data.Transactions[0].CounterAccountNumber}" +
            $"&virtualAccountName={resp.Data.Transactions[0].VirtualAccountName}&virtualAccountNumber={resp.Data.Transactions[0].VirtualAccountNumber}";
        // Split the raw signature string into key-value pairs
        List<string> keyValuePairs = rawSignature.Split('&').ToList();

        // Sort the key-value pairs based on the key
        keyValuePairs.Sort((pair1, pair2) =>
        {
            var key1 = pair1.Split('=')[0];
            var key2 = pair2.Split('=')[0];
            return string.Compare(key1, key2, StringComparison.Ordinal);
        });

        // Join the sorted key-value pairs back into a single string
        string sortedRawSignature = string.Join("&", keyValuePairs);

        // Generate the HMAC hash using the sorted string
        return await Task.FromResult(HashUtils.HmacSha256(sortedRawSignature, checksumKey));
    }
}