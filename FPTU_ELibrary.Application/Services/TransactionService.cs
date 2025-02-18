using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class TransactionService : GenericService<Transaction, TransactionDto, int>,
    ITransactionService<TransactionDto>
{
    private readonly Lazy<IUserService<UserDto>> _userSvc;

    public TransactionService(
        Lazy<IUserService<UserDto>> userSvc,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _userSvc = userSvc;
    }

    public async Task<IServiceResult> GetAllCardHolderTransactionByUserIdAsync(Guid userId, int pageIndex, int pageSize)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<Transaction>(br => br.UserId == userId);   
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(i => i.PaymentMethod)
            );
            
            // Add default order by
            baseSpec.AddOrderByDescending(i => i.CreatedAt);
            
            // Count total borrow request
            var totalTransactionWithSpec = await _unitOfWork.Repository<Transaction, int>().CountAsync(baseSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalTransactionWithSpec / pageSize);

            // Set pagination to specification after count total transaction
            if (pageIndex > totalPage
                || pageIndex < 1) // Exceed total page or page index smaller than 1
            {
                pageIndex = 1; // Set default to first page
            }

            // Apply pagination
            baseSpec.ApplyPaging(skip: pageSize * (pageIndex - 1), take: pageSize);
            
            // Retrieve data with spec
            var entities = await _unitOfWork.Repository<Transaction, int>()
                .GetAllWithSpecAsync(baseSpec);
            if (entities.Any())
            {
                // Convert to dto collection
                var transactionDtos = _mapper.Map<List<TransactionDto>>(entities);

                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<LibraryCardHolderTransactionDto>(
                    transactionDtos.Select(br => br.ToCardHolderTransactionDto()),
                    pageIndex, pageSize, totalPage, totalTransactionWithSpec);

                // Response with pagination 
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }
            
            // Not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                new List<LibraryCardHolderTransactionDto>());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get all transaction by user id");
        }
    }

    public async Task<IServiceResult> GetCardHolderTransactionByIdAsync(Guid userId, int transactionId)
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
                    StringUtils.Format(errMsg, isEng ? "reader" : "bạn đọc"));
            }
            
            // Build spec
            var baseSpec = new BaseSpecification<Transaction>(br => 
                br.UserId == userDto.UserId && br.TransactionId == transactionId);
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(i => i.PaymentMethod)
                .Include(i => i.DigitalBorrow)
                .Include(i => i.LibraryCardPackage)
                .Include(i => i.Fine)
                .Include(i => i.Invoice)
            );
            // Retrieve data with spec
            var existingEntity = await _unitOfWork.Repository<Transaction, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity != null)
            {
                // Convert to dto
                var transactionDto = _mapper.Map<TransactionDto>(existingEntity);
                
                // Get data successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001), transactionDto.ToCardHolderTransactionDto());
            }
            
            // Data not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get library card holder's transaction by id");
        }
    }
}