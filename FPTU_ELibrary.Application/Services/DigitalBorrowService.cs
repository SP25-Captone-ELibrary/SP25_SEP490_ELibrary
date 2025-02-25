using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Exceptions;
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

public class DigitalBorrowService : GenericService<DigitalBorrow, DigitalBorrowDto, int>,
    IDigitalBorrowService<DigitalBorrowDto>
{
    private readonly Lazy<IUserService<UserDto>> _userSvc;
    private readonly Lazy<ITransactionService<TransactionDto>>  _transactionService;

    public DigitalBorrowService(
        Lazy<IUserService<UserDto>> userSvc,
        Lazy<ITransactionService<TransactionDto>> transactionService,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _userSvc = userSvc;
        _transactionService = transactionService;
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
                    StringUtils.Format(errMsg, isEng ? "reader" : "bạn đọc"));
            }
            
            // Build spec
            var baseSpec = new BaseSpecification<DigitalBorrow>(br => 
                br.UserId == userDto.UserId && br.DigitalBorrowId == digitalBorrowId);
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(db => db.LibraryResource)
                .Include(db => db.Transactions)
                    .ThenInclude(tr => tr.Invoice)
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

    public async Task<IServiceResult> CreateTransactionForDigitalBorrow(string email, int resourceId)
    {
        // Determine current system lang
        var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
            LanguageContext.CurrentLanguage);
        var isEng = lang == SystemLanguage.English;
		
        // Get User By email
        var userBaseSpec = new BaseSpecification<User>(u => u.Email == email);
        var user = await _userSvc.Value.GetWithSpecAsync(userBaseSpec);
        if (user.Data is null)
        {
            var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
            return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                StringUtils.Format(errMsg, isEng ? "user" : "người dùng"));
        }

        var userValue = (UserDto)user.Data!;
        //Create Borrow Record
        DigitalBorrowDto newRecord = new DigitalBorrowDto()
        {
            UserId = userValue.UserId,
            ExtensionCount = 0,
            ResourceId = resourceId,
            Status = BorrowDigitalStatus.Active,
            RegisterDate = DateTime.Now,
            IsExtended = false,
            ExpiryDate = DateTime.Now.AddMonths(1)
        };
        var entity = _mapper.Map<DigitalBorrow>(newRecord);
        await _unitOfWork.Repository<DigitalBorrow, int>().AddAsync(entity);
        var addStatus = await _unitOfWork.SaveChangesAsync();
        if (await _unitOfWork.SaveChangesAsync() <= 0)
        {
            return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
        }

        TransactionDto transaction = new TransactionDto();
        transaction.TransactionCode = Guid.NewGuid().ToString();
        transaction.TransactionType = TransactionType.DigitalBorrow;
        transaction.UserId = userValue.UserId;
        transaction.DigitalBorrowId = entity.DigitalBorrowId;
        var transactionEntity = _mapper.Map<Transaction>(transaction);
        var result = await _transactionService.Value.CreateAsync(transactionEntity);
        if(result.Data is null) return result;

        return new ServiceResult(ResultCodeConst.SYS_Success0001,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001));
    }
    
}