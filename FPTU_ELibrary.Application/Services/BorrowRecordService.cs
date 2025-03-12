using CloudinaryDotNet.Core;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Payments;
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
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;
using Serilog;
using StackExchange.Redis;

namespace FPTU_ELibrary.Application.Services;

public class BorrowRecordService : GenericService<BorrowRecord, BorrowRecordDto, int>,
    IBorrowRecordService<BorrowRecordDto>
{
    // Lazy services
    private readonly Lazy<ILibraryItemService<LibraryItemDto>> _libItemSvc;
    private readonly Lazy<ILibraryItemInstanceService<LibraryItemInstanceDto>> _itemInstanceSvc;
    private readonly Lazy<IBorrowRequestService<BorrowRequestDto>> _borrowReqSvc;
    private readonly Lazy<IReservationQueueService<ReservationQueueDto>> _reservationQueueSvc;
    private readonly Lazy<ITransactionService<TransactionDto>> _transactionSvc;
    
    private readonly ICloudinaryService _cloudSvc;
    private readonly IUserService<UserDto> _userSvc;
    private readonly IEmployeeService<EmployeeDto> _employeeSvc;
    private readonly ILibraryCardService<LibraryCardDto> _cardSvc;
    private readonly ICategoryService<CategoryDto> _cateSvc;
    private readonly ILibraryItemConditionService<LibraryItemConditionDto> _conditionSvc;

    private readonly BorrowSettings _borrowSettings;

    public BorrowRecordService(
        // Lazy services
        Lazy<ILibraryItemService<LibraryItemDto>> libItemSvc,
        Lazy<ILibraryItemInstanceService<LibraryItemInstanceDto>> itemInstanceSvc,
        Lazy<IBorrowRequestService<BorrowRequestDto>> borrowReqSvc,
        Lazy<IReservationQueueService<ReservationQueueDto>> reservationQueueSvc,
        Lazy<ITransactionService<TransactionDto>> transactionSvc,
        
        // Normal services
        ICategoryService<CategoryDto> cateSvc,
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
        _libItemSvc = libItemSvc;
        _transactionSvc = transactionSvc;
        _itemInstanceSvc = itemInstanceSvc;
        _reservationQueueSvc = reservationQueueSvc;
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
                    SelfServiceBorrow = br.SelfServiceBorrow,
                    SelfServiceReturn = br.SelfServiceReturn,
                    BorrowType = br.BorrowType,
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
                var getBorrowRecordDto = new List<GetBorrowRecordDto>();
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
                    getBorrowRecordDto.Add(borrowRec.ToGetBorrowRecordDto(conditions: conditionDtos));
                }
                
                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<GetBorrowRecordDto>(
                    sources: getBorrowRecordDto, 
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
                new List<GetBorrowRecordDto>());
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

    public async Task<IServiceResult> GetByIdAsync(int id, string? email = null, Guid? userId = null)
    {
	    try
	    {
		    // Determine current system lang 
		    var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
			    LanguageContext.CurrentLanguage);
		    var isEng = lang == SystemLanguage.English;
		    
		    // Build spec
		    var baseSpec = new BaseSpecification<BorrowRecord>(br => br.BorrowRecordId == id);
		    // Add filter (if any)
		    if (!string.IsNullOrWhiteSpace(email))
		    {
			    baseSpec.AddFilter(br => br.LibraryCard.Users.Any(u => u.Email == email));
		    }
		    if (userId.HasValue && userId != Guid.Empty)
		    {
			    baseSpec.AddFilter(br => br.LibraryCard.Users.Any(u => u.UserId == userId));
		    }
			// Retrieve data with spec 
		    var existingEntity = await _unitOfWork.Repository<BorrowRecord, int>()
			    .GetWithSpecAndSelectorAsync(baseSpec, selector: br => new BorrowRecord()
			    {
				    BorrowRecordId = br.BorrowRecordId,
                    BorrowRequestId = br.BorrowRequestId,
                    LibraryCardId = br.LibraryCardId,
                    BorrowDate = br.BorrowDate,
                    SelfServiceBorrow = br.SelfServiceBorrow,
                    SelfServiceReturn = br.SelfServiceReturn,
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
                            TotalRequestItem = br.BorrowRequest.TotalRequestItem,
                            BorrowRequestDetails = br.BorrowRequest.BorrowRequestDetails.Select(brd => new BorrowRequestDetail()
		                    {
		                        BorrowRequestDetailId = brd.BorrowRequestDetailId,
		                        BorrowRequestId = brd.BorrowRequestId,
		                        LibraryItemId = brd.LibraryItemId,
		                        LibraryItem = new LibraryItem()
		                        {
		                            LibraryItemId = brd.LibraryItem.LibraryItemId,
		                            Title = brd.LibraryItem.Title,
		                            SubTitle = brd.LibraryItem.SubTitle,
		                            Responsibility = brd.LibraryItem.Responsibility,
		                            Edition = brd.LibraryItem.Edition,
		                            EditionNumber = brd.LibraryItem.EditionNumber,
		                            Language = brd.LibraryItem.Language,
		                            OriginLanguage = brd.LibraryItem.OriginLanguage,
		                            Summary = brd.LibraryItem.Summary,
		                            CoverImage = brd.LibraryItem.CoverImage,
		                            PublicationYear = brd.LibraryItem.PublicationYear,
		                            Publisher = brd.LibraryItem.Publisher,
		                            PublicationPlace = brd.LibraryItem.PublicationPlace,
		                            ClassificationNumber = brd.LibraryItem.ClassificationNumber,
		                            CutterNumber = brd.LibraryItem.CutterNumber,
		                            Isbn = brd.LibraryItem.Isbn,
		                            Ean = brd.LibraryItem.Ean,
		                            EstimatedPrice = brd.LibraryItem.EstimatedPrice,
		                            PageCount = brd.LibraryItem.PageCount,
		                            PhysicalDetails = brd.LibraryItem.PhysicalDetails,
		                            Dimensions = brd.LibraryItem.Dimensions,
		                            AccompanyingMaterial = brd.LibraryItem.AccompanyingMaterial,
		                            Genres = brd.LibraryItem.Genres,
		                            GeneralNote = brd.LibraryItem.GeneralNote,
		                            BibliographicalNote = brd.LibraryItem.BibliographicalNote,
		                            TopicalTerms = brd.LibraryItem.TopicalTerms,
		                            AdditionalAuthors = brd.LibraryItem.AdditionalAuthors,
		                            CategoryId = brd.LibraryItem.CategoryId,
		                            ShelfId = brd.LibraryItem.ShelfId,
		                            GroupId = brd.LibraryItem.GroupId,
		                            Status = brd.LibraryItem.Status,
		                            IsDeleted = brd.LibraryItem.IsDeleted,
		                            IsTrained = brd.LibraryItem.IsTrained,
		                            CanBorrow = brd.LibraryItem.CanBorrow,
		                            TrainedAt = brd.LibraryItem.TrainedAt,
		                            CreatedAt = brd.LibraryItem.CreatedAt,
		                            UpdatedAt = brd.LibraryItem.UpdatedAt,
		                            UpdatedBy = brd.LibraryItem.UpdatedBy,
		                            CreatedBy = brd.LibraryItem.CreatedBy,
		                            // References
		                            Category = brd.LibraryItem.Category,
		                            Shelf = brd.LibraryItem.Shelf,
		                            LibraryItemGroup = brd.LibraryItem.LibraryItemGroup,
		                            LibraryItemInventory = brd.LibraryItem.LibraryItemInventory,
		                            LibraryItemReviews = brd.LibraryItem.LibraryItemReviews,
		                            LibraryItemAuthors = brd.LibraryItem.LibraryItemAuthors.Select(ba => new LibraryItemAuthor()
		                            {
		                                LibraryItemAuthorId = ba.LibraryItemAuthorId,
		                                LibraryItemId = ba.LibraryItemId,
		                                AuthorId = ba.AuthorId,
		                                Author = ba.Author
		                            }).ToList()
		                        },
		                    }).ToList()
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
                        DueDate = brd.DueDate,
                        ReturnDate = brd.ReturnDate,
                        Status = brd.Status,
                        ConditionCheckDate = brd.ConditionCheckDate,
                        Condition = brd.Condition,
                        TotalExtension = brd.TotalExtension,
                        IsReminderSent = brd.IsReminderSent,
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
                            LibraryItem = new LibraryItem()
	                        {
	                            LibraryItemId = brd.LibraryItemInstance.LibraryItem.LibraryItemId,
	                            Title = brd.LibraryItemInstance.LibraryItem.Title,
	                            SubTitle = brd.LibraryItemInstance.LibraryItem.SubTitle,
	                            Responsibility = brd.LibraryItemInstance.LibraryItem.Responsibility,
	                            Edition = brd.LibraryItemInstance.LibraryItem.Edition,
	                            EditionNumber = brd.LibraryItemInstance.LibraryItem.EditionNumber,
	                            Language = brd.LibraryItemInstance.LibraryItem.Language,
	                            OriginLanguage = brd.LibraryItemInstance.LibraryItem.OriginLanguage,
	                            Summary = brd.LibraryItemInstance.LibraryItem.Summary,
	                            CoverImage = brd.LibraryItemInstance.LibraryItem.CoverImage,
	                            PublicationYear = brd.LibraryItemInstance.LibraryItem.PublicationYear,
	                            Publisher = brd.LibraryItemInstance.LibraryItem.Publisher,
	                            PublicationPlace = brd.LibraryItemInstance.LibraryItem.PublicationPlace,
	                            ClassificationNumber = brd.LibraryItemInstance.LibraryItem.ClassificationNumber,
	                            CutterNumber = brd.LibraryItemInstance.LibraryItem.CutterNumber,
	                            Isbn = brd.LibraryItemInstance.LibraryItem.Isbn,
	                            Ean = brd.LibraryItemInstance.LibraryItem.Ean,
	                            EstimatedPrice = brd.LibraryItemInstance.LibraryItem.EstimatedPrice,
	                            PageCount = brd.LibraryItemInstance.LibraryItem.PageCount,
	                            PhysicalDetails = brd.LibraryItemInstance.LibraryItem.PhysicalDetails,
	                            Dimensions = brd.LibraryItemInstance.LibraryItem.Dimensions,
	                            AccompanyingMaterial = brd.LibraryItemInstance.LibraryItem.AccompanyingMaterial,
	                            Genres = brd.LibraryItemInstance.LibraryItem.Genres,
	                            GeneralNote = brd.LibraryItemInstance.LibraryItem.GeneralNote,
	                            BibliographicalNote = brd.LibraryItemInstance.LibraryItem.BibliographicalNote,
	                            TopicalTerms = brd.LibraryItemInstance.LibraryItem.TopicalTerms,
	                            AdditionalAuthors = brd.LibraryItemInstance.LibraryItem.AdditionalAuthors,
	                            CategoryId = brd.LibraryItemInstance.LibraryItem.CategoryId,
	                            ShelfId = brd.LibraryItemInstance.LibraryItem.ShelfId,
	                            GroupId = brd.LibraryItemInstance.LibraryItem.GroupId,
	                            Status = brd.LibraryItemInstance.LibraryItem.Status,
	                            IsDeleted = brd.LibraryItemInstance.LibraryItem.IsDeleted,
	                            IsTrained = brd.LibraryItemInstance.LibraryItem.IsTrained,
	                            CanBorrow = brd.LibraryItemInstance.LibraryItem.CanBorrow,
	                            TrainedAt = brd.LibraryItemInstance.LibraryItem.TrainedAt,
	                            CreatedAt = brd.LibraryItemInstance.LibraryItem.CreatedAt,
	                            UpdatedAt = brd.LibraryItemInstance.LibraryItem.UpdatedAt,
	                            UpdatedBy = brd.LibraryItemInstance.LibraryItem.UpdatedBy,
	                            CreatedBy = brd.LibraryItemInstance.LibraryItem.CreatedBy,
	                            // References
	                            Category = brd.LibraryItemInstance.LibraryItem.Category,
	                            Shelf = brd.LibraryItemInstance.LibraryItem.Shelf,
	                            LibraryItemInstances = brd.LibraryItemInstance.LibraryItem.LibraryItemInstances
		                            .Where(lii => lii.LibraryItemInstanceId == brd.LibraryItemInstance.LibraryItemInstanceId).ToList(),
	                            LibraryItemInventory = brd.LibraryItemInstance.LibraryItem.LibraryItemInventory,
	                            LibraryItemReviews = brd.LibraryItemInstance.LibraryItem.LibraryItemReviews,
	                            LibraryItemAuthors = brd.LibraryItemInstance.LibraryItem.LibraryItemAuthors.Select(ba => new LibraryItemAuthor()
	                            {
	                                LibraryItemAuthorId = ba.LibraryItemAuthorId,
	                                LibraryItemId = ba.LibraryItemId,
	                                AuthorId = ba.AuthorId,
	                                Author = ba.Author
	                            }).ToList()
	                        },
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
				    recDto.ToGetBorrowRecordDto(conditions: conditionDtos));
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

    public async Task<IServiceResult> GetAllBorrowingByItemIdAsync(int itemId)
    {
	    try
	    {
			// Check exist library item
			var isItemExist = (await _libItemSvc.Value.AnyAsync(li => li.LibraryItemId == itemId)).Data is true;
			if (!isItemExist)
			{
				// Mark as not found or empty
				return new ServiceResult(ResultCodeConst.SYS_Warning0004,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
					new List<BorrowRecordDto>());
			}
			
			// Build spec
			var baseSpec = new BaseSpecification<BorrowRecord>(br => 
					br.BorrowRecordDetails.Any(brd => 
						brd.LibraryItemInstance.LibraryItemId == itemId && // Contain item instance of requested item
						brd.Status == BorrowRecordStatus.Borrowing && // Is borrowing
						brd.ReturnDate == null && // Not return yet
						brd.ReturnConditionId == null));
			// Apply include
			baseSpec.ApplyInclude(q => q
				.Include(br => br.BorrowRecordDetails)
					.ThenInclude(brd => brd.LibraryItemInstance)
			);
			// Retrieve all borrowing record containing specific item id
			var entities = await _unitOfWork.Repository<BorrowRecord, int>().GetAllWithSpecAsync(baseSpec);
			if (entities.Any())
			{
				// Get data successfully
				return new ServiceResult(ResultCodeConst.SYS_Success0002,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
					_mapper.Map<List<BorrowRecordDto>>(entities));
			}
			
			// Mark as not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
            	await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
            	new List<BorrowRecordDto>());
	    }
	    catch (Exception ex)
	    {
		    _logger.Error(ex.Message);
		    throw new Exception("Error invoke when process get all borrowing record by item id");
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
			
			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
			
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
			var borrowReqDto = (await _borrowReqSvc.Value.GetWithSpecAsync(borrowReqSpec)).Data as BorrowRequestDto;
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
							br => 
								br.LibraryCardId == libCard.LibraryCardId &&
								br.BorrowRecordDetails.Any(brd =>
								      brd.Status == BorrowRecordStatus.Borrowing && // Is borrowing
									  brd.LibraryItemInstance.LibraryItemId == itemInstanceDto.LibraryItemId) // belongs to specific item
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
				
				// Add default borrow status
				detail.Status = BorrowRecordStatus.Borrowing;

				switch (dto.BorrowType)
				{
					// Set due date based on borrow type
					case BorrowType.TakeHome:
						detail.DueDate =
							currentLocalDateTime.AddDays(longestBorrowDays); // Due date = current date + longest borrow days
						break;
					case BorrowType.InLibrary:
						// Default already set in request body
						break;
					default:
						var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001);
						var customMsg = isEng ? "Because borrow type is invalid" : "Loại hình mượn không hợp lệ";
						// Mark as failed to borrow
						return new ServiceResult(ResultCodeConst.SYS_Fail0001,$"{msg}.{customMsg}");
				}
			}
			
			// Check if invoke any errors
			if (customErrs.Any()) throw new UnprocessableEntityException("Invalid data", customErrs);
			
			// Set default values for borrow record
			dto.BorrowDate = currentLocalDateTime; // Current date
			dto.SelfServiceBorrow = false;
			dto.ProcessedBy = employeeDto.EmployeeId;
			dto.TotalRecordItem = dto.BorrowRecordDetails.Count;
			
			// Process add borrow record
			await _unitOfWork.Repository<BorrowRecord, int>().AddAsync(_mapper.Map<BorrowRecord>(dto));
			// Update borrow request status
			await _borrowReqSvc.Value.UpdateStatusWithoutSaveChangesAsync(borrowReqDto.BorrowRequestId,
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

			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
			
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
									  brd.LibraryItemInstance.LibraryItemId == itemInstanceDto.LibraryItemId && 
									  brd.Status == BorrowRecordStatus.Borrowing) && // belongs to specific item
								  br.LibraryCardId == libCard.LibraryCardId // With specific library card
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
				
				// Add default borrow status
				detail.Status = BorrowRecordStatus.Borrowing;

				switch (dto.BorrowType)
				{
					// Set due date based on borrow type
					case BorrowType.TakeHome:
						detail.DueDate =
							currentLocalDateTime.AddDays(longestBorrowDays); // Due date = current date + longest borrow days
						break;
					case BorrowType.InLibrary:
						// Default already set in request body
						break;
					default:
						var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001);
						var customMsg = isEng ? "Because borrow type is invalid" : "Loại hình mượn không hợp lệ";
						// Mark as failed to borrow
                        return new ServiceResult(ResultCodeConst.SYS_Fail0001,$"{msg}.{customMsg}");
				}
			}

			// Check if invoke any errors
			if (customErrs.Any()) throw new UnprocessableEntityException("Invalid data", customErrs);

			// Set default values for borrow record
			dto.BorrowDate = currentLocalDateTime; // Current date
			dto.SelfServiceBorrow = false;
			dto.ProcessedBy = employeeDto.EmployeeId;
			dto.TotalRecordItem = dto.BorrowRecordDetails.Count;
			
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
			
			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
			
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
								      brd.Status == BorrowRecordStatus.Borrowing && // Is borrowing
									  brd.LibraryItemInstance.LibraryItemId == itemInstanceDto.LibraryItemId) // belongs to specific item
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
				
				// Add default borrow status
				detail.Status = BorrowRecordStatus.Borrowing;

				switch (dto.BorrowType)
				{
					// Set due date based on borrow type
					case BorrowType.TakeHome:
						detail.DueDate =
							currentLocalDateTime.AddDays(longestBorrowDays); // Due date = current date + longest borrow days
						break;
					case BorrowType.InLibrary:
						// Default already set in request body
						break;
					default:
						var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001);
						var customMsg = isEng ? "Because borrow type is invalid" : "Loại hình mượn không hợp lệ";
						// Mark as failed to borrow
						return new ServiceResult(ResultCodeConst.SYS_Fail0001,$"{msg}.{customMsg}");
				}
			}

			// Check if invoke any errors
			if (customErrs.Any()) throw new UnprocessableEntityException("Invalid data", customErrs);
			
			// Set default values for borrow record
			dto.BorrowDate = currentLocalDateTime; // Current date
			dto.SelfServiceBorrow = true;
			dto.TotalRecordItem = dto.BorrowRecordDetails.Count;
			
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

	public async Task<IServiceResult> ExtendAsync(string email, int borrowRecordId, List<int> borrowRecordDetailIds)
	{
		try
		{
			// Determine current system lang 
			var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
				LanguageContext.CurrentLanguage);
			var isEng = lang == SystemLanguage.English;

			// Try to check exist and validate card
			var userSpec = new BaseSpecification<User>(u => u.Email == email);
			var userDto = (await _userSvc.GetWithSpecAsync(userSpec)).Data as UserDto;
			if (userDto == null || userDto.LibraryCardId == null) // Not found 
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMsg, isEng
						? "library card to process extend borrow record expiration date"
						: "thẻ thư viện để tiến hành gia hạn lịch sử mượn tài liệu"));
			}

			// Validate card
			var checkCardRes = await _cardSvc.CheckCardValidityAsync(Guid.Parse(userDto.LibraryCardId.ToString()!));
			if (checkCardRes.Data is false) return checkCardRes;

			// Build spec
			var baseSpec = new BaseSpecification<BorrowRecord>(br => br.BorrowRecordId == borrowRecordId);
			// Apply include
			baseSpec.ApplyInclude(q => q.Include(br => br.BorrowRecordDetails));
			// Try to retrieve borrow record by id 
			var borrowRecordEntity = await _unitOfWork.Repository<BorrowRecord, int>().GetWithSpecAsync(baseSpec);
			if (borrowRecordEntity == null)
			{
				// Not found {0}
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMsg, isEng
						? "borrow record to process extend expiration date"
						: "lịch sử mượn để tiến hành gia hạn"));
			}

			// Extract all borrow record details
			var existBorrowRecordDetailIds =
				borrowRecordEntity.BorrowRecordDetails.Select(x => x.BorrowRecordDetailId).ToList();
			// Check sequence equals between two collection
			// Check if any requested borrow record detail not belongs to the same record
			if (existBorrowRecordDetailIds.Count != borrowRecordDetailIds.Count ||
			    // Check whether all elements of list A are part of list B
			    !existBorrowRecordDetailIds.All(borrowRecordDetailIds.Contains))
			{
				// Msg: Failed to extend borrow record as {0}
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Fail0007);
				return new ServiceResult(ResultCodeConst.Borrow_Fail0007,
					StringUtils.Format(errMsg, isEng
						? "some item is not exist in borrow record to process extend expiration date"
						: "tồn tại tại liệu không trong lịch sử mượn để tiến hành gia hạn"));
			}

			// Custom errors
			var customErrs = new Dictionary<string, string[]>();

			// Iterate each borrow record detail to handle extension
			for (int i = 0; i < borrowRecordDetailIds.Count; ++i)
			{
				// Retrieve record detail by id 
				var borrowRecordDetailEntity = borrowRecordEntity.BorrowRecordDetails
					.FirstOrDefault(brd => brd.BorrowRecordDetailId == borrowRecordDetailIds[i]);
				if (borrowRecordDetailEntity == null)
				{
					// Msg: Not found {0}
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					// Add error
					customErrs = DictionaryUtils.AddOrUpdate(customErrs,
						key: $"borrowRecordDetailIds[{i}]",
						msg: StringUtils.Format(errMsg,
							isEng ? "borrow record detail" : "tài liệu trong lịch sử mượn"));
				}
				else
				{
					// Check allow to extend
					var checkAllowRes = await CheckExtensionAsync(borrowRecordDetailEntity.Status);
					if (checkAllowRes.Data is false)
					{
						// Add error
						customErrs = DictionaryUtils.AddOrUpdate(customErrs,
							key: $"borrowRecordDetailIds[{i}]",
							msg: checkAllowRes.Message ?? string.Empty);
					}

					// Check exist any pending reservation by item instance id
					var isExistPendingReserved = (await _reservationQueueSvc.Value.CheckPendingByItemInstanceIdAsync(
						borrowRecordDetailEntity.LibraryItemInstanceId)).Data is true;
					if (isExistPendingReserved && borrowRecordDetailEntity.TotalExtension == 1)
					{
						// Msg: Cannot process extend borrow record expiration as this item has already been reserved.
						// Please return the item by <0> to ensure its continued circulations
						var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0020);
						// Add error
						customErrs = DictionaryUtils.AddOrUpdate(customErrs,
							key: $"borrowRecordDetailIds[{i}]",
							msg: $"'{borrowRecordDetailEntity.DueDate:dd/MM/yyyy}'");
					}
					
					// Current local datetime
					var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
						// Vietnam timezone
						TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
					// Validate date allowing to extend
					var allowExtendDate = borrowRecordDetailEntity.DueDate.Subtract(
						TimeSpan.FromDays(_borrowSettings.AllowToExtendInDays));
					// Not allow to extend when exceed or equals to max borrow extension
					if (borrowRecordDetailEntity.TotalExtension >= _borrowSettings.MaxBorrowExtension)
					{
						// Msg: Failed to extend borrow record as {0}
						var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Fail0007);
						// Add error
						customErrs = DictionaryUtils.AddOrUpdate(customErrs,
							key: $"borrowRecordDetailIds[{i}]",
							msg: StringUtils.Format(errMsg, isEng
								? "reaching the maximum number of extensions"
								: "đã đạt đến số lần gia hạn tối đa"));
					}
					else if (DateTime.Compare(allowExtendDate, currentLocalDateTime) > 0)
					{
						// Msg: Cannot process extend borrow record before date {0}
						var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0021);
						// Add error
						customErrs = DictionaryUtils.AddOrUpdate(customErrs,
							key: $"borrowRecordDetailIds[{i}]",
							msg: StringUtils.Format(errMsg, $"'{allowExtendDate:dd/MM/yyyy}'"));
					}

					// Extend borrow record
					borrowRecordDetailEntity.DueDate =
						borrowRecordDetailEntity.DueDate.AddDays(_borrowSettings.TotalBorrowExtensionInDays);
					borrowRecordDetailEntity.TotalExtension++;
					// Add borrow extension history
					borrowRecordDetailEntity.BorrowDetailExtensionHistories.Add(new ()
					{
						ExtensionDate = currentLocalDateTime,
						NewExpiryDate = borrowRecordDetailEntity.DueDate,
						ExtensionNumber = borrowRecordDetailEntity.TotalExtension
					});
				}
			}

			// Check whether invoke any error
			if (customErrs.Any()) throw new UnprocessableEntityException("Invalid data", customErrs);

			// Process update
			await _unitOfWork.Repository<BorrowRecord, int>().UpdateAsync(borrowRecordEntity);
			// Save DB
			var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
			if (isSaved)
			{
				// Msg: Total {0} Item(s) extended successfully
				var msg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Success0006);
				return new ServiceResult(ResultCodeConst.Borrow_Success0006,
					StringUtils.Format(msg, borrowRecordDetailIds.Count.ToString()), true);
			}

			// Failed to extend 
			return new ServiceResult(ResultCodeConst.Borrow_Fail0006,
				await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Fail0006), false);
		}
		catch (UnprocessableEntityException)
		{
			throw;
		}
		catch (Exception ex)
		{
			_logger.Error(ex.Message);
			throw new Exception("Error invoke when process extend borrow record");
		}
	}

	public async Task<IServiceResult> CalculateBorrowReturnSummaryAsync(string email)
	{
		try
		{
			// Initialize summary fields
			var totalRequested = 0;
			var totalBorrowed = 0;
			var totalReturned = 0;
			var totalReserved = 0;
			var unpaidFees = 0.0m;
			
			// Retrieve user by email
			var userDto = (await _userSvc.GetByEmailAsync(email: email)).Data as UserDto;
			if (userDto == null || userDto.LibraryCardId == null) // Not found user
			{
				return new ServiceResult(ResultCodeConst.SYS_Warning0004,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
					new CalculateBorrowReturnSummary()
					{
						TotalRequested = 0,
						TotalBorrowed = 0,
						TotalReturned = 0,
						TotalReserved = 0,
						UnpaidFees = 0
					});
			}
			
			// Calculate total borrow request
			var reqSpec = new BaseSpecification<BorrowRequest>(br => 
				br.Status == BorrowRequestStatus.Created &&
				br.LibraryCardId == userDto.LibraryCardId
			);
			// Count all borrow request with created status
			var countReqRes = (await _borrowReqSvc.Value.GetAllWithSpecAndSelectorAsync(reqSpec,
				selector: br => br.BorrowRequestDetails.Count)).Data as List<int>;
			// Assign result (if any)
			totalRequested = countReqRes?.Sum() ?? 0;
			
			// Calculate total borrow record
			var recSpec = new BaseSpecification<BorrowRecord>(br => 
				br.BorrowRecordDetails.Any(brd => brd.Status == BorrowRecordStatus.Borrowing) && 
				br.LibraryCardId == userDto.LibraryCardId);
			// Count all borrow record with borrowing status
			var countRecRes = await _unitOfWork.Repository<BorrowRecord, int>().GetAllWithSpecAndSelectorAsync(recSpec,
            				selector: br => br.BorrowRecordDetails
					            .Count(brd => brd.Status == BorrowRecordStatus.Borrowing)) as List<int>;
			totalBorrowed = countRecRes?.Sum() ?? 0;
			
			// Calculate total return
			var returnSpec = new BaseSpecification<BorrowRecord>(br => 
				br.BorrowRecordDetails.Any(brd => brd.Status == BorrowRecordStatus.Returned) && 
				br.LibraryCardId == userDto.LibraryCardId);
			var countReturnRes = await _unitOfWork.Repository<BorrowRecord, int>().GetAllWithSpecAndSelectorAsync(recSpec,
		                    selector: br => br.BorrowRecordDetails
		                        .Count(brd => brd.Status == BorrowRecordStatus.Returned)) as List<int>;
			totalReturned = countReturnRes?.Sum() ?? 0;
			
			// Calculate total reserved
			var reserveSpec = new BaseSpecification<ReservationQueue>(rq =>
				rq.LibraryCardId == userDto.LibraryCardId &&
				(
					rq.QueueStatus == ReservationQueueStatus.Pending ||
					rq.QueueStatus == ReservationQueueStatus.Assigned
				));
			var countReserveRes = await _reservationQueueSvc.Value.CountAsync(reserveSpec);
			totalReserved = int.TryParse(countReserveRes.ToString(), out var validReservedNum) ? validReservedNum : 0;
			
			// Calculate unpaid fees
			var transactionSpec = new BaseSpecification<Transaction>(t =>
				t.UserId == userDto.UserId &&
				t.FineId != null &&
				t.TransactionType == TransactionType.Fine &&
				t.TransactionStatus == TransactionStatus.Pending);
			// Retrieve all unpaid transaction containing fines
			var unpaidTransactions = (await _transactionSvc.Value.GetAllWithSpecAndSelectorAsync(transactionSpec,
				selector: t => t.Amount)).Data as List<decimal>;
			unpaidFees = unpaidTransactions?.Sum() ?? 0;
			
			return new ServiceResult(ResultCodeConst.SYS_Warning0004,
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
				new CalculateBorrowReturnSummary()
				{
					TotalRequested = totalRequested,
					TotalBorrowed = totalBorrowed,
					TotalReturned = totalReturned,
					TotalReserved = totalReserved,
					UnpaidFees = unpaidFees
				});
		}
		catch (Exception ex)
		{
			_logger.Error(ex.Message);
			throw new Exception("Error invoke when process calculate borrow return summary");
		}
	}
	
	private async Task<IServiceResult> CheckExtensionAsync(BorrowRecordStatus status)
	{
		// Determine current system lang 
		var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
			LanguageContext.CurrentLanguage);
		var isEng = lang == SystemLanguage.English;
		
		// Initialize error message
		var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Fail0007);
		var customMsg = string.Empty;
		// Determine current borrow status
		switch (status)
		{
			case BorrowRecordStatus.Borrowing:
				break;
			case BorrowRecordStatus.Returned:
				customMsg = isEng
					? "this borrow record is marked as returned"
					: "lịch sử mượn đang ở trạng thái đã trả tài liệu";
            	break;
			case BorrowRecordStatus.Lost:
				customMsg = isEng
					? "this borrow record is marked as lost"
					: "lịch sử mượn đang ở trạng thái mất tài liệu";
				break;
			case BorrowRecordStatus.Overdue:
				customMsg = isEng
					? "this borrow record is marked as overdue"
					: "Lịch sử mượn đang bị quá hạn trả tài liệu";
				break;
		}

		if (!string.IsNullOrEmpty(customMsg))
		{
			// Not allow to extend
			return new ServiceResult(ResultCodeConst.Borrow_Fail0007,StringUtils.Format(errMsg, customMsg), false);
		}
		
		// Mark as allow to extend
		return new ServiceResult(ResultCodeConst.SYS_Success0002,
			await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), true);
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