using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using FPTU_ELibrary.Domain.Specifications.Params;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class BorrowRequestService : GenericService<BorrowRequest, BorrowRequestDto, int>,
    IBorrowRequestService<BorrowRequestDto>
{
    // Lazy services
    private readonly Lazy<ILibraryItemInstanceService<LibraryItemInstanceDto>> _itemInstanceSvc;
    private readonly Lazy<IUserService<UserDto>> _userSvc;

    private readonly ILibraryCardService<LibraryCardDto> _cardSvc;
    private readonly ILibraryItemInventoryService<LibraryItemInventoryDto> _inventorySvc;
    private readonly ILibraryItemService<LibraryItemDto> _libItemSvc;
    private readonly BorrowSettings _borrowSettings;

    public BorrowRequestService(
        // Lazy services
        Lazy<ILibraryItemInstanceService<LibraryItemInstanceDto>> itemInstanceSvc,
        Lazy<IUserService<UserDto>> userSvc,

        ILibraryCardService<LibraryCardDto> cardSvc,
        ILibraryItemService<LibraryItemDto> libItemSvc,
        ILibraryItemInventoryService<LibraryItemInventoryDto> inventorySvc,
        IOptionsMonitor<BorrowSettings> monitor,
        ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _cardSvc = cardSvc;
        _userSvc = userSvc;
        _libItemSvc = libItemSvc;
        _inventorySvc = inventorySvc;
        _itemInstanceSvc = itemInstanceSvc;
        _borrowSettings = monitor.CurrentValue;
    }

    public override async Task<IServiceResult> GetAllWithSpecAsync(ISpecification<BorrowRequest> spec,
        bool tracked = true)
    {
        try
        {
            // Check for proper specification
            var borrowReqSpec = spec as BorrowRequestSpecification;
            if (borrowReqSpec == null) // is null specification
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }

            // Count total borrow request
            var totalBorrowReqWithSpec = await _unitOfWork.Repository<BorrowRequest, int>().CountAsync(borrowReqSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalBorrowReqWithSpec / borrowReqSpec.PageSize);

            // Set pagination to specification after count total borrow req
            if (borrowReqSpec.PageIndex > totalPage
                || borrowReqSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
                borrowReqSpec.PageIndex = 1; // Set default to first page
            }

            // Apply pagination
            borrowReqSpec.ApplyPaging(
                skip: borrowReqSpec.PageSize * (borrowReqSpec.PageIndex - 1),
                take: borrowReqSpec.PageSize);

            // Get all with spec
            var entities = await _unitOfWork.Repository<BorrowRequest, int>()
                .GetAllWithSpecAsync(borrowReqSpec, tracked: false);

            if (entities.Any()) // Exist data
            {
                // Convert to dto collection 
                var borrowReqDtos = _mapper.Map<List<BorrowRequestDto>>(entities);

                // Set null borrow request details
                borrowReqDtos.ForEach(br => br.BorrowRequestDetails = new List<BorrowRequestDetailDto>());

                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<BorrowRequestDto>(borrowReqDtos,
                    borrowReqSpec.PageIndex, borrowReqSpec.PageSize, totalPage, totalBorrowReqWithSpec);

                // Response with pagination 
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }

            // Not found any data
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                // Mapping entities to dto 
                _mapper.Map<IEnumerable<BorrowRequestDto>>(entities));
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress get all borrow data");
        }
    }

    public override async Task<IServiceResult> GetByIdAsync(int id)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Build specification
            var baseSpec = new BaseSpecification<BorrowRequest>(br => br.BorrowRequestId == id);
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(br => br.BorrowRequestDetails)
                .ThenInclude(brd => brd.LibraryItem)
                .ThenInclude(li => li.Shelf)
                .Include(br => br.BorrowRequestDetails)
                .ThenInclude(brd => brd.LibraryItem)
                .ThenInclude(li => li.Category)
                .Include(br => br.BorrowRequestDetails)
                .ThenInclude(brd => brd.LibraryItem)
                .ThenInclude(li => li.LibraryItemInstances)
                .Include(br => br.LibraryCard)
            );
            // Retrieve with spec
            var existingEntity = await _unitOfWork.Repository<BorrowRequest, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                // Msg: Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "borrow request" : "lịch sử đăng ký mượn"));
            }

            // Get data success
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                _mapper.Map<BorrowRequestDto>(existingEntity));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress get borrow data by id");
        }
    }

    public async Task<IServiceResult> GetAllByEmailAsync(string email, ISpecification<BorrowRequest> spec)
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
            var userDto = (await _userSvc.Value.GetWithSpecAsync(userBaseSpec)).Data as UserDto;
            if (userDto == null) // Not found user
            {
                // Data not found or empty
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                    new List<BorrowRequestDto>());
            }

            if (userDto.LibraryCardId == null)
            {
                // Data not found or empty
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                    new List<BorrowRequestDto>());
            }

            // Check for proper specification
            var borrowReqSpec = spec as BorrowRequestSpecification;
            if (borrowReqSpec == null) // is null specification
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }

            // Add filter 
            borrowReqSpec.AddFilter(br => br.LibraryCardId == userDto.LibraryCardId);

            // Count total borrow request
            var totalBorrowReqWithSpec = await _unitOfWork.Repository<BorrowRequest, int>().CountAsync(borrowReqSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalBorrowReqWithSpec / borrowReqSpec.PageSize);

            // Set pagination to specification after count total borrow req
            if (borrowReqSpec.PageIndex > totalPage
                || borrowReqSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
                borrowReqSpec.PageIndex = 1; // Set default to first page
            }

            // Apply pagination
            borrowReqSpec.ApplyPaging(
                skip: borrowReqSpec.PageSize * (borrowReqSpec.PageIndex - 1),
                take: borrowReqSpec.PageSize);

            // Get all with spec
            var entities = await _unitOfWork.Repository<BorrowRequest, int>()
                .GetAllWithSpecAsync(borrowReqSpec, tracked: false);

            if (entities.Any()) // Exist data
            {
                // Convert to dto collection 
                var borrowReqDtos = _mapper.Map<IEnumerable<BorrowRequestDto>>(entities);

                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<BorrowRequestDto>(borrowReqDtos,
                    borrowReqSpec.PageIndex, borrowReqSpec.PageSize, totalPage, totalBorrowReqWithSpec);

                // Response with pagination 
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }

            // Not found any data
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                // Mapping entities to dto 
                _mapper.Map<IEnumerable<BorrowRequestDto>>(entities));
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception($"Error invoke when progress get all borrow for email: {email}");
        }
    }

    public async Task<IServiceResult> GetAllCardHolderBorrowRequestByUserIdAsync(Guid userId, int pageIndex,
        int pageSize)
    {
        try
        {
            // Retrieve user information
            // Build spec
            var userBaseSpec = new BaseSpecification<User>(u => Equals(u.UserId, userId));
            // Apply include
            userBaseSpec.ApplyInclude(q => q
                .Include(u => u.LibraryCard)!
            );
            var userDto = (await _userSvc.Value.GetWithSpecAsync(userBaseSpec)).Data as UserDto;
            if (userDto == null) // Not found user
            {
                // Data not found or empty
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                    new List<BorrowRequestDto>());
            }

            // Build spec
            var baseSpec = new BaseSpecification<BorrowRequest>(br => br.LibraryCardId == userDto.LibraryCardId);
            // Add default order by
            baseSpec.AddOrderByDescending(br => br.RequestDate);

            // Count total borrow request
            var totalReqWithSpec = await _unitOfWork.Repository<BorrowRequest, int>().CountAsync(baseSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalReqWithSpec / pageSize);

            // Set pagination to specification after count total borrow request
            if (pageIndex > totalPage
                || pageIndex < 1) // Exceed total page or page index smaller than 1
            {
                pageIndex = 1; // Set default to first page
            }

            // Apply pagination
            baseSpec.ApplyPaging(skip: pageSize * (pageIndex - 1), take: pageSize);

            // Retrieve data with spec
            var entities = await _unitOfWork.Repository<BorrowRequest, int>()
                .GetAllWithSpecAsync(baseSpec);
            if (entities.Any())
            {
                // Convert to dto collection
                var reqDtos = _mapper.Map<List<BorrowRequestDto>>(entities);

                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<LibraryCardHolderBorrowRequestDto>(
                    reqDtos.Select(br => br.ToLibraryCardBorrowRequestDto()),
                    pageIndex, pageSize, totalPage, totalReqWithSpec);

                // Response with pagination 
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }

            // Not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                new List<LibraryCardHolderBorrowRequestDto>());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke while process get all borrow request by user id");
        }
    }

    public async Task<IServiceResult> GetByIdAsync(string email, int id)
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
            var userDto = (await _userSvc.Value.GetWithSpecAsync(userBaseSpec)).Data as UserDto;
            if (userDto == null || userDto.LibraryCardId == null) // Not found user
            {
                // Data not found or empty
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
            }

            // Build borrow spec
            var borrowReqSpec = new BaseSpecification<BorrowRequest>(br => br.BorrowRequestId == id &&
                                                                           br.LibraryCardId == userDto.LibraryCardId);
            // Apply include 
            borrowReqSpec.ApplyInclude(q => q
                .Include(br => br.BorrowRequestDetails)
                .ThenInclude(brd => brd.LibraryItem)
                .ThenInclude(li => li.LibraryItemInstances)
            );
            // Retrieve with spec
            var existingEntity = await _unitOfWork.Repository<BorrowRequest, int>().GetWithSpecAsync(borrowReqSpec);
            if (existingEntity != null)
            {
                // Get data successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                    _mapper.Map<BorrowRequestDto>(existingEntity));
            }

            // Data not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception($"Error invoke when progress get borrow by id for email: {email}");
        }
    }

    public async Task<IServiceResult> GetCardHolderBorrowRequestByIdAsync(Guid userId, int id)
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
            var baseSpec = new BaseSpecification<BorrowRequest>(br => br.LibraryCardId == userDto.LibraryCardId);
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(br => br.BorrowRequestDetails)
                    .ThenInclude(brd => brd.LibraryItem)
                        .ThenInclude(brd => brd.Category)
                .Include(br => br.BorrowRequestDetails)
                    .ThenInclude(brd => brd.LibraryItem)
                        .ThenInclude(brd => brd.LibraryItemAuthors)
                            .ThenInclude(lia => lia.Author)
            );
            var existingEntity = await _unitOfWork.Repository<BorrowRequest, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity != null)
            {
                // Convert to dto 
                var dto = _mapper.Map<BorrowRequestDto>(existingEntity);
                
                // Get data successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                    dto.ToLibraryCardBorrowRequestDto());
            }
            
            // Data not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get card holder borrow request by id");
        }
    }

    public async Task<IServiceResult> CreateAsync(string email, BorrowRequestDto dto)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(dto);
            // Check for valid validations
            if (validationResult != null && !validationResult.IsValid)
            {
                // Convert ValidationResult to ValidationProblemsDetails.Errors
                var errors = validationResult.ToProblemDetails().Errors;
                throw new UnprocessableEntityException("Invalid Validations", errors);
            }

            // Retrieve user information
            // Build spec
            var userBaseSpec = new BaseSpecification<User>(u => Equals(u.Email, email));
            // Apply include
            userBaseSpec.ApplyInclude(q => q
                .Include(u => u.LibraryCard)!
            );
            var userDto = (await _userSvc.Value.GetWithSpecAsync(userBaseSpec)).Data as UserDto;
            if (userDto == null) throw new ForbiddenException(); // Not found user 
            
            // Try parse card id to Guid type 
            Guid.TryParse(userDto.LibraryCardId.ToString(), out var validCardId);
            // Check exist library card
            if (validCardId == Guid.Empty)
            {
                // You need a library card to access this service
                return new ServiceResult(ResultCodeConst.LibraryCard_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0004));
            }

            // Validate library card 
            var validateCardRes = await _cardSvc.CheckCardValidityAsync(validCardId);
            // Return invalid card
            if (validateCardRes.ResultCode != ResultCodeConst.LibraryCard_Success0001) return validateCardRes;

            // Retrieve library card information
            var libCard = (await _cardSvc.GetByIdAsync(validCardId)).Data as LibraryCardDto;
            if (libCard == null)
            {
                // Unknown error
                return new ServiceResult(ResultCodeConst.SYS_Warning0006,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0006));
            }

            // Check exist any details 
            if (!dto.BorrowRequestDetails.Any())
            {
                var msg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0001);
                // Required at least {0} items to process
                return new ServiceResult(ResultCodeConst.Borrow_Warning0001,
                    StringUtils.Format(msg, "1"));
            }
            else
            {
                // Validate borrow amount before handling each request detail 
                var validateAmountRes = await ValidateBorrowAmountAsync(
                    totalItem: dto.BorrowRequestDetails.Count,
                    libCard: libCard);
                if (validateAmountRes != null) return validateAmountRes;
            }

            // Custom errors
            var customErrs = new Dictionary<string, string[]>();
            // Initialize unique detail check
            var uniqueDetailSet = new HashSet<int>();

            // Convert to list 
            var detailList = dto.BorrowRequestDetails.ToList();
            // Initialize borrow details
            var borrowDetails = new List<BorrowRequestDetailDto>();
            // Iterate each of detail to check for quantity availability
            for (int i = 0; i < detailList.Count; ++i)
            {
                var detail = detailList[i];

                // Check exist item 
                var isItemExist =
                    (await _libItemSvc.AnyAsync(li => li.LibraryItemId == detail.LibraryItemId)).Data is true;
                if (!isItemExist)
                {
                    // Add error
                    customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                        key: $"libraryItemIds[{i}]",
                        msg: StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002),
                            isEng ? "item" : "tài liệu"));
                }
                else
                {
                    // Check duplicate
                    if (!uniqueDetailSet.Add(detail.LibraryItemId))
                    {
                        // Add error
                        customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                            key: $"libraryItemIds[{i}]",
                            // Duplicate items are not allowed. You can only borrow one copy of each item per time
                            msg: await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0003));
                    }

                    // Check already borrowed the item 
                    var isAlreadyBorrowed = await _unitOfWork.Repository<BorrowRequest, int>()
                        .AnyAsync(br =>
                                br.Status != BorrowRequestStatus.Expired && // Exclude elements with expired status
                                br.Status != BorrowRequestStatus.Cancelled && // Exclude elements with cancelled status
                                br.LibraryCardId == libCard.LibraryCardId && // With specific library card 
                                br.BorrowRequestDetails.Any(brd =>
                                    brd.LibraryItemId == detail.LibraryItemId) // With specific item
                        );
                    if (isAlreadyBorrowed)
                    {
                        // Add error
                        customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                            key: $"libraryItemIds[{i}]",
                            // The item is currently borrowed and cannot be borrowed again
                            msg: await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0004));
                    }

                    // Retrieving item inventory 
                    var itemIven =
                        (await _inventorySvc.GetByIdAsync(id: detail.LibraryItemId)).Data as LibraryItemInventoryDto;
                    if (itemIven == null)
                    {
                        // Unknown error
                        return new ServiceResult(ResultCodeConst.SYS_Warning0006,
                            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0006));
                    }

                    // Check available item units
                    if (itemIven.AvailableUnits == 0)
                    {
                        // Add error
                        customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                            key: $"libraryItemIds[{i}]",
                            // Item quantity is not available
                            msg: await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0002));
                    }

                    // Add borrow detail
                    borrowDetails.Add(new()
                    {
                        LibraryItemId = detail.LibraryItemId,
                    });

                    // Update inventory total
                    if (itemIven.AvailableUnits > 0)
                    {
                        // Reduce available units
                        itemIven.AvailableUnits--;
                        // Increase request units
                        itemIven.RequestUnits++;
                    }

                    // Process update without save change
                    await _inventorySvc.UpdateWithoutSaveChangesAsync(itemIven);
                }
            }

            // Check if any error invoke
            if (customErrs.Any()) throw new UnprocessableEntityException("Invalid data", customErrs);

            // Count request amount with borrowed or requested amount to check for threshold
            // Check for total borrowing items in DB
            // Build spec
            var borrowReqSpec = new BaseSpecification<BorrowRequest>(br =>
                br.LibraryCardId == validCardId && // with specific library card
                (br.Status == BorrowRequestStatus.Borrowed || // In borrowed status OR 
                 br.Status == BorrowRequestStatus.Created)); // In created status
            // Apply include
            borrowReqSpec.ApplyInclude(q => q
                .Include(br => br.BorrowRequestDetails)
            );
            // Retrieve all borrow request of specific library card with spec
            var borrowReqEntities = await _unitOfWork.Repository<BorrowRequest, int>()
                .GetAllWithSpecAsync(borrowReqSpec);
            // Convert to list 
            var borrowReqList = borrowReqEntities.ToList();
            if (borrowReqList.Any())
            {
                // Check total borrowing items
                var totalBorrowingAmount = 0;
                foreach (var borrowReq in borrowReqList)
                {
                    totalBorrowingAmount += borrowReq.BorrowRequestDetails.Count;
                }

                // Borrow request detail reach the threshold
                var sumTotal =
                    totalBorrowingAmount +
                    dto.BorrowRequestDetails.Count; // Sum current borrowing amount with total request amount
                // Validate borrow amount 
                var validateAmountRes = await ValidateBorrowAmountAsync(
                    totalItem: sumTotal,
                    libCard: libCard);
                if (validateAmountRes != null) return validateAmountRes;
            }

            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

            // Progress create request
            var borrowReqDto = new BorrowRequestDto()
            {
                LibraryCardId = validCardId,
                BorrowRequestDetails = borrowDetails,
                TotalRequestItem = borrowDetails.Count,
                RequestDate = currentLocalDateTime,
                ExpirationDate = currentLocalDateTime.AddDays(_borrowSettings.BorrowRequestExpirationInDays),
                Status = BorrowRequestStatus.Created,
                IsReminderSent = false,
            };
            await _unitOfWork.Repository<BorrowRequest, int>().AddAsync(_mapper.Map<BorrowRequest>(borrowReqDto));

            // Update library item borrow more status if it true in current request
            if (libCard.IsAllowBorrowMore)
            {
                await _cardSvc.UpdateBorrowMoreStatusWithoutSaveChangesAsync(validCardId);
            }

            // Save DB
            var isSaved = await _unitOfWork.SaveChangesWithTransactionAsync() > 0;
            if (isSaved)
            {
                // Total {0} item(s) have been borrowed successfully
                return new ServiceResult(ResultCodeConst.Borrow_Success0001,
                    StringUtils.Format(
                        await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Success0001),
                        detailList.Count.ToString()));
            }

            // An error occurred, the item borrowing registration failed
            return new ServiceResult(ResultCodeConst.Borrow_Fail0001,
                await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Fail0001));
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process create borrow request from public");
        }
    }

    public async Task<IServiceResult> CancelAsync(string email, int id, string? cancellationReason)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Retrieve user information
            // Build spec
            var userBaseSpec = new BaseSpecification<User>(u => Equals(u.Email, email));
            // Apply include
            userBaseSpec.ApplyInclude(q => q
                .Include(u => u.LibraryCard)!
            );
            var userDto = (await _userSvc.Value.GetWithSpecAsync(userBaseSpec)).Data as UserDto;
            if (userDto == null) throw new ForbiddenException(); // Not found user 
            
            // Try parse card id to Guid type 
            Guid.TryParse(userDto.LibraryCardId.ToString(), out var validCardId);
            // Check exist library card
            if (validCardId == Guid.Empty)
            {
                // You need a library card to access this service
                return new ServiceResult(ResultCodeConst.LibraryCard_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0004));
            }

            // Validate library card 
            var validateCardRes = await _cardSvc.CheckCardValidityAsync(validCardId);
            // Return invalid card
            if (validateCardRes.ResultCode != ResultCodeConst.LibraryCard_Success0001) return validateCardRes;
            
            // Build specification
            var baseSpec = new BaseSpecification<BorrowRequest>(br => br.BorrowRequestId == id);
            // Apply include 
            baseSpec.ApplyInclude(q => q
                // Include request details
                .Include(br => br.BorrowRequestDetails)
            );
            // Retrieve entity by id 
            var existingEntity = await _unitOfWork.Repository<BorrowRequest, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng ? "borrow request" : "yêu cầu mượn"));
            }
            
            // Check whether request library card is correct
            if (!Equals(existingEntity.LibraryCardId, validCardId))
            {
                // Cannot process as library card is incorrect
                return new ServiceResult(ResultCodeConst.Borrow_Warning0008,
                    await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0008));
            }
            
            // Check for current request status
            if (existingEntity.Status != BorrowRequestStatus.Created) // Different from Created status
            {
                // Is Expired
                if (existingEntity.Status == BorrowRequestStatus.Expired)
                {
                    // Cannot process as borrow request has been expired
                    return new ServiceResult(ResultCodeConst.Borrow_Warning0007,
                        await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0007));
                }
                
                // Cannot cancel because item has been proceeded
                return new ServiceResult(ResultCodeConst.Borrow_Warning0006,
                    await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0006));
            }
            
            // Iterate each of detail to update for quantity availability
            foreach (var detail in existingEntity.BorrowRequestDetails)
            {
                // Retrieving item inventory 
                var itemIven =
                    (await _inventorySvc.GetByIdAsync(id: detail.LibraryItemId)).Data as LibraryItemInventoryDto;
                if (itemIven == null)
                {
                    // Unknown error
                    return new ServiceResult(ResultCodeConst.SYS_Warning0006,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0006));
                }
                
                // Updated inventory units
                if (itemIven.RequestUnits > 0)
                {
                    // Request units
                    itemIven.RequestUnits--;
                    // Available units
                    itemIven.AvailableUnits++;
                }
                 // Process update without save change
                await _inventorySvc.UpdateWithoutSaveChangesAsync(itemIven);
            }
            
            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Add cancellation props 
            existingEntity.Status = BorrowRequestStatus.Cancelled;
            existingEntity.CancelledAt = currentLocalDateTime;
            existingEntity.CancellationReason = cancellationReason;
            
            // Process update
            await _unitOfWork.Repository<BorrowRequest, int>().UpdateAsync(existingEntity);
            // Save DB
            var isSaved = await _unitOfWork.SaveChangesWithTransactionAsync() > 0;
            if (isSaved)
            {
                // Cancel borrowing {0} item(s) successfully
                var msg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Success0002);
                return new ServiceResult(ResultCodeConst.Borrow_Success0002,
                    StringUtils.Format(msg, existingEntity.BorrowRequestDetails.Count.ToString()), true);
            }
            
            // Fail to update
            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
        }
        catch (ForbiddenException)
        {
            throw;
        }    
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process cancel borrow request");
        }
    }

    public async Task<IServiceResult> UpdateStatusWithoutSaveChangesAsync(int id, BorrowRequestStatus status)
    {
        try
        {
            // Check exist borrow request
            var existingEntity = await _unitOfWork.Repository<BorrowRequest, int>()
                .GetByIdAsync(id);
            if (existingEntity == null)
            {
                // Fail to update
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
            }
            
            // Check for status change
            if (Equals(existingEntity.Status, status))
            {
                // Mark as update success
                return new ServiceResult(ResultCodeConst.SYS_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
            }
            
            // Update prop
            existingEntity.Status = status;
            
            // Process update without save changes
            await _unitOfWork.Repository<BorrowRequest, int>().UpdateAsync(existingEntity);
            
            // Mark as update success
            return new ServiceResult(ResultCodeConst.SYS_Success0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update borrow request status without save changes");
        }
    }

    public async Task<IServiceResult> CheckExistBarcodeInRequestAsync(int id, string barcode)
    {
        try
        {
            // Determine current lang context
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            var libItemDto = (await _libItemSvc.GetByInstanceBarcodeAsync(barcode)).Data as LibraryItemDto;
            if (libItemDto == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
                    StringUtils.Format(errMsg, isEng ? "item instance by barcode" : "tài liệu"));
            }
            
            // Build spec
            var baseSpec = new BaseSpecification<BorrowRequest>(br =>
                br.BorrowRequestId == id &&
                br.BorrowRequestDetails.Any(
                    brd => brd.LibraryItemId == libItemDto.LibraryItemId));
            // Check exist
            var isExistInRequest = await _unitOfWork.Repository<BorrowRequest, int>().AnyAsync(baseSpec);
            if (!isExistInRequest)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng
                        ? $"item match code '{barcode}' in registered borrow request"
                        : $"tài liệu với số ĐKCB '{barcode}' trong yêu cầu đăng ký mượn"), false);
            }
            
            // Is exist -> Return library item instance  
            return await _itemInstanceSvc.Value.GetByBarcodeAsync(barcode);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when check exist barcode in request");
        }
    }
    
    private async Task<IServiceResult?> ValidateBorrowAmountAsync(int totalItem, LibraryCardDto libCard)
    {
        // Max amount to borrow (if any)
        var maxAmountToBorrow = libCard.MaxItemOnceTime;
        // Default Threshold amount
        var defaultThresholdTotal = _borrowSettings.BorrowAmountOnceTime;
        
        // Check for default amount boundary
        if (totalItem > defaultThresholdTotal) // Total item borrow exceed than default max amount to borrow
        {
            // Msg: You can borrow up to {0} items at a time
            var msg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0005);
            
            if (!libCard.IsAllowBorrowMore) // Is not allow to borrow more
            {
                return new ServiceResult(ResultCodeConst.Borrow_Warning0005, StringUtils.Format(msg, defaultThresholdTotal.ToString()));
            }

            if (libCard.IsAllowBorrowMore && // Is allow to borrow more
                maxAmountToBorrow > 0 && totalItem > maxAmountToBorrow) // Total item borrow not exceed max amount to borrow from lib card
            {
                return new ServiceResult(ResultCodeConst.Borrow_Warning0005, StringUtils.Format(msg, maxAmountToBorrow.ToString()));
            }
        }

        return null;
    }
}