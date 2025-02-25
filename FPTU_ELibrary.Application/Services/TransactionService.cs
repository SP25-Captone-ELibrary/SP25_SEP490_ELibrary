using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Payments;
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
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class TransactionService : GenericService<Transaction, TransactionDto, int>,
    ITransactionService<TransactionDto>
{
    // Lazy services
    private readonly Lazy<IUserService<UserDto>> _userSvc;

    private readonly ILibraryCardPackageService<LibraryCardPackageDto> _cardPackageService;
    private readonly IDigitalBorrowService<DigitalBorrowDto> _digitalBorrowService;
    private readonly IEmployeeService<EmployeeDto> _employeeService;
    private readonly IFineService<FineDto> _fineService;

    public TransactionService(
        Lazy<IUserService<UserDto>> userSvc,
        ILibraryCardPackageService<LibraryCardPackageDto> cardPackageService,
        IDigitalBorrowService<DigitalBorrowDto> digitalBorrowService,
        IEmployeeService<EmployeeDto> employeeService,
        ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        IMapper mapper, IFineService<FineDto> fineService,
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _userSvc = userSvc;
        _cardPackageService = cardPackageService;
        _digitalBorrowService = digitalBorrowService;
        _employeeService = employeeService;
        _fineService = fineService;
    }

    public override async Task<IServiceResult> UpdateAsync(int id, TransactionDto dto)
    {
        // Initiate service result
        var serviceResult = new ServiceResult();

        try
        {
            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(dto);
            // Check for valid validations
            if (validationResult != null && !validationResult.IsValid)
            {
                // Convert ValidationResult to ValidationProblemsDetails.Errors
                var errors = validationResult.ToProblemDetails().Errors;
                throw new UnprocessableEntityException("Invalid validations", errors);
            }

            // Retrieve the entity
            var existingEntity = await _unitOfWork.Repository<Transaction, int>().GetByIdAsync(id);
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, typeof(Transaction).ToString().ToLower()));
            }

            // Process add update entity
            // Map properties from dto to existingEntity
            existingEntity.InvoiceId = dto.InvoiceId;

            // Check if there are any differences between the original and the updated entity
            if (!_unitOfWork.Repository<Transaction, int>().HasChanges(existingEntity))
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
                serviceResult.Data = true;
                return serviceResult;
            }

            // Progress update when all require passed
            await _unitOfWork.Repository<Transaction, int>().UpdateAsync(existingEntity);

            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Fail0003;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003);
                serviceResult.Data = false;
            }

            // Mark as update success
            serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
            serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
            serviceResult.Data = true;
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw;
        }

        return serviceResult;
    }

    public async Task<IServiceResult> GetAllCardHolderTransactionByUserIdAsync(Guid userId, int pageIndex, int pageSize)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<Transaction>(br => br.UserId == userId);
            // Apply include
            // baseSpec.ApplyInclude(q => q
            //     .Include(i => i.PaymentMethod)
            // );

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
                .Include(i => i.DigitalBorrow)
                .Include(i => i.LibraryCardPackage)
                .Include(i => i.Fine)
                .Include(i => i.Invoice)!
                // .Include(i => i.PaymentMethod)
            );
            // Retrieve data with spec
            var existingEntity = await _unitOfWork.Repository<Transaction, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity != null)
            {
                // Convert to dto
                var transactionDto = _mapper.Map<TransactionDto>(existingEntity);

                // Get data successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001),
                    transactionDto.ToCardHolderTransactionDto());
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

    public async Task<IServiceResult> CreateAsync(Transaction entity)
    {
        try
        {
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            var transactionDto = _mapper.Map<TransactionDto>(entity);
            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(transactionDto);
            // Check for valid validations
            if (validationResult != null && !validationResult.IsValid)
            {
                // Convert ValidationResult to ValidationProblemsDetails.Errors
                var errors = validationResult.ToProblemDetails().Errors;
                throw new UnprocessableEntityException("Invalid Validations", errors);
            }
            
            TransactionDto response = new TransactionDto();
            //case 1: Create transaction for every kind of fine ( overdue + item is changed when return) 
            if (entity.FineId != null)
            {
                var fineBaseSpec = new BaseSpecification<Fine>(f => f.FineId == entity.FineId);
                fineBaseSpec.EnableSplitQuery();
                fineBaseSpec.ApplyInclude(q => q.Include(f => f.FinePolicy)
                    .Include(f => f.BorrowRecord)
                    .ThenInclude(br => br.LibraryCard)
                    .ThenInclude(li => li.Users));
                var fine = await _fineService.GetWithSpecAsync(fineBaseSpec);
                if (fine.Data is null)
                {
                    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                        StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002), "fine"));
                }

                var fineValue = (FineDto)fine.Data!;
                response.TransactionCode = Guid.NewGuid().ToString();
                response.Amount = fineValue.FinePolicy.FixedFineAmount ?? 0;
                response.TransactionType = TransactionType.Fine;
                response.UserId = fineValue.BorrowRecord.LibraryCard.Users.First().UserId;
                response.TransactionStatus = TransactionStatus.Pending;
            }

            // case2: Create transaction for card
            if (entity.LibraryCardPackageId != null)
            {
                //Get current user:
                var userBaseSpec = new BaseSpecification<User>(u => 
                    u.UserId.ToString().ToLower().Equals(entity.UserId.ToString().ToLower()));
                userBaseSpec.EnableSplitQuery();
                userBaseSpec.ApplyInclude(q => q.Include(u => u.LibraryCard)!);
                var user = await _userSvc.Value.GetWithSpecAsync(userBaseSpec);
                if (user.Data is null)
                {
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                        StringUtils.Format(errMsg, isEng ? "user" : "người dùng"));
                }

                var userValue = (UserDto)user.Data!;
                TransactionType transactionType;
                if (userValue.LibraryCard != null)
                {
                    transactionType = TransactionType.LibraryCardExtension;
                }
                else
                {
                    transactionType = TransactionType.LibraryCardRegister;
                }

                var cardPackageBaseSpec = new BaseSpecification<LibraryCardPackage>(cp
                    => cp.LibraryCardPackageId == entity.LibraryCardPackageId);
                var cardPackage = await _cardPackageService.GetWithSpecAsync(cardPackageBaseSpec);
                if (cardPackage.Data is null)
                {
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                        StringUtils.Format(errMsg, isEng ? "card-package" : "gói thẻ thư viện"));
                }

                var cardPackageValue = (LibraryCardPackage)cardPackage.Data!;
                response.TransactionCode = Guid.NewGuid().ToString();
                response.Amount = cardPackageValue.Price;
                response.TransactionType = transactionType;
                response.TransactionStatus = TransactionStatus.Pending;
                response.UserId = userValue.UserId;
            }

            // case 3: Create transaction for digital borrow 
            if (entity.DigitalBorrowId != null)
            {
                var digitalBorrowBaseSpec = new BaseSpecification<DigitalBorrow>(cp
                    => cp.DigitalBorrowId == entity.DigitalBorrowId);
                digitalBorrowBaseSpec.EnableSplitQuery();
                digitalBorrowBaseSpec.ApplyInclude(q => q.Include(db => db.LibraryResource));
                var digitalBorrow = await _digitalBorrowService.GetWithSpecAsync(digitalBorrowBaseSpec);
                if (digitalBorrow.Data is null)
                {
                    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                        StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002),
                            "card-package"));
                }

                var digitalBorrowValue = (DigitalBorrowDto)digitalBorrow.Data!;
                response.TransactionCode = Guid.NewGuid().ToString();
                response.Amount = digitalBorrowValue.LibraryResource.BorrowPrice;
                response.TransactionType = TransactionType.DigitalBorrow;
                response.TransactionStatus = TransactionStatus.Pending;
            }

            var transactionEntity = _mapper.Map<Transaction>(response);
            await _unitOfWork.Repository<Transaction, int>().AddAsync(transactionEntity);
            if (await _unitOfWork.SaveChangesAsync() <= 0)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
            }

            return new ServiceResult(ResultCodeConst.SYS_Success0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001), response);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process create transaction");
        }
    }

    public async Task<IServiceResult> GetAvailableTransactionType(string email)
    { 
        // Determine current system lang
        var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
            LanguageContext.CurrentLanguage);
        var isEng = lang == SystemLanguage.English;
        
        var userBaseSpec = new BaseSpecification<User>(u => u.Email.Equals(email));
        var user = await _userSvc.Value.GetWithSpecAsync(userBaseSpec);
        if (user.Data != null)
        {
            return new ServiceResult(ResultCodeConst.SYS_Success0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001), new List<string>
                {
                    nameof(TransactionType.LibraryCardExtension),
                    nameof(TransactionType.LibraryCardRegister),
                    nameof(TransactionType.DigitalBorrow)
                });
        }
        var employeeBaseSpec = new BaseSpecification<Employee>(e => e.Email.Equals(email));
        var employee = await _employeeService.GetWithSpecAsync(employeeBaseSpec);
        if (employee.Data != null)
        {
            return new ServiceResult(ResultCodeConst.SYS_Success0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001), new List<string>
                {
                    nameof(TransactionType.Fine)
                });
        }
        var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
        return new ServiceResult(ResultCodeConst.SYS_Warning0002,
            StringUtils.Format(errMsg, isEng ? "user" : "người dùng"));
    }
    

    public Task<IServiceResult> GetTransactionById(int transactionId)
    {
        throw new NotImplementedException();
    }
    
}