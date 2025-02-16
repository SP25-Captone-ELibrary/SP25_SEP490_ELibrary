using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Dtos.Payments.PayOS;
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
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class InvoiceService : GenericService<Invoice, InvoiceDto, int>,
    IInvoiceService<InvoiceDto>
{
    private readonly IUserService<UserDto> _userSvc;
    private readonly ITransactionService<TransactionDto> _transactionService;
    private readonly PayOSSettings _monitor;

    public InvoiceService(
        IUserService<UserDto> userSvc,
        ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        ITransactionService<TransactionDto> transactionService,
        IMapper mapper, IOptionsMonitor<PayOSSettings> monitor,
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _userSvc = userSvc;
        _transactionService = transactionService;
        _monitor = monitor.CurrentValue;
    }

    public async Task<IServiceResult> GetAllByUserIdAsync(Guid userId, int pageIndex, int pageSize)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<Invoice>(br => br.UserId == userId);
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(i => i.Transactions)
                .ThenInclude(t => t.PaymentMethod)
            );

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

    public async Task<IServiceResult> GetAllByEmailAsync(string email, int pageIndex, int pageSize)
    {
        try
        {
            // Retrieve user information
            // Build spec
            var userBaseSpec = new BaseSpecification<User>(u => Equals(u.Email, email));
            // Apply include
            userBaseSpec.ApplyInclude(q => q
                .Include(u => u.LibraryCard)!
            );
            var userDto = (await _userSvc.GetWithSpecAsync(userBaseSpec)).Data as UserDto;
            if (userDto == null) // Not found user
            {
                // Data not found or empty
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                    new List<LibraryCardHolderInvoiceDto>());
            }

            // Build spec
            var baseSpec = new BaseSpecification<Invoice>(br => br.UserId == userDto.UserId);
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(i => i.Transactions)
                .ThenInclude(t => t.PaymentMethod)
            );

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
            throw new Exception("Error invoke when process get all invoice by email");
        }
    }

    public async Task<IServiceResult> CreatePayment(List<int> transactionIds, Guid userId)
    {
        try
        {
            //get user
            var user = await _userSvc.GetByIdAsync(userId);
            if (user.Data is null)
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                        StringUtils.Format(ResultCodeConst.SYS_Warning0002, "user"))
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
                Description = "Prep4IELTS Premium",
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
            Dictionary<string, int> transactionType = new Dictionary<string, int>()
            {
                { TransactionType.Fine.ToString(), 0 },
                { TransactionType.DigitalBorrow.ToString(), 0 },
                { TransactionType.LibraryCardRegister.ToString(), 0 },

            };
            foreach (var transactionId in transactionIds)
            {
                var transaction = await _transactionService.GetByIdAsync(transactionId);
                if (transaction.Data is null)
                {
                    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                        StringUtils.Format(ResultCodeConst.SYS_Warning0002, "transaction"));
                }

                var transactionValue = (TransactionDto)transaction.Data!;
                payOsPaymentRequest.Amount += (int)Math.Ceiling(transactionValue.Amount);
                if (transactionValue.FineId is not null)
                {
                    transactionType[TransactionType.Fine.ToString()] += 1;
                }
                {
                    transactionType[TransactionType.DigitalBorrow.ToString()] += 1;
                }
                {
                    transactionType[TransactionType.LibraryCardRegister.ToString()] += 1;
                }
            }
            


            return new ServiceResult();
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process create invoice");
        }
    }
}