using CloudinaryDotNet.Core;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
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
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class BorrowRecordService : GenericService<BorrowRecord, BorrowRecordDto, int>,
    IBorrowRecordService<BorrowRecordDto>
{
    // Lazy services
    private readonly Lazy<ILibraryItemInstanceService<LibraryItemInstanceDto>> _itemInstanceSvc;
    
    private readonly ICloudinaryService _cloudSvc;
    private readonly IUserService<UserDto> _userSvc;
    private readonly IEmployeeService<EmployeeDto> _employeeSvc;
    private readonly ILibraryCardService<LibraryCardDto> _cardSvc;
    private readonly IBorrowRequestService<BorrowRequestDto> _borrowReqSvc;
    private readonly ICategoryService<CategoryDto> _cateSvc;
    private readonly ILibraryItemConditionService<LibraryItemConditionDto> _conditionSvc;

    private readonly BorrowSettings _borrowSettings;
    
    public BorrowRecordService(
        // Lazy services
        Lazy<ILibraryItemInstanceService<LibraryItemInstanceDto>> itemInstanceSvc,
        
        // Normal services
        ICategoryService<CategoryDto> cateSvc,
        IBorrowRequestService<BorrowRequestDto> borrowReqSvc,
        ILibraryCardService<LibraryCardDto> cardSvc,
        ILibraryItemConditionService<LibraryItemConditionDto> conditionSvc,
        IEmployeeService<EmployeeDto> employeeSvc,
        IUserService<UserDto> userSvc,
        ICloudinaryService cloudSvc,
        IOptionsMonitor<BorrowSettings> monitor,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _cateSvc = cateSvc;
        _cardSvc = cardSvc;
        _cloudSvc = cloudSvc;
        _conditionSvc = conditionSvc;
        _userSvc = userSvc;
        _employeeSvc = employeeSvc;
        _borrowReqSvc = borrowReqSvc;
        _itemInstanceSvc = itemInstanceSvc;
        _borrowSettings = monitor.CurrentValue;
    }

    public override async Task<IServiceResult> GetAllWithSpecAsync(ISpecification<BorrowRecord> spec, bool tracked = true)
    {
	    try
	    {
		    // Check for proper specification
		    var borrowRecSpec = spec as BorrowRecordSpecification;
		    if (borrowRecSpec == null) // is null specification
		    {
			    return new ServiceResult(ResultCodeConst.SYS_Fail0002,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
		    }	
		    
		    // Count total borrow record
		    var totalActualBorrowRec = await _unitOfWork.Repository<BorrowRecord, int>().CountAsync(borrowRecSpec);
		    // Count total page
		    var totalPage = (int)Math.Ceiling((double)totalActualBorrowRec / borrowRecSpec.PageSize);

		    // Set pagination to specification after count total borrow rec
		    if (borrowRecSpec.PageIndex > totalPage
		        || borrowRecSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
		    {
			    borrowRecSpec.PageIndex = 1; // Set default to first page
		    }

		    // Apply pagination
		    borrowRecSpec.ApplyPaging(
			    skip: borrowRecSpec.PageSize * (borrowRecSpec.PageIndex - 1),
			    take: borrowRecSpec.PageSize);
		    
		    // Get all with spec
		    var entities = await _unitOfWork.Repository<BorrowRecord, int>()
			    .GetAllWithSpecAndSelectorAsync(borrowRecSpec, selector: br => new BorrowRecord()
			    {
				    BorrowRecordId = br.BorrowRecordId,
                    BorrowRequestId = br.BorrowRequestId,
                    LibraryCardId = br.LibraryCardId,
                    BorrowDate = br.BorrowDate,
                    DueDate = br.DueDate,
                    ReturnDate = br.ReturnDate,
                    Status = br.Status,
                    SelfServiceBorrow = br.SelfServiceBorrow,
                    SelfServiceReturn = br.SelfServiceReturn,
                    BorrowType = br.BorrowType,
                    TotalExtension = br.TotalExtension,
                    TotalRecordItem = br.TotalRecordItem,
                    ProcessedBy = br.ProcessedBy,
                    ProcessedByNavigation = br.ProcessedByNavigation,
                    LibraryCard = br.LibraryCard,
                    Fines = br.Fines,
                    BorrowRequest = br.BorrowRequest != null
                        ? new BorrowRequest()
                        {
                            BorrowRequestId = br.BorrowRequest.BorrowRequestId,
                            LibraryCardId = br.BorrowRequest.LibraryCardId,
                            RequestDate = br.BorrowRequest.RequestDate,
                            ExpirationDate = br.BorrowRequest.ExpirationDate,
                            Status = br.BorrowRequest.Status,
                            Description = br.BorrowRequest.Description,
                            CancelledAt = br.BorrowRequest.CancelledAt,
                            CancellationReason = br.BorrowRequest.CancellationReason,
                            IsReminderSent = br.BorrowRequest.IsReminderSent,
                            TotalRequestItem = br.BorrowRequest.TotalRequestItem
                        }
                        : null
			    });
			
		    if (entities.Any())
            {
                // Convert to dto collection
                var recDtos = _mapper.Map<List<BorrowRecordDto>>(entities);
                
                // Get all conditions 
                var conditionDtos = (await _conditionSvc.GetAllAsync()).Data as List<LibraryItemConditionDto>;
                
                // Initialize list of LibraryCardHolderBorrowRecordDto
                var cardHolderBorrowRecordDto = new List<LibraryCardHolderBorrowRecordDto>();
                // Iterate each borrow record
                foreach (var borrowRec in recDtos)
                {
                    foreach (var borrowDetail in borrowRec.BorrowRecordDetails)
                    {
                        var imageList = new List<string?>();
                        var imagePublicIds = borrowDetail.ImagePublicIds?.Split(",").ToList();
                        if (imagePublicIds != null && imagePublicIds.Any())
                        {
                            foreach (var publicId in imagePublicIds)
                            {
                                // Try to get complete image URL from public ID
                                var url = (await _cloudSvc.BuildMediaUrlAsync(publicId, FileType.Image)).Data as string;
                                imageList.Add(url);
                            }
                            
                            borrowDetail.ImagePublicIds = String.Join(",", imageList);
                        }
                    }
                    
                    
                    // Add borrow record to cardholder borrow record
                    cardHolderBorrowRecordDto.Add(borrowRec.ToCardHolderBorrowRecordDto(
                        conditions: conditionDtos));
                }
                
                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<LibraryCardHolderBorrowRecordDto>(
                    sources: cardHolderBorrowRecordDto, 
                    pageIndex: borrowRecSpec.PageIndex,
                    pageSize: borrowRecSpec.PageSize,
                    totalPage: totalPage, 
                    totalActualItem: totalActualBorrowRec);

                // Response with pagination 
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }
            
            // Not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                new List<LibraryCardHolderBorrowRecordDto>());
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
		    // Determine current system lang 
		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
			    LanguageContext.CurrentLanguage);
		    var isEng = lang == SystemLanguage.English;
		    
		    // Build spec
		    var baseSpec = new BaseSpecification<BorrowRecord>(br => br.BorrowRecordId == id);
			// Retrieve data with spec 
		    var existingEntity = await _unitOfWork.Repository<BorrowRecord, int>()
			    .GetWithSpecAndSelectorAsync(baseSpec, selector: br => new BorrowRecord()
			    {
				    BorrowRecordId = br.BorrowRecordId,
                    BorrowRequestId = br.BorrowRequestId,
                    LibraryCardId = br.LibraryCardId,
                    BorrowDate = br.BorrowDate,
                    DueDate = br.DueDate,
                    ReturnDate = br.ReturnDate,
                    Status = br.Status,
                    SelfServiceBorrow = br.SelfServiceBorrow,
                    SelfServiceReturn = br.SelfServiceReturn,
                    TotalExtension = br.TotalExtension,
                    TotalRecordItem = br.TotalRecordItem,
                    ProcessedBy = br.ProcessedBy,
                    ProcessedByNavigation = br.ProcessedByNavigation,
                    LibraryCard = br.LibraryCard,
                    BorrowType = br.BorrowType,
                    Fines = br.Fines,
                    BorrowRequest = br.BorrowRequest != null
                        ? new BorrowRequest()
                        {
                            BorrowRequestId = br.BorrowRequest.BorrowRequestId,
                            LibraryCardId = br.BorrowRequest.LibraryCardId,
                            RequestDate = br.BorrowRequest.RequestDate,
                            ExpirationDate = br.BorrowRequest.ExpirationDate,
                            Status = br.BorrowRequest.Status,
                            Description = br.BorrowRequest.Description,
                            CancelledAt = br.BorrowRequest.CancelledAt,
                            CancellationReason = br.BorrowRequest.CancellationReason,
                            IsReminderSent = br.BorrowRequest.IsReminderSent,
                            TotalRequestItem = br.BorrowRequest.TotalRequestItem
                        }
                        : null,
                    BorrowRecordDetails = br.BorrowRecordDetails.Select(brd => new BorrowRecordDetail()
                    {
                        BorrowRecordDetailId = brd.BorrowRecordDetailId,
                        BorrowRecordId = brd.BorrowRecordId,
                        LibraryItemInstanceId = brd.LibraryItemInstanceId,
                        ImagePublicIds = brd.ImagePublicIds,
                        ConditionId = brd.ConditionId,
                        ReturnConditionId = brd.ReturnConditionId,
                        ConditionCheckDate = brd.ConditionCheckDate,
                        Condition = brd.Condition,
                        LibraryItemInstance = new LibraryItemInstance()
                        {
                            LibraryItemInstanceId = brd.LibraryItemInstanceId,
                            LibraryItemId = brd.LibraryItemInstance.LibraryItemId,
                            Barcode = brd.LibraryItemInstance.Barcode,
                            Status = brd.LibraryItemInstance.Status,
                            CreatedAt = brd.LibraryItemInstance.CreatedAt,
                            UpdatedAt = brd.LibraryItemInstance.UpdatedAt,
                            CreatedBy = brd.LibraryItemInstance.CreatedBy,
                            UpdatedBy = brd.LibraryItemInstance.UpdatedBy,
                            IsDeleted = brd.LibraryItemInstance.IsDeleted,
                            LibraryItem = brd.LibraryItemInstance.LibraryItem
                        }
                    }).ToList()
			    });
		    if (existingEntity != null)
		    {
			    // Convert to dto collection
			    var recDto = _mapper.Map<BorrowRecordDto>(existingEntity);
                
			    // Get all conditions 
			    var conditionDtos = (await _conditionSvc.GetAllAsync()).Data as List<LibraryItemConditionDto>;
                
				// Iterate each borrow record detail to retrieve URL 
			    foreach (var borrowDetail in recDto.BorrowRecordDetails)
			    {
				    var imageList = new List<string?>();
				    var imagePublicIds = borrowDetail.ImagePublicIds?.Split(",").ToList();
				    if (imagePublicIds != null && imagePublicIds.Any())
				    {
					    foreach (var publicId in imagePublicIds)
					    {
						    // Try to get complete image URL from public ID
						    var url = (await _cloudSvc.BuildMediaUrlAsync(publicId, FileType.Image)).Data as string;
						    imageList.Add(url);
					    }
                            
					    borrowDetail.ImagePublicIds = String.Join(",", imageList);
				    }
			    }

			    return new ServiceResult(ResultCodeConst.SYS_Success0002,
				    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
				    recDto.ToCardHolderBorrowRecordDto(conditions: conditionDtos));
		    }
		    
		    // Not found {0}
		    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
		    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
			    StringUtils.Format(errMsg, isEng ? "borrow record" : "lịch sử mượn"));
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process get borrow by id");
	    }
    }

    public async Task<IServiceResult> GetCardHolderBorrowRecordByIdAsync(Guid userId, int borrowRecordId)
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
		    var baseSpec = new BaseSpecification<BorrowRecord>(br => 
			    br.LibraryCardId == userDto.LibraryCardId && br.BorrowRecordId == borrowRecordId);
		    // Retrieve data with spec
            var existingEntity = await _unitOfWork.Repository<BorrowRecord, int>()
                .GetWithSpecAndSelectorAsync(baseSpec, selector: br => new BorrowRecord()
                {
                    BorrowRecordId = br.BorrowRecordId,
                    BorrowRequestId = br.BorrowRequestId,
                    LibraryCardId = br.LibraryCardId,
                    BorrowDate = br.BorrowDate,
                    DueDate = br.DueDate,
                    ReturnDate = br.ReturnDate,
                    Status = br.Status,
                    SelfServiceBorrow = br.SelfServiceBorrow,
                    SelfServiceReturn = br.SelfServiceReturn,
                    BorrowType = br.BorrowType,
                    TotalExtension = br.TotalExtension,
                    TotalRecordItem = br.TotalRecordItem,
                    ProcessedBy = br.ProcessedBy,
                    ProcessedByNavigation = br.ProcessedByNavigation,
                    LibraryCard = br.LibraryCard,
                    Fines = br.Fines,
                    BorrowRequest = br.BorrowRequest != null
                        ? new BorrowRequest()
                        {
                            BorrowRequestId = br.BorrowRequest.BorrowRequestId,
                            LibraryCardId = br.BorrowRequest.LibraryCardId,
                            RequestDate = br.BorrowRequest.RequestDate,
                            ExpirationDate = br.BorrowRequest.ExpirationDate,
                            Status = br.BorrowRequest.Status,
                            Description = br.BorrowRequest.Description,
                            CancelledAt = br.BorrowRequest.CancelledAt,
                            CancellationReason = br.BorrowRequest.CancellationReason,
                            IsReminderSent = br.BorrowRequest.IsReminderSent,
                            TotalRequestItem = br.BorrowRequest.TotalRequestItem
                        }
                        : null,
                    BorrowRecordDetails = br.BorrowRecordDetails.Select(brd => new BorrowRecordDetail()
                    {
                        BorrowRecordDetailId = brd.BorrowRecordDetailId,
                        BorrowRecordId = brd.BorrowRecordId,
                        LibraryItemInstanceId = brd.LibraryItemInstanceId,
                        ImagePublicIds = brd.ImagePublicIds,
                        ConditionId = brd.ConditionId,
                        ReturnConditionId = brd.ReturnConditionId,
                        ConditionCheckDate = brd.ConditionCheckDate,
                        Condition = brd.Condition,
                        LibraryItemInstance = new LibraryItemInstance()
                        {
                            LibraryItemInstanceId = brd.LibraryItemInstanceId,
                            LibraryItemId = brd.LibraryItemInstance.LibraryItemId,
                            Barcode = brd.LibraryItemInstance.Barcode,
                            Status = brd.LibraryItemInstance.Status,
                            CreatedAt = brd.LibraryItemInstance.CreatedAt,
                            UpdatedAt = brd.LibraryItemInstance.UpdatedAt,
                            CreatedBy = brd.LibraryItemInstance.CreatedBy,
                            UpdatedBy = brd.LibraryItemInstance.UpdatedBy,
                            IsDeleted = brd.LibraryItemInstance.IsDeleted,
                            LibraryItem = brd.LibraryItemInstance.LibraryItem
                        }
                    }).ToList()
                });
            if (existingEntity != null)
            {
	            // Get all conditions 
	            var conditionDtos = (await _conditionSvc.GetAllAsync()).Data as List<LibraryItemConditionDto>;
	            // Convert to dto
	            var borrowRecDto = _mapper.Map<BorrowRecordDto>(existingEntity);
	            
	            // Iterate each borrow record
	            foreach (var borrowDetail in borrowRecDto.BorrowRecordDetails)
	            {
		            var imageList = new List<string?>();
		            var imagePublicIds = borrowDetail.ImagePublicIds?.Split(",").ToList();
		            if (imagePublicIds != null && imagePublicIds.Any())
		            {
			            foreach (var publicId in imagePublicIds)
			            {
				            // Try to get complete image URL from public ID
				            var url = (await _cloudSvc.BuildMediaUrlAsync(publicId, FileType.Image)).Data as string;
				            imageList.Add(url);
			            }
                            
			            borrowDetail.ImagePublicIds = String.Join(",", imageList);
		            }
	            }
	           
	            // Get data successfully
	            return new ServiceResult(ResultCodeConst.SYS_Success0002,
		            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
		            borrowRecDto.ToCardHolderBorrowRecordDto(conditions: conditionDtos));
            }
            
            // Data not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
	            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process get library card holder's borrow record by id");
	    }
    }
    
    public async Task<IServiceResult> GetAllCardHolderBorrowRecordByUserIdAsync(Guid userId, int pageIndex, int pageSize)
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
            var userDto = (await _userSvc.GetWithSpecAsync(userBaseSpec)).Data as UserDto;
            if (userDto == null) // Not found user
            {
                // Data not found or empty
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                    new List<BorrowRequestDto>());
            }
            
            // Build spec
            var baseSpec = new BaseSpecification<BorrowRecord>(br => br.LibraryCardId == userDto.LibraryCardId);   
            
            // Add default order by
            baseSpec.AddOrderByDescending(br => br.BorrowDate);
            
            // Count total borrow request
            var totalRecWithSpec = await _unitOfWork.Repository<BorrowRecord, int>().CountAsync(baseSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalRecWithSpec / pageSize);

            // Set pagination to specification after count total borrow record
            if (pageIndex > totalPage
                || pageIndex < 1) // Exceed total page or page index smaller than 1
            {
                pageIndex = 1; // Set default to first page
            }

            // Apply pagination
            baseSpec.ApplyPaging(skip: pageSize * (pageIndex - 1), take: pageSize);
            
            // Retrieve data with spec
            var entities = await _unitOfWork.Repository<BorrowRecord, int>()
                .GetAllWithSpecAndSelectorAsync(baseSpec, selector: br => new BorrowRecord()
                {
                    BorrowRecordId = br.BorrowRecordId,
                    BorrowRequestId = br.BorrowRequestId,
                    LibraryCardId = br.LibraryCardId,
                    BorrowDate = br.BorrowDate,
                    DueDate = br.DueDate,
                    ReturnDate = br.ReturnDate,
                    Status = br.Status,
                    SelfServiceBorrow = br.SelfServiceBorrow,
                    SelfServiceReturn = br.SelfServiceReturn,
                    BorrowType = br.BorrowType,
                    TotalExtension = br.TotalExtension,
                    TotalRecordItem = br.TotalRecordItem,
                    ProcessedBy = br.ProcessedBy,
                    ProcessedByNavigation = br.ProcessedByNavigation,
                    LibraryCard = br.LibraryCard,
                    Fines = br.Fines,
                    BorrowRequest = br.BorrowRequest != null
                        ? new BorrowRequest()
                        {
                            BorrowRequestId = br.BorrowRequest.BorrowRequestId,
                            LibraryCardId = br.BorrowRequest.LibraryCardId,
                            RequestDate = br.BorrowRequest.RequestDate,
                            ExpirationDate = br.BorrowRequest.ExpirationDate,
                            Status = br.BorrowRequest.Status,
                            Description = br.BorrowRequest.Description,
                            CancelledAt = br.BorrowRequest.CancelledAt,
                            CancellationReason = br.BorrowRequest.CancellationReason,
                            IsReminderSent = br.BorrowRequest.IsReminderSent,
                            TotalRequestItem = br.BorrowRequest.TotalRequestItem
                        }
                        : null
                });
            if (entities.Any())
            {
                // Convert to dto collection
                var recDtos = _mapper.Map<List<BorrowRecordDto>>(entities);
                
                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<LibraryCardHolderBorrowRecordDto>(
	                recDtos.Select(rec => rec.ToCardHolderBorrowRecordDto(conditions: new())), pageIndex, pageSize, totalPage, totalRecWithSpec);

                // Response with pagination 
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }
            
            // Not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                new List<LibraryCardHolderBorrowRecordDto>());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke while process get all borrow record by user id");
        }
    }
    
    public async Task<IServiceResult> ProcessRequestToBorrowRecordAsync(string processedByEmail, BorrowRecordDto dto)
	{
		try
		{
			// Determine current system lang 
			var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
				LanguageContext.CurrentLanguage);
			var isEng = lang == SystemLanguage.English;
			
			// Retrieve user information
			// Build spec
			var employeeBaseSpec = new BaseSpecification<Employee>(u => Equals(u.Email, processedByEmail));
			// Apply include
			employeeBaseSpec.ApplyInclude(q => q
				.Include(u => u.Role)
			);
			// Retrieve user with spec
			var employeeDto = (await _employeeSvc.GetWithSpecAsync(employeeBaseSpec)).Data as EmployeeDto;
			// Not found or not proceeded by employee
			if (employeeDto == null || employeeDto.Role.RoleType != nameof(RoleType.Employee))
			{
				// Forbid 
				throw new ForbiddenException();
			}
						
			// Retrieve library card information
			var libCard = (await _cardSvc.GetByIdAsync(dto.LibraryCardId)).Data as LibraryCardDto;
			if (libCard == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
					StringUtils.Format(errMsg, isEng ? "library card" : "thẻ thư viện"));
			}

			// Validate library card 
			var validateCardRes = await _cardSvc.CheckCardValidityAsync(dto.LibraryCardId);
			// Return invalid card
			if (validateCardRes.ResultCode != ResultCodeConst.LibraryCard_Success0001) return validateCardRes;

			// Check exist any details 
			if (!dto.BorrowRecordDetails.Any())
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
					totalItem: dto.BorrowRecordDetails.Count,
					libCard: libCard);
				if (validateAmountRes != null) return validateAmountRes;
			}
			
			// Check whether request already handled to borrow record
			var isBorrowReqExist = await _unitOfWork.Repository<BorrowRecord,int>().AnyAsync(br => br.BorrowRequestId == dto.BorrowRequestId);
			if (isBorrowReqExist)
			{
				// Msg: Cannot create borrow record because borrow request has been processed
				return new ServiceResult(ResultCodeConst.Borrow_Warning0011,
					await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0011));
			}
			
			// Retrieve borrow request information
			// Build borrow request spec
			var borrowReqSpec = new BaseSpecification<BorrowRequest>(br => br.BorrowRequestId == dto.BorrowRequestId);
			// Apply include
			borrowReqSpec.ApplyInclude(q => q
				.Include(br => br.BorrowRequestDetails)
			);
			var borrowReqDto = (await _borrowReqSvc.GetWithSpecAsync(borrowReqSpec)).Data as BorrowRequestDto;
			if (borrowReqDto == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
					StringUtils.Format(errMsg, isEng ? "borrow request in request history" : "lịch sử đăng ký mượn"));
			}
			
			// Check for borrow request status
			// Only process for request with created status
			if (borrowReqDto.Status != BorrowRequestStatus.Created) 
			{
				switch (borrowReqDto.Status)
				{
					case BorrowRequestStatus.Borrowed:
						// Msg: Cannot create borrow record because borrow request has been processed
						return new ServiceResult(ResultCodeConst.Borrow_Warning0011,
							await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0011));
					case BorrowRequestStatus.Cancelled:
						// Msg: Cannot create borrow record because borrow request has been cancelled
						return new ServiceResult(ResultCodeConst.Borrow_Warning0012,
							await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0012));
					case BorrowRequestStatus.Expired:
						// Msg: Cannot create borrow record because borrow request has been expired
						return new ServiceResult(ResultCodeConst.Borrow_Warning0013,
							await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0013));
					default:
						// Msg: An error occured, failed to create borrow record
						return new ServiceResult(ResultCodeConst.Borrow_Fail0002,
							await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Fail0002));
				}
			}
			
			// Check whether card match with request
			if (!Equals(borrowReqDto.LibraryCardId, dto.LibraryCardId))
			{
				// Msg: Library card does not match the card registered to borrow
				return new ServiceResult(ResultCodeConst.LibraryCard_Warning0005,
					await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0005));
			}
			
			// Check for total details
			var totalRequested = borrowReqDto.BorrowRequestDetails.Count;
			var totalEntered = dto.BorrowRecordDetails.Count;
			if (totalEntered < totalRequested)
			{
				// Msg: The total number of items entered is not enough compared to the total number registered to borrow
				return new ServiceResult(ResultCodeConst.Borrow_Warning0009,
					await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0009));
			}else if (totalEntered > totalRequested)
			{
				// Msg: The total number of items entered exceeds the total number registered for borrowing
				return new ServiceResult(ResultCodeConst.Borrow_Warning0010,
					await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0010));
			}
			
			// Custom errors
			var customErrs = new Dictionary<string, string[]>();
			// Initialize field to determine the longest borrow days
			var longestBorrowDays = 0;
			// Initialize hashset to check duplicate instance
			var instanceIdHashSet = new HashSet<int>();
			// Initialize hashset to check whether instances are in the same item
			var libraryItemIdHashSet = new HashSet<int>();
			// Extract all existing item in request
			var itemInRequestIds = borrowReqDto.BorrowRequestDetails.Select(bi => bi.LibraryItemId).ToList();
			// Iterate each borrow record details to check for item instance quantity status
			var borrowRecordDetailList = dto.BorrowRecordDetails.ToList();
			for (int i = 0; i <borrowRecordDetailList.Count; ++i)
			{
				var detail = borrowRecordDetailList[i];
				
				// Check duplicates instance in the same item
				if (!instanceIdHashSet.Add(detail.LibraryItemInstanceId))
				{
					// Add error 
					customErrs = DictionaryUtils.AddOrUpdate(customErrs, $"borrowRecordDetails[{i}].libraryItemInstanceId",
						isEng 
							? "Item instance is duplicated" 
							: "Bản sao đã bị trùng");
				}
				
				// Build item instance spec
				var instanceSpec = new BaseSpecification<LibraryItemInstance>(li => 
					li.LibraryItemInstanceId == detail.LibraryItemInstanceId);
				// Apply including condition history
				instanceSpec.ApplyInclude(q => q
					.Include(h => h.LibraryItemConditionHistories)
				);
				// Retrieving item instance with spec
				var itemInstanceDto =
					(await _itemInstanceSvc.Value.GetWithSpecAsync(instanceSpec)).Data as LibraryItemInstanceDto;
				if (itemInstanceDto == null)
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					// Add error
					customErrs = DictionaryUtils.AddOrUpdate(customErrs,
						key: $"borrowRecordDetails[{i}].libraryItemInstanceId",
						msg: StringUtils.Format(errMsg, isEng ? "any item instance match" : "bản sao"));
				}
				else
				{
					// Try adding to hashset to check duplicate instances within the same item
					if (!libraryItemIdHashSet.Add(itemInstanceDto.LibraryItemId))
					{
						// Add error 
						customErrs = DictionaryUtils.AddOrUpdate(customErrs,
							key: $"borrowRecordDetails[{i}].libraryItemInstanceId",
							msg: isEng
								? "Instance is duplicated with another instance as it belongs to the same item"
								: "Bản sao đã trùng với một bản sao khác vì thuộc cùng một tài liệu");
					}
					
					// Assign instance latest condition 
					var latestConditionHis = itemInstanceDto.LibraryItemConditionHistories
						.OrderByDescending(h => h.CreatedAt).FirstOrDefault();
					if (latestConditionHis == null)
					{
						var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
						// Add error
						customErrs = DictionaryUtils.AddOrUpdate(customErrs,
							key: $"borrowRecordDetails[{i}].libraryItemInstanceId",
							msg: StringUtils.Format(errMsg, isEng
								? "condition history of instance, please updated"
								: "lịch sử tình trạng bản sao, vui lòng cập nhật"));
					}
					else
					{
						detail.ConditionId = latestConditionHis.ConditionId;
					}
					
					// Check item instance status
					if (Enum.TryParse(itemInstanceDto.Status, true, out LibraryItemInstanceStatus status))
					{
						switch (status)
						{
							case LibraryItemInstanceStatus.InShelf:
								// Skip, continue to check for other instance
								break;
							case LibraryItemInstanceStatus.OutOfShelf:
								// Announce that item has not in borrowing status yet
								customErrs = DictionaryUtils.AddOrUpdate(customErrs,
									$"borrowRecordDetails[{i}].libraryItemInstanceId",
									isEng
										? "Item instance is in out-of-shelf status, cannot process"
										: "Trạng thái của bản sao đang ở trong kho, không thể xử lí");
								break;
							case LibraryItemInstanceStatus.Borrowed:
								// Announce that item has not in borrowing status yet
								customErrs = DictionaryUtils.AddOrUpdate(customErrs,
									$"borrowRecordDetails[{i}].libraryItemInstanceId",
									isEng
										? "Item instance is in borrowed status"
										: "Trạng thái của bản sao đang được mượn");
								break;
							case LibraryItemInstanceStatus.Reserved:
								// Announce that item has not in borrowing status yet
								customErrs = DictionaryUtils.AddOrUpdate(customErrs,
									$"borrowRecordDetails[{i}].libraryItemInstanceId",
									isEng
										? "Item instance is in reserved status"
										: "Bản sao đang ở trạng thái được đặt trước");
								break;
						}
					}
					else
					{
						// Unknown error
						return new ServiceResult(ResultCodeConst.SYS_Warning0006,
							await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0006));
					}
					
					// Check item instance exist in borrow request items
					if (!itemInRequestIds.Contains(itemInstanceDto.LibraryItemId))
					{
						var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
						// Add error
						customErrs = DictionaryUtils.AddOrUpdate(customErrs, $"borrowRecordDetails[{i}].libraryItemInstanceId",
							StringUtils.Format(errMsg, isEng 
								? "item instance in registered borrow request" 
								: "bản sao tồn tại trong lịch sử đăng ký mượn"));
					}
					
					// Check whether user has already borrowed item of this instance
					var hasAlreadyBorrowedItem = await _unitOfWork.Repository<BorrowRecord, int>()
						.AnyAsync(new BaseSpecification<BorrowRecord>(
							br => br.BorrowRecordDetails.Any(brd =>
									  brd.LibraryItemInstance.LibraryItemId == itemInstanceDto.LibraryItemId) && // belongs to specific item
								  br.Status == BorrowRecordStatus.Borrowing // is borrowing 
						));
					if (hasAlreadyBorrowedItem)
					{
						// Add error
						customErrs = DictionaryUtils.AddOrUpdate(customErrs,
							key: $"borrowRecordDetails[{i}].libraryItemInstanceId",
							// Msg: Instance belongs to borrowing item
							msg: await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0014));
					}
				}
				
				// Retrieve category
				var categoryDto = (await _cateSvc.GetWithSpecAsync(new BaseSpecification<Category>(
					c => c.LibraryItems.Any(li => li.LibraryItemId == itemInstanceDto!.LibraryItemId)))).Data as CategoryDto;
				// Check whether category is not null and its total borrow days greater than current longest borrow days
				if (categoryDto != null && categoryDto.TotalBorrowDays > longestBorrowDays)
				{
					// Assign value
					longestBorrowDays = categoryDto.TotalBorrowDays;
				}
			}
			
			// Check if invoke any errors
			if (customErrs.Any()) throw new UnprocessableEntityException("Invalid data", customErrs);

			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
			
			// Set default values for borrow record
			dto.BorrowDate = currentLocalDateTime; // Current date
			dto.DueDate = currentLocalDateTime.AddDays(longestBorrowDays); // Due date = current date + longest borrow days
			dto.Status = BorrowRecordStatus.Borrowing;
			dto.SelfServiceBorrow = false;
			dto.ProcessedBy = employeeDto.EmployeeId;
			dto.TotalRecordItem = dto.BorrowRecordDetails.Count;
			
			// Process add borrow record
			await _unitOfWork.Repository<BorrowRecord, int>().AddAsync(_mapper.Map<BorrowRecord>(dto));
			// Update borrow request status
			await _borrowReqSvc.UpdateStatusWithoutSaveChangesAsync(borrowReqDto.BorrowRequestId,
				BorrowRequestStatus.Borrowed); // Update to borrowed status
			// Update range library item status
			await _itemInstanceSvc.Value.UpdateRangeStatusAndInventoryWithoutSaveChangesAsync(
				libraryItemInstanceIds: dto.BorrowRecordDetails.Select(x => x.LibraryItemInstanceId).ToList(),
				status: LibraryItemInstanceStatus.Borrowed,
				isProcessBorrowRequest: true);
			
			// Save DB with transaction
			var isSaved = await _unitOfWork.SaveChangesWithTransactionAsync() > 0;
			if (isSaved)
			{
				// Msg: Total {0} item(s) have been added to borrow record successfully
				var msg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Success0003);
				return new ServiceResult(ResultCodeConst.Borrow_Success0003,
					StringUtils.Format(msg, dto.BorrowRecordDetails.Count.ToString()));
			}
			
			// Fail to save
			return new ServiceResult(ResultCodeConst.SYS_Fail0001,
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
		}
		catch (UnprocessableEntityException)
		{
			throw;
		}
		catch (ForbiddenException)
		{
			throw;
		}
		catch (Exception ex)
		{
			_logger.Error(ex.Message);
			throw new Exception("Error invoke when process borrow request to borrow record. Performed by: " + processedByEmail);
		}
	}

	public async Task<IServiceResult> CreateAsync(string processedByEmail, BorrowRecordDto dto)
	{
		try
		{
			// Determine current system lang 
			var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
				LanguageContext.CurrentLanguage);
			var isEng = lang == SystemLanguage.English;

			// Retrieve user information
			// Build spec
			var employeeBaseSpec = new BaseSpecification<Employee>(u => Equals(u.Email, processedByEmail));
			// Apply include
			employeeBaseSpec.ApplyInclude(q => q
				.Include(u => u.Role)
			);
			// Retrieve user with spec
			var employeeDto = (await _employeeSvc.GetWithSpecAsync(employeeBaseSpec)).Data as EmployeeDto;
			// Not found or not proceeded by employee
			if (employeeDto == null || employeeDto.Role.RoleType != nameof(RoleType.Employee))
			{
				// Forbid 
				throw new ForbiddenException();
			}

			// Retrieve library card information
			var libCard = (await _cardSvc.GetByIdAsync(dto.LibraryCardId)).Data as LibraryCardDto;
			if (libCard == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
					StringUtils.Format(errMsg, isEng ? "library card" : "thẻ thư viện"));
			}

			// Validate library card 
			var validateCardRes = await _cardSvc.CheckCardValidityAsync(dto.LibraryCardId);
			// Return invalid card
			if (validateCardRes.ResultCode != ResultCodeConst.LibraryCard_Success0001) return validateCardRes;

			// Check exist any details 
			if (!dto.BorrowRecordDetails.Any())
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
					totalItem: dto.BorrowRecordDetails.Count,
					libCard: libCard);
				if (validateAmountRes != null) return validateAmountRes;
			}
			
			// Custom errors
			var customErrs = new Dictionary<string, string[]>();
			// Initialize field to determine the longest borrow days
			var longestBorrowDays = 0;
			// Initialize hash set to check duplicate instance
			var instanceIdHashSet = new HashSet<int>();
			// Initialize hashset to check whether instances are in the same item
			var libraryItemIdHashSet = new HashSet<int>();
			// Iterate each borrow record details to check for item instance quantity status
			var borrowRecordDetailList = dto.BorrowRecordDetails.ToList();
			for (int i = 0; i < borrowRecordDetailList.Count; ++i)
			{
				var detail = borrowRecordDetailList[i];

				// Check duplicates instance in the same item
				if (!instanceIdHashSet.Add(detail.LibraryItemInstanceId))
				{
					// Add error 
					customErrs = DictionaryUtils.AddOrUpdate(customErrs,
						$"borrowRecordDetails[{i}].libraryItemInstanceId",
						isEng
							? "Item instance is duplicated"
							: "Bản sao đã bị trùng");
				}

				// Build item instance spec
				var instanceSpec = new BaseSpecification<LibraryItemInstance>(li =>
					li.LibraryItemInstanceId == detail.LibraryItemInstanceId);
				// Apply including condition history
				instanceSpec.ApplyInclude(q => q
					.Include(h => h.LibraryItemConditionHistories)
				);
				// Retrieving item instance with spec
				var itemInstanceDto =
					(await _itemInstanceSvc.Value.GetWithSpecAsync(instanceSpec)).Data as LibraryItemInstanceDto;
				if (itemInstanceDto == null)
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					// Add error
					customErrs = DictionaryUtils.AddOrUpdate(customErrs,
						$"borrowRecordDetails[{i}].libraryItemInstanceId",
						StringUtils.Format(errMsg, isEng ? "any item instance match" : "bản sao"));
				}
				else
				{
					// Try adding to hashset to check duplicate instances within the same item
					if (!libraryItemIdHashSet.Add(itemInstanceDto.LibraryItemId))
					{
						// Add error 
						customErrs = DictionaryUtils.AddOrUpdate(customErrs,
							key: $"borrowRecordDetails[{i}].libraryItemInstanceId",
							msg: isEng
								? "Instance is duplicated with another instance as it belongs to the same item"
								: "Bản sao đã trùng với một bản sao khác vì thuộc cùng một tài liệu");
					}
					
					// Assign instance latest condition 
					var latestConditionHis = itemInstanceDto.LibraryItemConditionHistories
						.OrderByDescending(h => h.CreatedAt).FirstOrDefault();
					if (latestConditionHis == null)
					{
						var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
						// Add error
						customErrs = DictionaryUtils.AddOrUpdate(customErrs,
							$"borrowRecordDetails[{i}].libraryItemInstanceId",
							StringUtils.Format(errMsg, isEng
								? "condition history of instance, please updated"
								: "lịch sử tình trạng bản sao, vui lòng cập nhật"));
					}
					else
					{
						detail.ConditionId = latestConditionHis.ConditionId;
					}
					
					// Check item instance status
					if (Enum.TryParse(itemInstanceDto.Status, true, out LibraryItemInstanceStatus status))
					{
						switch (status)
						{
							case LibraryItemInstanceStatus.InShelf:
								// Skip, continue to check for other instance
								break;
							case LibraryItemInstanceStatus.OutOfShelf:
								// Announce that item has not in borrowing status yet
								customErrs = DictionaryUtils.AddOrUpdate(customErrs,
									$"borrowRecordDetails[{i}].libraryItemInstanceId",
									isEng
										? "Item instance is in out-of-shelf status, cannot process"
										: "Trạng thái của bản sao đang ở trong kho, không thể xử lí");
								break;
							case LibraryItemInstanceStatus.Borrowed:
								// Announce that item has not in borrowing status yet
								customErrs = DictionaryUtils.AddOrUpdate(customErrs,
									$"borrowRecordDetails[{i}].libraryItemInstanceId",
									isEng
										? "Item instance is in borrowed status"
										: "Trạng thái của bản sao đang được mượn");
								break;
							case LibraryItemInstanceStatus.Reserved:
								// Announce that item has not in borrowing status yet
								customErrs = DictionaryUtils.AddOrUpdate(customErrs,
									$"borrowRecordDetails[{i}].libraryItemInstanceId",
									isEng
										? "Item instance is in reserved status"
										: "Bản sao đang ở trạng thái được đặt trước");
								break;
						}
					}
					else
					{
						// Unknown error
						return new ServiceResult(ResultCodeConst.SYS_Warning0006,
							await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0006));
					}
					
					// Check whether user has already borrowed item of this instance
					var hasAlreadyBorrowedItem = await _unitOfWork.Repository<BorrowRecord, int>()
						.AnyAsync(new BaseSpecification<BorrowRecord>(
							br => br.BorrowRecordDetails.Any(brd =>
									  brd.LibraryItemInstance.LibraryItemId == itemInstanceDto.LibraryItemId) && // belongs to specific item
								  br.Status == BorrowRecordStatus.Borrowing // is borrowing 
						));
					if (hasAlreadyBorrowedItem)
					{
						// Add error
						customErrs = DictionaryUtils.AddOrUpdate(customErrs,
							key: $"borrowRecordDetails[{i}].libraryItemInstanceId",
							msg: isEng
								? "This instance belongs to borrowing item"
								: "Bản sao tìm thấy trong một tài liệu đang được mượn");
					}
				}

				// Retrieve category
				var categoryDto = (await _cateSvc.GetWithSpecAsync(new BaseSpecification<Category>(
						c => c.LibraryItems.Any(li => li.LibraryItemId == itemInstanceDto!.LibraryItemId))))
					.Data as CategoryDto;
				// Check whether category is not null and its total borrow days greater than current longest borrow days
				if (categoryDto != null && categoryDto.TotalBorrowDays > longestBorrowDays)
				{
					// Assign value
					longestBorrowDays = categoryDto.TotalBorrowDays;
				}
			}

			// Check if invoke any errors
			if (customErrs.Any()) throw new UnprocessableEntityException("Invalid data", customErrs);

			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

			// Set default values for borrow record
			dto.BorrowDate = currentLocalDateTime; // Current date
			dto.Status = BorrowRecordStatus.Borrowing;
			dto.SelfServiceBorrow = false;
			dto.ProcessedBy = employeeDto.EmployeeId;
			dto.TotalRecordItem = dto.BorrowRecordDetails.Count;
			
			// Set due date based on borrow type
			if (dto.BorrowType == BorrowType.TakeHome)
			{
				dto.DueDate =
					currentLocalDateTime.AddDays(longestBorrowDays); // Due date = current date + longest borrow days
			}else if (dto.BorrowType == BorrowType.InLibrary)
			{
				// Default already set in request body
			}
			
			// Process add borrow record
			await _unitOfWork.Repository<BorrowRecord, int>().AddAsync(_mapper.Map<BorrowRecord>(dto));
			// Update range library item status
			await _itemInstanceSvc.Value.UpdateRangeStatusAndInventoryWithoutSaveChangesAsync(
				libraryItemInstanceIds: dto.BorrowRecordDetails.Select(x => x.LibraryItemInstanceId).ToList(),
				status: LibraryItemInstanceStatus.Borrowed,
				isProcessBorrowRequest: false);

			// Save DB with transaction
			var isSaved = await _unitOfWork.SaveChangesWithTransactionAsync() > 0;
			if (isSaved)
			{
				// Msg: Total {0} item(s) have been added to borrow record successfully
				var msg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Success0003);
				return new ServiceResult(ResultCodeConst.Borrow_Success0003,
					StringUtils.Format(msg, dto.BorrowRecordDetails.Count.ToString()));
			}

			// Fail to save
			return new ServiceResult(ResultCodeConst.SYS_Fail0001,
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
		}
		catch (UnprocessableEntityException)
		{
			throw;
		}
		catch (ForbiddenException)
		{
			throw;
		}
		catch (Exception ex)
		{
			_logger.Error(ex.Message);
			throw new Exception("Error invoke when process create borrow record");
		}
	}

	public async Task<IServiceResult> SelfCheckoutAsync(Guid libraryCardId, BorrowRecordDto dto)
	{
		try
		{
			// Determine current system lang 
			var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
				LanguageContext.CurrentLanguage);
			var isEng = lang == SystemLanguage.English;
			
			
			// Retrieve library card information
			var libCard = (await _cardSvc.GetByIdAsync(libraryCardId)).Data as LibraryCardDto;
			if (libCard == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
					StringUtils.Format(errMsg, isEng ? "library card" : "thẻ thư viện"));
			}

			// Validate library card 
			var validateCardRes = await _cardSvc.CheckCardValidityAsync(libraryCardId);
			// Return invalid card
			if (validateCardRes.ResultCode != ResultCodeConst.LibraryCard_Success0001) return validateCardRes;
			
			// Check exist any details 
			if (!dto.BorrowRecordDetails.Any())
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
					totalItem: dto.BorrowRecordDetails.Count,
					libCard: libCard);
				if (validateAmountRes != null) return validateAmountRes;
			}
			
			// Custom errors
			var customErrs = new Dictionary<string, string[]>();
			// Initialize field to determine the longest borrow days
			var longestBorrowDays = 0;
			// Initialize hash set to check duplicate instance
			var instanceIdHashSet = new HashSet<int>();
			// Initialize hashset to check whether instances are in the same item
			var libraryItemIdHashSet = new HashSet<int>();
			// Iterate each borrow record details to check for item instance quantity status
			var borrowRecordDetailList = dto.BorrowRecordDetails.ToList();
			for (int i = 0; i < borrowRecordDetailList.Count; ++i)
			{
				var detail = borrowRecordDetailList[i];

				// Check duplicates instance in the same item
				if (!instanceIdHashSet.Add(detail.LibraryItemInstanceId))
				{
					// Add error 
					customErrs = DictionaryUtils.AddOrUpdate(customErrs,
						$"borrowRecordDetails[{i}].libraryItemInstanceId",
						isEng
							? "Item instance is duplicated"
							: "Bản sao đã bị trùng");
				}

				// Build item instance spec
				var instanceSpec = new BaseSpecification<LibraryItemInstance>(li =>
					li.LibraryItemInstanceId == detail.LibraryItemInstanceId);
				// Apply including condition history
				instanceSpec.ApplyInclude(q => q
					.Include(h => h.LibraryItemConditionHistories)
				);
				// Retrieving item instance with spec
				var itemInstanceDto =
					(await _itemInstanceSvc.Value.GetWithSpecAsync(instanceSpec)).Data as LibraryItemInstanceDto;
				if (itemInstanceDto == null)
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					// Add error
					customErrs = DictionaryUtils.AddOrUpdate(customErrs,
						$"borrowRecordDetails[{i}].libraryItemInstanceId",
						StringUtils.Format(errMsg, isEng ? "any item instance match" : "bản sao"));
				}
				else
				{
					// Try adding to hashset to check duplicate instances within the same item
					if (!libraryItemIdHashSet.Add(itemInstanceDto.LibraryItemId))
					{
						// Add error 
						customErrs = DictionaryUtils.AddOrUpdate(customErrs,
							key: $"borrowRecordDetails[{i}].libraryItemInstanceId",
							msg: isEng
								? "Instance is duplicated with another instance as it belongs to the same item"
								: "Bản sao đã trùng với một bản sao khác vì thuộc cùng một tài liệu");
					}
					
					// Assign instance latest condition 
					var latestConditionHis = itemInstanceDto.LibraryItemConditionHistories
						.OrderByDescending(h => h.CreatedAt).FirstOrDefault();
					if (latestConditionHis == null)
					{
						var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
						// Add error
						customErrs = DictionaryUtils.AddOrUpdate(customErrs,
							$"borrowRecordDetails[{i}].libraryItemInstanceId",
							StringUtils.Format(errMsg, isEng
								? "condition history of instance, please updated"
								: "lịch sử tình trạng bản sao, vui lòng cập nhật"));
					}
					else
					{
						detail.ConditionId = latestConditionHis.ConditionId;
					}
					
					// Check item instance status
					if (Enum.TryParse(itemInstanceDto.Status, true, out LibraryItemInstanceStatus status))
					{
						switch (status)
						{
							case LibraryItemInstanceStatus.InShelf:
								// Skip, continue to check for other instance
								break;
							case LibraryItemInstanceStatus.OutOfShelf:
								// Announce that item has not in borrowing status yet
								customErrs = DictionaryUtils.AddOrUpdate(customErrs,
									$"borrowRecordDetails[{i}].libraryItemInstanceId",
									isEng
										? "Item instance is in out-of-shelf status, cannot process"
										: "Trạng thái của bản sao đang ở trong kho, không thể xử lí");
								break;
							case LibraryItemInstanceStatus.Borrowed:
								// Announce that item has not in borrowing status yet
								customErrs = DictionaryUtils.AddOrUpdate(customErrs,
									$"borrowRecordDetails[{i}].libraryItemInstanceId",
									isEng
										? "Item instance is in borrowed status"
										: "Trạng thái của bản sao đang được mượn");
								break;
							case LibraryItemInstanceStatus.Reserved:
								// Announce that item has not in borrowing status yet
								customErrs = DictionaryUtils.AddOrUpdate(customErrs,
									$"borrowRecordDetails[{i}].libraryItemInstanceId",
									isEng
										? "Item instance is in reserved status"
										: "Bản sao đang ở trạng thái được đặt trước");
								break;
						}
					}
					else
					{
						// Unknown error
						return new ServiceResult(ResultCodeConst.SYS_Warning0006,
							await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0006));
					}
					
					// Check whether user has already borrowed item of this instance
					var hasAlreadyBorrowedItem = await _unitOfWork.Repository<BorrowRecord, int>()
						.AnyAsync(new BaseSpecification<BorrowRecord>(
							br => br.BorrowRecordDetails.Any(brd =>
									  brd.LibraryItemInstance.LibraryItemId == itemInstanceDto.LibraryItemId) && // belongs to specific item
								  br.Status == BorrowRecordStatus.Borrowing // is borrowing 
						));
					if (hasAlreadyBorrowedItem)
					{
						// Add error
						customErrs = DictionaryUtils.AddOrUpdate(customErrs,
							key: $"borrowRecordDetails[{i}].libraryItemInstanceId",
							msg: isEng
								? "This instance belongs to borrowing item"
								: "Bản sao tìm thấy trong một tài liệu đang được mượn");
					}
				}

				// Retrieve category
				var categoryDto = (await _cateSvc.GetWithSpecAsync(new BaseSpecification<Category>(
						c => c.LibraryItems.Any(li => li.LibraryItemId == itemInstanceDto!.LibraryItemId))))
					.Data as CategoryDto;
				// Check whether category is not null and its total borrow days greater than current longest borrow days
				if (categoryDto != null && categoryDto.TotalBorrowDays > longestBorrowDays)
				{
					// Assign value
					longestBorrowDays = categoryDto.TotalBorrowDays;
				}
			}

			// Check if invoke any errors
			if (customErrs.Any()) throw new UnprocessableEntityException("Invalid data", customErrs);

			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
			
			// Set default values for borrow record
			dto.BorrowDate = currentLocalDateTime; // Current date
			dto.Status = BorrowRecordStatus.Borrowing;
			dto.SelfServiceBorrow = true;
			dto.TotalRecordItem = dto.BorrowRecordDetails.Count;
			
			// Set due date based on borrow type
			if (dto.BorrowType == BorrowType.TakeHome)
			{
				dto.DueDate =
					currentLocalDateTime.AddDays(longestBorrowDays); // Due date = current date + longest borrow days
			}else if (dto.BorrowType == BorrowType.InLibrary)
			{
				// Default already set in request body
			}
			
			// Process add borrow record
			await _unitOfWork.Repository<BorrowRecord, int>().AddAsync(_mapper.Map<BorrowRecord>(dto));
			// Update range library item status
			await _itemInstanceSvc.Value.UpdateRangeStatusAndInventoryWithoutSaveChangesAsync(
				libraryItemInstanceIds: dto.BorrowRecordDetails.Select(x => x.LibraryItemInstanceId).ToList(),
				status: LibraryItemInstanceStatus.Borrowed,
				isProcessBorrowRequest: false);

			// Save DB with transaction
			var isSaved = await _unitOfWork.SaveChangesWithTransactionAsync() > 0;
			if (isSaved)
			{
				// Msg: Total {0} item(s) have been added to borrow record successfully
				var msg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Success0003);
				return new ServiceResult(ResultCodeConst.Borrow_Success0003,
					StringUtils.Format(msg, dto.BorrowRecordDetails.Count.ToString()));
			}

			// Fail to save
			return new ServiceResult(ResultCodeConst.SYS_Fail0001,
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
		}
		catch (UnprocessableEntityException)
		{
			throw;
		}
		catch (ForbiddenException)
		{
			throw;
		}
		catch (Exception ex)
		{
			_logger.Error(ex.Message);
			throw new Exception("Error invoke when process handle shelf borrow checkout");
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