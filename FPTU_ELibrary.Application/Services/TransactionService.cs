using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Dtos.Payments.PayOS;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class TransactionService : GenericService<Transaction, TransactionDto, int>,
    ITransactionService<TransactionDto>
{
    // Lazy services
    private readonly Lazy<IUserService<UserDto>> _userSvc;
    private readonly Lazy<ILibraryCardService<LibraryCardDto>> _libCardService;
    private readonly Lazy<IBorrowRecordService<BorrowRecordDto>> _borrowRecService;
    private readonly Lazy<IBorrowRequestService<BorrowRequestDto>> _borrowReqService;
    private readonly Lazy<IBorrowRequestResourceService<BorrowRequestResourceDto>> _borrowReqResourceService;
    private readonly Lazy<IDigitalBorrowService<DigitalBorrowDto>> _digitalBorrowService;
    private readonly Lazy<ILibraryResourceService<LibraryResourceDto>> _resourceService;
    private readonly Lazy<IPaymentMethodService<PaymentMethodDto>> _paymentMethodService;
    private readonly Lazy<ILibraryCardPackageService<LibraryCardPackageDto>> _cardPackageService;

    private readonly IEmployeeService<EmployeeDto> _employeeService;

    private readonly PaymentSettings _paymentSettings;
    private readonly PayOSSettings _payOsSettings;
    private readonly BorrowSettings _borrowSettings;

    public TransactionService(
        // Lazy services
        Lazy<IUserService<UserDto>> userSvc,
        Lazy<ILibraryCardService<LibraryCardDto>> libCardService,
        Lazy<IBorrowRecordService<BorrowRecordDto>> borrowRecService,
        Lazy<IBorrowRequestService<BorrowRequestDto>> borrowReqService,
        Lazy<IBorrowRequestResourceService<BorrowRequestResourceDto>> borrowReqResourceService,
        Lazy<ILibraryResourceService<LibraryResourceDto>> resourceService,
        Lazy<ILibraryCardPackageService<LibraryCardPackageDto>> cardPackageService,
        Lazy<IPaymentMethodService<PaymentMethodDto>> paymentMethodService,
        Lazy<IDigitalBorrowService<DigitalBorrowDto>> digitalBorrowService,
        
        IEmployeeService<EmployeeDto> employeeService,
        IOptionsMonitor<PaymentSettings> monitor,
        IOptionsMonitor<BorrowSettings> monitor1,
        IOptionsMonitor<PayOSSettings> payOsMonitor,
        ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _userSvc = userSvc;
        _borrowRecService = borrowRecService;
        _borrowReqService = borrowReqService;
        _borrowReqResourceService = borrowReqResourceService;
        _libCardService = libCardService;
        _resourceService = resourceService;
        _cardPackageService = cardPackageService;
        _paymentMethodService = paymentMethodService;
        _digitalBorrowService = digitalBorrowService;
        _employeeService = employeeService;
        _borrowSettings = monitor1.CurrentValue;
        _paymentSettings = monitor.CurrentValue;
        _payOsSettings = payOsMonitor.CurrentValue;
    }

    public async Task<IServiceResult> GetAllWithSpecFromDashboardAsync(ISpecification<Transaction> spec)
    {
        return await base.GetAllWithSpecAsync(spec);
    }
    
    public async Task<IServiceResult> GetAllCardHolderTransactionAsync(ISpecification<Transaction> specification, bool tracked = true)
    {
        try
        {
            // Try to parse specification to TransactionSpecification
            var transSpecification = specification as TransactionSpecification;
            // Check if specification is null
            if (transSpecification == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }

            // Count total transactions
            var totalTransWithSpec = await _unitOfWork.Repository<Transaction, int>().CountAsync(transSpecification);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalTransWithSpec / transSpecification.PageSize);

            // Set pagination to specification after count total library item
            if (transSpecification.PageIndex > totalPage
                || transSpecification.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
                transSpecification.PageIndex = 1; // Set default to first page
            }

            // Apply pagination
            transSpecification.ApplyPaging(
                skip: transSpecification.PageSize * (transSpecification.PageIndex - 1),
                take: transSpecification.PageSize);

            var entities = await _unitOfWork.Repository<Transaction, int>()
                .GetAllWithSpecAsync(transSpecification, tracked);
            if (entities.Any())
            {
                // Map to dto
                var dtoList = _mapper.Map<List<TransactionDto>>(entities);
                
                // Convert to get transaction dto
                var getDigitalBorrowList = dtoList.Select(d => d.ToGetTransactionDto());
                
                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<GetTransactionDto>(getDigitalBorrowList,
                    transSpecification.PageIndex, transSpecification.PageSize, totalPage, totalTransWithSpec);
                
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
            throw new Exception("Erorr invoke when process get all transactions");
        }
    }

    public async Task<IServiceResult> GetByIdAsync(int id, string? email = null, Guid? userId = null)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Build spec
            var baseSpec = new BaseSpecification<Transaction>(br => br.TransactionId == id);
            // Add filter (if any)
            if (!string.IsNullOrEmpty(email))
            {
                baseSpec.AddFilter(db => db.User.Email == email);
            }
            if (userId.HasValue && userId != Guid.Empty)
            {
                baseSpec.AddFilter(db => db.UserId == userId);
            }

            // Apply include 
            baseSpec.ApplyInclude(q => q
                .Include(t => t.User)
                .Include(t => t.Fine)
                    .ThenInclude(f => f.FinePolicy)
                .Include(t => t.LibraryResource)
                .Include(t => t.LibraryCardPackage)
                .Include(t => t.PaymentMethod)
                .Include(t => t.BorrowRequestResources)
                    .ThenInclude(brr => brr.LibraryResource)
            );
            // Retrieve with spec
            var existingEntity = await _unitOfWork.Repository<Transaction, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity != null)
            {
                // Map to dto
                var transactionDto = _mapper.Map<TransactionDto>(existingEntity);
                
                // Covert to get transaction dto
                var getTransactionDto = transactionDto.ToGetTransactionDto();
                
                // Get data successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), getTransactionDto);
            }
            
            // Not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get card holder transaction by transaction id");
        }
    }
    
    public async Task<IServiceResult> CreateAsync(TransactionDto dto, string createdByEmail)
    {
        try
        {
            // Determine current system language
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Check exist user
            var userSpec = new BaseSpecification<User>(u => Equals(u.Email, createdByEmail));
            // Apply including library card
            userSpec.ApplyInclude(q => q.Include(u => u.LibraryCard!));
            var userDto = (await _userSvc.Value.GetWithSpecAsync(userSpec)).Data as UserDto;
            if (userDto == null)
            {
                // Forbid to access
                throw new ForbiddenException("Not allow to access");
            }
            
            // Check whether existing any transaction has pending status
            var pendingSpec = new BaseSpecification<Transaction>(t => 
                t.UserId == userDto.UserId &&
                t.TransactionType == dto.TransactionType &&
                t.TransactionStatus == TransactionStatus.Pending);
            var pendingTransaction = await _unitOfWork.Repository<Transaction, int>()
                .GetWithSpecAsync(pendingSpec);
            if (pendingTransaction != null)
            {
                // Initialize payOS payment response
                var payOsPaymentResponse = new PayOSPaymentResponseDto()
                {
                    Code = pendingTransaction.TransactionCode ?? string.Empty,
                    Desc = pendingTransaction.Description ?? string.Empty,
                    Data = new()
                    {
                        OrderCode = pendingTransaction.TransactionCode ?? string.Empty,
                        Amount = pendingTransaction.Amount,
                        PaymentLinkId = pendingTransaction.PaymentLinkId ?? string.Empty,
                        QrCode = pendingTransaction.QrCode ?? string.Empty,
                        Status = pendingTransaction.TransactionStatus.ToString().ToUpper(),
                    }
                };

                int expiredAtInSeconds = 0;
                if (pendingTransaction.ExpiredAt != null)
                {
                    expiredAtInSeconds = (int)((DateTimeOffset)pendingTransaction.ExpiredAt).ToUnixTimeSeconds();
                }
                else
                {
                    expiredAtInSeconds = (int)((DateTimeOffset)currentLocalDateTime.AddMinutes(_paymentSettings.TransactionExpiredInMinutes)).ToUnixTimeSeconds();
                }
                
                // Msg: Failed to create payment transaction as existing transaction with pending status
                return new ServiceResult(
                    resultCode: ResultCodeConst.Transaction_Warning0003,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Warning0003),
                    data: new PayOSPaymentLinkResponseDto()
                    {
                        PayOsResponse  = payOsPaymentResponse,
                        ExpiredAtOffsetUnixSeconds = expiredAtInSeconds
                    });
            }
            
            // Initialize payment description
            var viePaymentDesc = string.Empty;
            var engPaymentDesc = string.Empty;
            // Determine transaction type (process only for library card register/extension and digital borrow in this procedure)
            switch (dto.TransactionType)
            {
                case TransactionType.LibraryCardRegister or TransactionType.LibraryCardExtension:
                    // Check whether request along with card package or not
                    if (dto.LibraryCardPackageId == null || dto.LibraryCardPackageId == 0)
                    {
                        // Msg: Failed to create payment transaction. Please try again
                        return new ServiceResult(ResultCodeConst.Transaction_Fail0001,
                            await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0001));
                    }
                    else if(int.TryParse(dto.LibraryCardPackageId.ToString(), out var validPackageId))
                    {
                        // Default is payment for card register 
                        if (isEng) engPaymentDesc = "Library card register";
                        else viePaymentDesc = "Dang ky the thu vien";
                        
                        // Check if transaction type is card extension
                        if (dto.TransactionType == TransactionType.LibraryCardExtension)
                        {
                            // Require user has library card and has not extended yet 
                            if (userDto.LibraryCardId == null || userDto.LibraryCardId == Guid.Empty)
                            {
                                // Msg: Not found library card to process extend expiration date
                                return new ServiceResult(ResultCodeConst.Transaction_Warning0001,
                                    await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Warning0001));
                            }
                            
                            // Check card has expired yet 
                            var checkCardExtensionRes = await _libCardService.Value.CheckCardExtensionAsync(
                                Guid.Parse(userDto.LibraryCardId.ToString() ?? string.Empty));
                            if (checkCardExtensionRes.Data is false) return checkCardExtensionRes;
                            
                            // Assign card extension desc
                            if (isEng) engPaymentDesc = "Library card extension";
                            else viePaymentDesc = "Gia han the thu vien";
                        }
                        
                        // Try to retrieve library card
                        var cardPackageDto =
                            (await _cardPackageService.Value.GetByIdAsync(validPackageId)).Data as LibraryCardPackageDto;
                        if (cardPackageDto == null || !cardPackageDto.IsActive)
                        {
                            // Msg: Payment object does not exist. Please try again
                            return new ServiceResult(ResultCodeConst.Transaction_Fail0002,
                                await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0002));
                        }
                        
                        // Check whether user has not registered card yet
                        if (userDto.LibraryCardId == null || userDto.LibraryCardId == Guid.Empty)
                        {
                            // Msg: Cannot process create payment for library card register as not found card information
                            return new ServiceResult(ResultCodeConst.Transaction_Warning0002,
                                await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Warning0002));
                        }
                            
                        // Assign amount need to pay for 
                        dto.Amount = cardPackageDto.Price;
                        // Assign id
                        dto.LibraryCardPackageId = validPackageId;
                    }
                    break;
                case TransactionType.DigitalBorrow:
                    // Check whether request along with digital borrow or not
                    if (dto.ResourceId == null || dto.ResourceId == 0)
                    {
                        // Msg: Failed to create payment transaction. Please try again
                        return new ServiceResult(ResultCodeConst.Transaction_Fail0001,
                            await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0001));
                    }
                    else if (int.TryParse(dto.ResourceId.ToString(), out var validResourceId))
                    {
                        // Try to retrieve resource id
                        var resourceSpec = new BaseSpecification<LibraryResource>(r => 
                            r.ResourceId == validResourceId);
                        var resourceDto = (await _resourceService.Value.GetWithSpecAsync(resourceSpec)).Data as LibraryResourceDto;
                        if (resourceDto == null || resourceDto.IsDeleted)
                        {
                            // Msg: Payment object does not exist. Please try again
                            return new ServiceResult(ResultCodeConst.Transaction_Fail0002,
                                await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0002));
                        }
                        
                        // Check whether user has already borrowed the resource
                        var digitalSpec = new BaseSpecification<DigitalBorrow>(
                            db => db.ResourceId == resourceDto.ResourceId && db.UserId == userDto.UserId);
                        var digitalBorrowDto = (await _digitalBorrowService.Value.GetWithSpecAsync(digitalSpec)).Data as DigitalBorrowDto;
                        if (digitalBorrowDto != null)
                        {
                            // Declare err message string
                            string errMsg;
                            
                            // Check whether digital borrow has expired
                            if (digitalBorrowDto.Status == BorrowDigitalStatus.Expired)
                            {
                                // Msg: Digital resource {0} is borrowed but now is in expired status. You can extend expiration date in your borrowed history
                                errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0016);
                                return new ServiceResult(ResultCodeConst.Borrow_Warning0016,
                                    StringUtils.Format(errMsg, $"'{resourceDto.ResourceTitle}'"));
                            }
                            
                            // Msg: Digital resource {0} is borrowing. Cannot not create register
                            errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0015);
                            return new ServiceResult(ResultCodeConst.Borrow_Warning0015,
                                StringUtils.Format(errMsg, $"'{resourceDto.ResourceTitle}'"));
                        }
                        
                        // Assign amount need to pay for 
                        dto.Amount = resourceDto.BorrowPrice;
                        // Assign id 
                        dto.ResourceId = validResourceId;
                        
                        // Assign digital borrow desc
                        if (isEng) engPaymentDesc = "Digital resource borrow";
                        else viePaymentDesc = "Muon tai lieu dien tu";
                    }
                    break;
                case TransactionType.DigitalExtension:
                    // Check whether request along with digital borrow or not
                    if (dto.ResourceId == null || dto.ResourceId == 0)
                    {
                        // Msg: Failed to create payment transaction. Please try again
                        return new ServiceResult(ResultCodeConst.Transaction_Fail0001,
                            await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0001));
                    }else if (int.TryParse(dto.ResourceId.ToString(), out var validResourceId))
                    {
                        // Try to retrieve resource id
                        var resourceSpec = new BaseSpecification<LibraryResource>(r => 
                            r.ResourceId == validResourceId);
                        var resourceDto = (await _resourceService.Value.GetWithSpecAsync(resourceSpec)).Data as LibraryResourceDto;
                        if (resourceDto == null || resourceDto.IsDeleted)
                        {
                            // Msg: Payment object does not exist. Please try again
                            return new ServiceResult(ResultCodeConst.Transaction_Fail0002,
                                await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0002));
                        }
                        
                        // Check whether user has borrowed the resource or not 
                        var isDigitalBorrowing = (await _digitalBorrowService.Value.AnyAsync(
                            db => db.ResourceId == resourceDto.ResourceId && db.UserId == userDto.UserId)).Data is true;
                        if (!isDigitalBorrowing) // Not found any digital borrow
                        {
                            // Msg: Digital resource {0} not found in borrow history to process extend expiration date
                            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0017);
                            return new ServiceResult(ResultCodeConst.Borrow_Warning0017,
                                StringUtils.Format(errMsg, $"'{resourceDto.ResourceTitle}'"));
                        }
                        
                        // Assign amount need to pay for 
                        dto.Amount = resourceDto.BorrowPrice;
                        // Assign id 
                        dto.ResourceId = validResourceId;
                        
                        // Assign digital borrow desc
                        if (isEng) engPaymentDesc = "Digital resource extend";
                        else viePaymentDesc = "Gia han tai lieu dien tu";
                    }
                    break;
                default:
                    // Msg: You are not allowed to create this type of transaction
                    return new ServiceResult(ResultCodeConst.Transaction_Fail0003,
                        await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0003));
            }
            
            // Generate transaction code
            var transactionCodeDigits = PaymentUtils.GenerateRandomOrderCodeDigits(_paymentSettings.TransactionCodeLength);
            // Add necessary properties
            dto.TransactionCode = transactionCodeDigits.ToString();
            dto.CreatedAt = currentLocalDateTime;
            dto.ExpiredAt = currentLocalDateTime.AddMinutes(_paymentSettings.TransactionExpiredInMinutes);
            dto.CreatedBy = createdByEmail;
            dto.UserId = userDto.UserId;
            dto.TransactionStatus = TransactionStatus.Pending;
            dto.Description = viePaymentDesc;
            
            // Add default transaction method is digital payment as this function only use by user
            dto.TransactionMethod = TransactionMethod.DigitalPayment;
                
            // Initialize expired at offset unix seconds
            var expiredAtOffsetUnixSeconds = (int)((DateTimeOffset)dto.ExpiredAt).ToUnixTimeSeconds();
            // Generate payment link
            var payOsPaymentRequest = new PayOSPaymentRequestDto()
            {
                OrderCode = transactionCodeDigits,
                Amount = (int) Math.Round(dto.Amount),
                Description = isEng ? engPaymentDesc  : viePaymentDesc,
                BuyerName = $"{userDto.FirstName} {userDto.LastName}".ToUpper(),
                BuyerEmail = userDto.Email,
                BuyerPhone = userDto.Phone ?? string.Empty,
                BuyerAddress = userDto.Address ?? string.Empty,
                Items = [
                    new
                    {
                        Name = isEng ? dto.TransactionType.ToString() : dto.TransactionType.GetDescription(),
                        Quantity = 1,
                        Price = dto.Amount
                    }
                ],
                CancelUrl = _payOsSettings.CancelUrl,
                ReturnUrl = _payOsSettings.ReturnUrl,
                ExpiredAt = expiredAtOffsetUnixSeconds
            };
            
            // Generate signature
            await payOsPaymentRequest.GenerateSignatureAsync(transactionCodeDigits, _payOsSettings);
            var payOsPaymentResp = await payOsPaymentRequest.GetUrlAsync(_payOsSettings);
            
            // Create Payment status
            bool isCreatePaymentSuccess = payOsPaymentResp.Item1;
            
            // Check if create payment success with resp data
            if (isCreatePaymentSuccess && payOsPaymentResp.Item3 != null)
            {
                // Update payment information (if any)
                dto.QrCode = payOsPaymentResp.Item3.Data.QrCode;
                dto.PaymentLinkId = payOsPaymentResp.Item3.Data.PaymentLinkId;
                dto.Description = payOsPaymentResp.Item3.Data.Description;
                
                // Process create transaction
                await _unitOfWork.Repository<Transaction, int>().AddAsync(_mapper.Map<Transaction>(dto));

                if (await _unitOfWork.SaveChangesAsync() > 0)
                {
                    // Create payment link successfully
                    return new ServiceResult(ResultCodeConst.Transaction_Success0001,
                        await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Success0001), 
                        new PayOSPaymentLinkResponseDto()
                        {
                            PayOsResponse = payOsPaymentResp.Item3,
                            ExpiredAtOffsetUnixSeconds = expiredAtOffsetUnixSeconds
                        });
                }
            }
            
            // Mark as failed to create payment link
            return new ServiceResult(ResultCodeConst.Transaction_Fail0004,
                await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0004));
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process create transaction");
        }
    }

    public async Task<IServiceResult> CreateTransactionForBorrowRequestAsync(string createdByEmail, int borrowRequestId)
    {
        try
        {
            // Determine current system language
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Check exist user
            var userSpec = new BaseSpecification<User>(u => Equals(u.Email, createdByEmail));
            // Apply including library card
            userSpec.ApplyInclude(q => q.Include(u => u.LibraryCard!));
            var userDto = (await _userSvc.Value.GetWithSpecAsync(userSpec)).Data as UserDto;
            if (userDto == null)
            {
                // Forbid to access
                throw new ForbiddenException("Not allow to access");
            }
            
            // Check whether existing any transaction has pending status
            var pendingSpec = new BaseSpecification<Transaction>(t =>
                t.UserId == userDto.UserId &&
                t.TransactionType == TransactionType.DigitalBorrow &&
                t.TransactionStatus == TransactionStatus.Pending);
            var pendingTransaction = await _unitOfWork.Repository<Transaction, int>()
                .GetWithSpecAsync(pendingSpec);
            if (pendingTransaction != null)
            {
                // Initialize payOS payment response
                var payOsPaymentResponse = new PayOSPaymentResponseDto()
                {
                    Code = pendingTransaction.TransactionCode ?? string.Empty,
                    Desc = pendingTransaction.Description ?? string.Empty,
                    Data = new()
                    {
                        OrderCode = pendingTransaction.TransactionCode ?? string.Empty,
                        Amount = pendingTransaction.Amount,
                        PaymentLinkId = pendingTransaction.PaymentLinkId ?? string.Empty,
                        QrCode = pendingTransaction.QrCode ?? string.Empty,
                        Status = pendingTransaction.TransactionStatus.ToString().ToUpper(),
                    }
                };

                int expiredAtInSeconds = 0;
                if (pendingTransaction.ExpiredAt != null)
                {
                    expiredAtInSeconds = (int)((DateTimeOffset)pendingTransaction.ExpiredAt).ToUnixTimeSeconds();
                }
                else
                {
                    expiredAtInSeconds = (int)((DateTimeOffset)currentLocalDateTime.AddMinutes(_paymentSettings.TransactionExpiredInMinutes)).ToUnixTimeSeconds();
                }
                
                // Msg: Failed to create payment transaction as existing transaction with pending status
                return new ServiceResult(
                    resultCode: ResultCodeConst.Transaction_Warning0003,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Warning0003),
                    data: new PayOSPaymentLinkResponseDto()
                    {
                        PayOsResponse  = payOsPaymentResponse,
                        ExpiredAtOffsetUnixSeconds = expiredAtInSeconds
                    });
            }
            
            // Check exist borrow request
            var recordSpec = new BaseSpecification<BorrowRequest>(br => br.BorrowRequestId == borrowRequestId);
            // Apply include
            recordSpec.ApplyInclude(q => q
                    .Include(b => b.BorrowRequestResources) // Include all borrow request resources
                        .ThenInclude(brr => brr.Transaction!) // Include transaction reference
            );
            // Retrieve with spec
            var borrowReqDto = (await _borrowReqService.Value.GetWithSpecAsync(recordSpec)).Data as BorrowRequestDto;
            if (borrowReqDto == null)
            {
                // Msg: Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng
                        ? "borrow request to process create transaction for library resources borrowing payment"
                        : "lịch sử yêu cầu mượn để tạo thanh toán tài liệu điện tử"));
            }
            
            // Generate transaction code
            var transactionCodeDigits = PaymentUtils.GenerateRandomOrderCodeDigits(_paymentSettings.TransactionCodeLength);
            var expiredAt = currentLocalDateTime.AddMinutes(_paymentSettings.TransactionExpiredInMinutes);
            // Extract all pending resources payment
            var pendingReqResources = borrowReqDto.BorrowRequestResources
                .Where(brr => brr.TransactionId == null && brr.Transaction == null).ToList();
            if (!pendingReqResources.Any())
            {
                // Msg: Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng
                        ? "any pending resource payment to create transaction"
                        : "tài liệu điện tử cần thanh toán"));
            }
            
            // Retrieve payOS payment method
            var payOsPaymentMethod = (await _paymentMethodService.Value.GetWithSpecAsync(
                new BaseSpecification<PaymentMethod>(p => p.MethodName == PaymentType.PayOS.ToString()))).Data as PaymentMethodDto;
            if (payOsPaymentMethod == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng 
                        ? "payment method to process create borrow request payment" 
                        : "phương thức thanh toán để tạo phiên thanh toán cho yêu cầu mượn"));
            }
            
            // Initialize list borrow request resource dto to process add transaction
            var toAddTransactionList = new List<BorrowRequestResourceDto>();
            // Iterate each borrow resource to create transaction
            foreach (var requestResource in pendingReqResources)
            {
                toAddTransactionList.Add(new BorrowRequestResourceDto()
                {
                    BorrowRequestResourceId = requestResource.BorrowRequestResourceId,
                    Transaction = new ()
                    {
                        TransactionCode = transactionCodeDigits.ToString(),
                        Amount = requestResource.BorrowPrice,
                        CreatedAt = currentLocalDateTime,
                        ExpiredAt = expiredAt,
                        CreatedBy = createdByEmail,
                        TransactionStatus = TransactionStatus.Pending,
                        TransactionType = TransactionType.DigitalBorrow,
                        TransactionMethod = TransactionMethod.DigitalPayment,
                        UserId = userDto.UserId,
                        ResourceId = requestResource.ResourceId,
                        PaymentMethodId = payOsPaymentMethod.PaymentMethodId
                    }
                });
            }
            
            // Aggregate amount
            var aggregatedAmount = toAddTransactionList.Sum(t => t.Transaction?.Amount ?? 0);
            // Initialize expired at offset unix seconds
            var expiredAtOffsetUnixSeconds = (int)((DateTimeOffset) expiredAt).ToUnixTimeSeconds();
            // Generate payment link
            var payOsPaymentRequest = new PayOSPaymentRequestDto()
            {
                OrderCode = transactionCodeDigits,
                Amount = (int) aggregatedAmount,
                Description = isEng 
                    ? $"Pay for {toAddTransactionList.Count} digital borrow"  
                    : $"Thanh toan {toAddTransactionList.Count} tai lieu",
                BuyerName = $"{userDto.FirstName} {userDto.LastName}".ToUpper(),
                BuyerEmail = userDto.Email,
                BuyerPhone = userDto.Phone ?? string.Empty,
                BuyerAddress = userDto.Address ?? string.Empty,
                Items = toAddTransactionList
                    .Select(f => new
                    {
                        Name = isEng ? f.Transaction?.TransactionType.ToString() : f.Transaction?.TransactionType.GetDescription(),
                        Quantity = 1,
                        Price = f.Transaction?.Amount ?? 0
                    })
                    .Cast<object>()
                    .ToList(),
                CancelUrl = _payOsSettings.CancelUrl,
                ReturnUrl = _payOsSettings.ReturnUrl,
                ExpiredAt = expiredAtOffsetUnixSeconds
            };
            
            // Generate signature
            await payOsPaymentRequest.GenerateSignatureAsync(transactionCodeDigits, _payOsSettings);
            var payOsPaymentResp = await payOsPaymentRequest.GetUrlAsync(_payOsSettings);
            
            // Create Payment status
            bool isCreatePaymentSuccess = payOsPaymentResp.Item1;
            
            // Check if create payment success with resp data
            if (isCreatePaymentSuccess && payOsPaymentResp.Item3 != null)
            {
                // Iterate each transaction to update QRCode
                foreach (var dto in toAddTransactionList)
                {
                    // Update payment information (if any)
                    if (dto.Transaction != null)
                    {
                        dto.Transaction.QrCode = payOsPaymentResp.Item3.Data.QrCode;
                        dto.Transaction.PaymentLinkId = payOsPaymentResp.Item3.Data.PaymentLinkId;
                        dto.Transaction.Description = payOsPaymentResp.Item3.Data.Description;
                    }
                }
                
                // Process add range 
                await _borrowReqResourceService.Value.AddRangeResourceTransactionsWithoutSaveChangesAsync(
                    borrowResources: toAddTransactionList);
                
                // Save DB
                if (await _unitOfWork.SaveChangesWithTransactionAsync() > 0)
                {
                    // Create payment link successfully
                    return new ServiceResult(ResultCodeConst.Transaction_Success0001,
                        await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Success0001), 
                        new PayOSPaymentLinkResponseDto()
                        {
                            PayOsResponse = payOsPaymentResp.Item3,
                            ExpiredAtOffsetUnixSeconds = expiredAtOffsetUnixSeconds
                        });
                }
            }
            
            // Mark as failed to create payment link
            return new ServiceResult(ResultCodeConst.Transaction_Fail0004,
                await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0004));
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process create transaction for borrow request");
        }
    }
    
    public async Task<IServiceResult> CreateTransactionForBorrowRecordAsync(string createdByEmail, int borrowRecordId)
    {
        try
        {
            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Determine current system language
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Check exist user
            var userSpec = new BaseSpecification<User>(u => Equals(u.Email, createdByEmail));
            // Apply including library card
            userSpec.ApplyInclude(q => q.Include(u => u.LibraryCard!));
            var userDto = (await _userSvc.Value.GetWithSpecAsync(userSpec)).Data as UserDto;
            if (userDto == null)
            {
                // Forbid to access
                throw new ForbiddenException("Not allow to access");
            }
            
            // Check whether existing any transaction has pending status
            var pendingSpec = new BaseSpecification<Transaction>(t => 
                t.UserId == userDto.UserId &&
                t.TransactionType == TransactionType.Fine &&
                t.TransactionStatus == TransactionStatus.Pending);
            var pendingTransaction = await _unitOfWork.Repository<Transaction, int>()
                .GetWithSpecAsync(pendingSpec);
            if (pendingTransaction != null)
            {
                // Initialize payOS payment response
                var payOsPaymentResponse = new PayOSPaymentResponseDto()
                {
                    Code = pendingTransaction.TransactionCode ?? string.Empty,
                    Desc = pendingTransaction.Description ?? string.Empty,
                    Data = new()
                    {
                        OrderCode = pendingTransaction.TransactionCode ?? string.Empty,
                        Amount = pendingTransaction.Amount,
                        PaymentLinkId = pendingTransaction.PaymentLinkId ?? string.Empty,
                        QrCode = pendingTransaction.QrCode ?? string.Empty,
                        Status = pendingTransaction.TransactionStatus.ToString().ToUpper(),
                    }
                };

                int expiredAtInSeconds = 0;
                if (pendingTransaction.ExpiredAt != null)
                {
                    expiredAtInSeconds = (int)((DateTimeOffset)pendingTransaction.ExpiredAt).ToUnixTimeSeconds();
                }
                else
                {
                    expiredAtInSeconds = (int)((DateTimeOffset)currentLocalDateTime.AddMinutes(_paymentSettings.TransactionExpiredInMinutes)).ToUnixTimeSeconds();
                }
                
                // Msg: Failed to create payment transaction as existing transaction with pending status
                return new ServiceResult(
                    resultCode: ResultCodeConst.Transaction_Warning0003,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Warning0003),
                    data: new PayOSPaymentLinkResponseDto()
                    {
                        PayOsResponse  = payOsPaymentResponse,
                        ExpiredAtOffsetUnixSeconds = expiredAtInSeconds
                    });
            }
            
            // Check exist borrow record
            var recordSpec = new BaseSpecification<BorrowRecord>(br => br.BorrowRecordId == borrowRecordId);
            // Apply include
            recordSpec.ApplyInclude(q => q
                .Include(b => b.BorrowRecordDetails)
                    .ThenInclude(brd => brd.Fines) // Include all fines of each borrow record
                        .ThenInclude(f => f.FinePolicy) // Include all fine policies
            );
            // Retrieve with spec
            var borrowRecDto = (await _borrowRecService.Value.GetWithSpecAsync(recordSpec)).Data as BorrowRecordDto;
            if (borrowRecDto == null)
            {
                // Msg: Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng
                        ? "borrow record to process create transaction for fine payment"
                        : "lịch sử mượn để tạo phí thanh toán"));
            }
            
            // Generate transaction code
            var transactionCodeDigits = PaymentUtils.GenerateRandomOrderCodeDigits(_paymentSettings.TransactionCodeLength);
            var expiredAt = currentLocalDateTime.AddMinutes(_paymentSettings.TransactionExpiredInMinutes);
            // Initialize list of transaction
            var transactionDtos = new List<TransactionDto>();
            // Extract all pending fine
            var pendingFines = borrowRecDto.BorrowRecordDetails
                .SelectMany(brd => brd.Fines)
                .Where(f => f.Status == FineStatus.Pending || f.Status == FineStatus.Expired)
                .ToList();
            if (!pendingFines.Any())
            {
               // Msg: Not found {0}
               var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
               return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                   StringUtils.Format(errMsg, isEng
                       ? "any pending fines to create transaction"
                       : "phí phạt cần thanh toán"));
            }
            
            // Retrieve payOS payment method
            var payOsPaymentMethod = (await _paymentMethodService.Value.GetWithSpecAsync(
                new BaseSpecification<PaymentMethod>(p => p.MethodName == PaymentType.PayOS.ToString()))).Data as PaymentMethodDto;
            if (payOsPaymentMethod == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng 
                        ? "payment method to process create fines payment" 
                        : "phương thức thanh toán để tạo phiên thanh toán phí phạt"));
            }
            
            // Iterate each fine to create transaction
            foreach (var fine in pendingFines)
            {
                // Initialize fine amount
                // decimal? recalculateFineAmount = 0;

                // if (fine.ExpiryAt != null && fine.Status == FineStatus.Expired)
                // {
                //     // Count days of expired date compared to created date
                //     var overDueDays = currentLocalDateTime.Subtract(fine.ExpiryAt.Value).Days;
                //     
                //     // Determine policy condition type
                //     switch (fine.FinePolicy.ConditionType)
                //     {
                //         case FinePolicyConditionType.Damage or FinePolicyConditionType.OverDue:
                //             // Recalculate fine amount
                //             recalculateFineAmount = overDueDays > 0 && fine.FinePolicy.FineAmountPerDay != null 
                //                 ? fine.FineAmount + (overDueDays * fine.FinePolicy.FineAmountPerDay)
                //                 : 0;
                //             break;
                //         case FinePolicyConditionType.Lost:
                //             // Recalculate fine amount with percentage of total amount
                //             recalculateFineAmount = overDueDays > 0
                //                 ? fine.FineAmount * (1 + (decimal) _borrowSettings.LostAmountPercentagePerDay / 100)
                //                 : fine.FineAmount;
                //             break;
                //     }
                // }
                
                // Add transaction with default value
                
                transactionDtos.Add(new ()
                {
                    TransactionCode = transactionCodeDigits.ToString(),
                    Amount = (int)Math.Round(fine.FineAmount),
                    CreatedAt = currentLocalDateTime,
                    ExpiredAt = expiredAt,
                    CreatedBy = createdByEmail,
                    TransactionType = TransactionType.Fine,
                    TransactionStatus = TransactionStatus.Pending,
                    TransactionMethod = TransactionMethod.DigitalPayment,
                    PaymentMethodId = payOsPaymentMethod.PaymentMethodId,
                    Description = fine.FineNote,
                    UserId = userDto.UserId,
                    FineId = fine.FineId
                });
            }
            
            // Aggregate amount
            var aggregatedAmount = transactionDtos.Sum(t => t.Amount);
            // Initialize expired at offset unix seconds
            var expiredAtOffsetUnixSeconds = (int)((DateTimeOffset) expiredAt).ToUnixTimeSeconds();
            // Generate payment link
            var payOsPaymentRequest = new PayOSPaymentRequestDto()
            {
                OrderCode = transactionCodeDigits,
                Amount = (int) aggregatedAmount,
                Description = isEng 
                    ? $"Pay for {transactionDtos.Count} fines"  
                    : $"Thanh toan {transactionDtos.Count} phi phat",
                BuyerName = $"{userDto.FirstName} {userDto.LastName}".ToUpper(),
                BuyerEmail = userDto.Email,
                BuyerPhone = userDto.Phone ?? string.Empty,
                BuyerAddress = userDto.Address ?? string.Empty,
                Items = transactionDtos
                    .Select(f => new
                    {
                        Name = isEng ? f.TransactionType.ToString() : f.TransactionType.GetDescription(),
                        Quantity = 1,
                        Price = f.Amount
                    })
                    .Cast<object>()
                    .ToList(),
                CancelUrl = _payOsSettings.CancelUrl,
                ReturnUrl = _payOsSettings.ReturnUrl,
                ExpiredAt = expiredAtOffsetUnixSeconds
            };
            
            // Generate signature
            await payOsPaymentRequest.GenerateSignatureAsync(transactionCodeDigits, _payOsSettings);
            var payOsPaymentResp = await payOsPaymentRequest.GetUrlAsync(_payOsSettings);
            
            // Create Payment status
            bool isCreatePaymentSuccess = payOsPaymentResp.Item1;
            
            // Check if create payment success with resp data
            if (isCreatePaymentSuccess && payOsPaymentResp.Item3 != null)
            {
                // Iterate each transaction to update QRCode
                foreach (var dto in transactionDtos)
                {
                    // Update payment information (if any)
                    dto.QrCode = payOsPaymentResp.Item3.Data.QrCode;
                    dto.PaymentLinkId = payOsPaymentResp.Item3.Data.PaymentLinkId;
                    dto.Description = payOsPaymentResp.Item3.Data.Description;
                }
                
                // Process create transaction
                await _unitOfWork.Repository<Transaction, int>().AddRangeAsync(_mapper.Map<List<Transaction>>(transactionDtos));

                if (await _unitOfWork.SaveChangesAsync() > 0)
                {
                    // Create payment link successfully
                    return new ServiceResult(ResultCodeConst.Transaction_Success0001,
                        await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Success0001), 
                        new PayOSPaymentLinkResponseDto()
                        {
                            PayOsResponse = payOsPaymentResp.Item3,
                            ExpiredAtOffsetUnixSeconds = expiredAtOffsetUnixSeconds
                        });
                }
            }
            
            // Mark as failed to create payment link
            return new ServiceResult(ResultCodeConst.Transaction_Fail0004,
                await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0004));
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process create transaction for borrow record");
        }
    }
    
    public async Task<IServiceResult> CreateWithoutSaveChangesAsync(TransactionDto dto)
    {
        try
        {
            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(dto);
            // Check for valid validations
            if (validationResult != null && !validationResult.IsValid)
            {
                _logger.Error("Invalid validation invoke when process create transaction without save changes");
                // Mark as failed to create
                return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
            }
            
            // Process add new transaction
            await _unitOfWork.Repository<Transaction, int>().AddAsync(_mapper.Map<Transaction>(dto));
            
            // Mark as create success
            return new ServiceResult(ResultCodeConst.SYS_Success0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process create transaction without save changes");
        }
    }
    
    public async Task<IServiceResult> GetAllByTransactionCodeAsync(string transactionCode)
    {
        try
        {
            // Build spec 
            var baseSpec = new BaseSpecification<Transaction>(trans =>
                Equals(trans.TransactionCode, transactionCode));
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(t => t.Fine)
                .Include(t => t.LibraryResource)
                .Include(t => t.LibraryCardPackage)
                .Include(t => t.PaymentMethod)!
            );
            // Get all with spec
            var entities = await _unitOfWork.Repository<Transaction, int>().GetAllWithSpecAsync(baseSpec);
            if (entities.Any())
            {
                // Get data successfully 
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                    _mapper.Map<List<TransactionDto>>(entities));
            }
            
            // Data not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                _mapper.Map<List<TransactionDto>>(entities));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get all by transaction code");
        }
    }
    
    public async Task<IServiceResult> UpdateStatusByTransactionCodeAsync(
        string transactionCode, DateTime? transactionDate,
        string? cancellationReason, DateTime? cancelledAt, TransactionStatus status)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<Transaction>(t => Equals(t.TransactionCode, transactionCode));
            // Retrieve with spec
            var entities = await _unitOfWork.Repository<Transaction, int>().GetAllWithSpecAsync(baseSpec);
            // Convert to list 
            var transactionList = entities.ToList();
            if (transactionList.Any())
            {
                // Iterate each transaction to update status
                foreach (var transactionEntity in transactionList)
                {
                    // Determine transaction status
                    switch (status)
                    {
                        case TransactionStatus.Pending:
                            transactionEntity.TransactionStatus = TransactionStatus.Pending;
                            break;
                        case TransactionStatus.Expired:
                            transactionEntity.TransactionStatus = TransactionStatus.Expired;
                            break;
                        case TransactionStatus.Paid:
                            // Update transaction status
                            transactionEntity.TransactionStatus = TransactionStatus.Paid;
                            // Update transaction payment datetime
                            transactionEntity.TransactionDate = transactionDate;
                            break;
                        case TransactionStatus.Cancelled:
                            transactionEntity.TransactionStatus = TransactionStatus.Cancelled;
                            transactionEntity.CancellationReason = cancellationReason;
                            transactionEntity.CancelledAt = cancelledAt;
                            break;
                    }
                    
                    // Process update
                    await _unitOfWork.Repository<Transaction, int>().UpdateAsync(transactionEntity);
                }
                
                // Save DB
                var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
                if (isSaved)
                {
                    // Update successfully
                    return new ServiceResult(ResultCodeConst.SYS_Success0003,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);   
                }
            }
            // Fail to update
            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);   
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update transaction status");
        }
    }

    public async Task<IServiceResult> CancelTransactionsByCodeAsync(string transactionCode, string cancellationReason)
    {
        try
        {
            // Check exist transaction
            // Build spec
            var baseSpec = new BaseSpecification<Transaction>(t => Equals(t.TransactionCode, transactionCode));
            // Retrieve all transactions by code
            var entities = (await _unitOfWork.Repository<Transaction, int>().GetAllWithSpecAsync(baseSpec)).ToList();
            if (entities.Any())
            {
                // Current local datetime
                var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                	// Vietnam timezone
                	TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
                            
                // Iterate each transaction to update its status
                foreach (var transaction in entities)
                {
                    // Update transaction status
                    transaction.TransactionStatus = TransactionStatus.Cancelled;
                    // Cancelled at
                    transaction.CancelledAt = currentLocalDateTime;
                    // Cancellation reason
                    transaction.CancellationReason = cancellationReason;
                    
                    // Process update transaction
                    await _unitOfWork.Repository<Transaction, int>().UpdateAsync(transaction);
                }
                
                // Save DB
                var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
                if (isSaved)
                {
                    // Msg: Cancel payment transaction successfully
                    return new ServiceResult(ResultCodeConst.Transaction_Success0003,
                        await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Success0003));
                }
            }
            
            // Msg: Failed to cancel payment transaction
            return new ServiceResult(ResultCodeConst.Transaction_Fail0007,
                await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0007));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process cancel transaction by code");
        }
    }
    
    #region Archived Code
    // public async Task<IServiceResult> CreateAsync(Transaction entity)
    // {
    //     try
    //     {
    //         // Determine current system lang
    //         var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
    //             LanguageContext.CurrentLanguage);
    //         var isEng = lang == SystemLanguage.English;
    //         
    //         var transactionDto = _mapper.Map<TransactionDto>(entity);
    //         // Validate inputs using the generic validator
    //         var validationResult = await ValidatorExtensions.ValidateAsync(transactionDto);
    //         // Check for valid validations
    //         if (validationResult != null && !validationResult.IsValid)
    //         {
    //             // Convert ValidationResult to ValidationProblemsDetails.Errors
    //             var errors = validationResult.ToProblemDetails().Errors;
    //             throw new UnprocessableEntityException("Invalid Validations", errors);
    //         }
    //         
    //         TransactionDto response = new TransactionDto();
    //         //case 1: Create transaction for every kind of fine ( overdue + item is changed when return) 
    //         if (entity.FineId != null)
    //         {
    //             var fineBaseSpec = new BaseSpecification<Fine>(f => f.FineId == entity.FineId);
    //             fineBaseSpec.EnableSplitQuery();
    //             fineBaseSpec.ApplyInclude(q => q.Include(f => f.FinePolicy)
    //                 .Include(f => f.BorrowRecord)
    //                 .ThenInclude(br => br.LibraryCard)
    //                 .ThenInclude(li => li.Users));
    //             var fine = await _fineService.GetWithSpecAsync(fineBaseSpec);
    //             if (fine.Data is null)
    //             {
    //                 return new ServiceResult(ResultCodeConst.SYS_Warning0002,
    //                     StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002), "fine"));
    //             }
    //
    //             var fineValue = (FineDto)fine.Data!;
    //             response.TransactionCode = Guid.NewGuid().ToString();
    //             response.Amount = fineValue.FinePolicy.FixedFineAmount ?? 0;
    //             response.TransactionType = TransactionType.Fine;
    //             response.UserId = fineValue.BorrowRecord.LibraryCard.Users.First().UserId;
    //             response.TransactionStatus = TransactionStatus.Pending;
    //         }
    //
    //         // case2: Create transaction for card
    //         if (entity.LibraryCardPackageId != null)
    //         {
    //             //Get current user:
    //             var userBaseSpec = new BaseSpecification<User>(u => 
    //                 u.UserId.ToString().ToLower().Equals(entity.UserId.ToString().ToLower()));
    //             userBaseSpec.EnableSplitQuery();
    //             userBaseSpec.ApplyInclude(q => q.Include(u => u.LibraryCard)!);
    //             var user = await _userSvc.Value.GetWithSpecAsync(userBaseSpec);
    //             if (user.Data is null)
    //             {
    //                 var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
    //                 return new ServiceResult(ResultCodeConst.SYS_Warning0002,
    //                     StringUtils.Format(errMsg, isEng ? "user" : "bạn đọc"));
    //             }
    //
    //             var userValue = (UserDto)user.Data!;
    //             TransactionType transactionType;
    //             if (userValue.LibraryCard != null)
    //             {
    //                 transactionType = TransactionType.LibraryCardExtension;
    //             }
    //             else
    //             {
    //                 transactionType = TransactionType.LibraryCardRegister;
    //             }
    //
    //             var cardPackageBaseSpec = new BaseSpecification<LibraryCardPackage>(cp
    //                 => cp.LibraryCardPackageId == entity.LibraryCardPackageId);
    //             var cardPackage = await _cardPackageService.GetWithSpecAsync(cardPackageBaseSpec);
    //             if (cardPackage.Data is null)
    //             {
    //                 var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
    //                 return new ServiceResult(ResultCodeConst.SYS_Warning0002,
    //                     StringUtils.Format(errMsg, isEng ? "card-package" : "gói thẻ thư viện"));
    //             }
    //
    //             var cardPackageValue = (LibraryCardPackage)cardPackage.Data!;
    //             response.TransactionCode = Guid.NewGuid().ToString();
    //             response.Amount = cardPackageValue.Price;
    //             response.TransactionType = transactionType;
    //             response.TransactionStatus = TransactionStatus.Pending;
    //             response.UserId = userValue.UserId;
    //         }
    //
    //         // case 3: Create transaction for digital borrow 
    //         if (entity.DigitalBorrowId != null)
    //         {
    //             var digitalBorrowBaseSpec = new BaseSpecification<DigitalBorrow>(cp
    //                 => cp.DigitalBorrowId == entity.DigitalBorrowId);
    //             digitalBorrowBaseSpec.EnableSplitQuery();
    //             digitalBorrowBaseSpec.ApplyInclude(q => q.Include(db => db.LibraryResource));
    //             var digitalBorrow = await _digitalBorrowService.GetWithSpecAsync(digitalBorrowBaseSpec);
    //             if (digitalBorrow.Data is null)
    //             {
    //                 return new ServiceResult(ResultCodeConst.SYS_Warning0002,
    //                     StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002),
    //                         "card-package"));
    //             }
    //
    //             var digitalBorrowValue = (DigitalBorrowDto)digitalBorrow.Data!;
    //             response.TransactionCode = Guid.NewGuid().ToString();
    //             response.Amount = digitalBorrowValue.LibraryResource.BorrowPrice;
    //             response.TransactionType = TransactionType.DigitalBorrow;
    //             response.TransactionStatus = TransactionStatus.Pending;
    //         }
    //
    //         var transactionEntity = _mapper.Map<Transaction>(response);
    //         await _unitOfWork.Repository<Transaction, int>().AddAsync(transactionEntity);
    //         if (await _unitOfWork.SaveChangesAsync() <= 0)
    //         {
    //             return new ServiceResult(ResultCodeConst.SYS_Fail0001,
    //                 await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
    //         }
    //
    //         return new ServiceResult(ResultCodeConst.SYS_Success0001,
    //             await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001), response);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.Error(ex.Message);
    //         throw new Exception("Error invoke when process create transaction");
    //     }
    // }
    
    // public async Task<IServiceResult> GetAvailableTransactionType(string email)
    // { 
    //     // Determine current system lang
    //     var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
    //         LanguageContext.CurrentLanguage);
    //     var isEng = lang == SystemLanguage.English;
    //     
    //     var userBaseSpec = new BaseSpecification<User>(u => u.Email.Equals(email));
    //     var user = await _userSvc.Value.GetWithSpecAsync(userBaseSpec);
    //     if (user.Data != null)
    //     {
    //         return new ServiceResult(ResultCodeConst.SYS_Success0001,
    //             await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001), new List<string>
    //             {
    //                 nameof(TransactionType.LibraryCardExtension),
    //                 nameof(TransactionType.LibraryCardRegister),
    //                 nameof(TransactionType.DigitalBorrow)
    //             });
    //     }
    //     var employeeBaseSpec = new BaseSpecification<Employee>(e => e.Email.Equals(email));
    //     var employee = await _employeeService.GetWithSpecAsync(employeeBaseSpec);
    //     if (employee.Data != null)
    //     {
    //         return new ServiceResult(ResultCodeConst.SYS_Success0001,
    //             await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001), new List<string>
    //             {
    //                 nameof(TransactionType.Fine)
    //             });
    //     }
    //     var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
    //     return new ServiceResult(ResultCodeConst.SYS_Warning0002,
    //         StringUtils.Format(errMsg, isEng ? "user" : "bạn đọc"));
    // }
    //
    //
    // public Task<IServiceResult> GetTransactionById(int transactionId)
    // {
    //     throw new NotImplementedException();
    // }
    #endregion
}