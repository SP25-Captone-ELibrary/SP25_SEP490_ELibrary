using System.Globalization;
using CloudinaryDotNet.Actions;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Dtos.Users;
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
    private readonly Lazy<IFineService<FineDto>> _fineSvc;
    private readonly Lazy<IFinePolicyService<FinePolicyDto>> _finePolicySvc;
    private readonly Lazy<ILibraryItemService<LibraryItemDto>> _libItemSvc;
    private readonly Lazy<ILibraryItemInstanceService<LibraryItemInstanceDto>> _itemInstanceSvc;
    private readonly Lazy<IBorrowRequestService<BorrowRequestDto>> _borrowReqSvc;
    private readonly Lazy<IReservationQueueService<ReservationQueueDto>> _reservationQueueSvc;
    private readonly Lazy<ITransactionService<TransactionDto>> _transactionSvc;
    
    
    private readonly IEmailService _emailSvc;
    private readonly ICloudinaryService _cloudSvc;
    private readonly IUserService<UserDto> _userSvc;
    private readonly IEmployeeService<EmployeeDto> _employeeSvc;
    private readonly ILibraryCardService<LibraryCardDto> _cardSvc;
    private readonly ICategoryService<CategoryDto> _cateSvc;
    private readonly ILibraryItemConditionService<LibraryItemConditionDto> _conditionSvc;

    private readonly AppSettings _appSettings;
    private readonly BorrowSettings _borrowSettings;

    public BorrowRecordService(
        // Lazy services
        Lazy<ILibraryItemService<LibraryItemDto>> libItemSvc,
        Lazy<ILibraryItemInstanceService<LibraryItemInstanceDto>> itemInstanceSvc,
        Lazy<IBorrowRequestService<BorrowRequestDto>> borrowReqSvc,
        Lazy<IReservationQueueService<ReservationQueueDto>> reservationQueueSvc,
        Lazy<ITransactionService<TransactionDto>> transactionSvc,
        Lazy<IFineService<FineDto>> fineSvc,
        Lazy<IFinePolicyService<FinePolicyDto>> finePolicySvc,
        
        // Normal services
        ICategoryService<CategoryDto> cateSvc,
        ILibraryCardService<LibraryCardDto> cardSvc,
        ILibraryItemConditionService<LibraryItemConditionDto> conditionSvc,
        IEmployeeService<EmployeeDto> employeeSvc,
        IUserService<UserDto> userSvc,
        IEmailService emailSvc,
        ICloudinaryService cloudSvc,
        IOptionsMonitor<BorrowSettings> monitor,
        IOptionsMonitor<AppSettings> monitor1,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _cateSvc = cateSvc;
        _cardSvc = cardSvc;
        _cloudSvc = cloudSvc;
        _emailSvc = emailSvc;
        _conditionSvc = conditionSvc;
        _userSvc = userSvc;
        _fineSvc = fineSvc;
        _finePolicySvc = finePolicySvc;
        _employeeSvc = employeeSvc;
        _borrowReqSvc = borrowReqSvc;
        _libItemSvc = libItemSvc;
        _transactionSvc = transactionSvc;
        _itemInstanceSvc = itemInstanceSvc;
        _reservationQueueSvc = reservationQueueSvc;

        _appSettings = monitor1.CurrentValue;
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
		                    }).ToList(),
                            ReservationQueues = br.BorrowRequest.ReservationQueues.Select(rq => new ReservationQueue()
		                    {
		                        QueueId = rq.QueueId,
		                        LibraryItemId = rq.LibraryItemId,
		                        LibraryItemInstanceId = rq.LibraryItemInstanceId,
		                        LibraryCardId = rq.LibraryCardId,
		                        QueueStatus = rq.QueueStatus,
		                        BorrowRequestId = rq.BorrowRequestId,
		                        IsReservedAfterRequestFailed = rq.IsReservedAfterRequestFailed,
		                        ExpectedAvailableDateMin = rq.ExpectedAvailableDateMin,
		                        ExpectedAvailableDateMax = rq.ExpectedAvailableDateMax,
		                        ReservationDate = rq.ReservationDate,
		                        ExpiryDate = rq.ExpiryDate,
		                        IsNotified = rq.IsNotified,
		                        CancelledBy = rq.CancelledBy,
		                        CancellationReason = rq.CancellationReason,
		                        LibraryItem = new LibraryItem()
		                        {
		                            LibraryItemId = rq.LibraryItem.LibraryItemId,
		                            Title = rq.LibraryItem.Title,
		                            SubTitle = rq.LibraryItem.SubTitle,
		                            Responsibility = rq.LibraryItem.Responsibility,
		                            Edition = rq.LibraryItem.Edition,
		                            EditionNumber = rq.LibraryItem.EditionNumber,
		                            Language = rq.LibraryItem.Language,
		                            OriginLanguage = rq.LibraryItem.OriginLanguage,
		                            Summary = rq.LibraryItem.Summary,
		                            CoverImage = rq.LibraryItem.CoverImage,
		                            PublicationYear = rq.LibraryItem.PublicationYear,
		                            Publisher = rq.LibraryItem.Publisher,
		                            PublicationPlace = rq.LibraryItem.PublicationPlace,
		                            ClassificationNumber = rq.LibraryItem.ClassificationNumber,
		                            CutterNumber = rq.LibraryItem.CutterNumber,
		                            Isbn = rq.LibraryItem.Isbn,
		                            Ean = rq.LibraryItem.Ean,
		                            EstimatedPrice = rq.LibraryItem.EstimatedPrice,
		                            PageCount = rq.LibraryItem.PageCount,
		                            PhysicalDetails = rq.LibraryItem.PhysicalDetails,
		                            Dimensions = rq.LibraryItem.Dimensions,
		                            AccompanyingMaterial = rq.LibraryItem.AccompanyingMaterial,
		                            Genres = rq.LibraryItem.Genres,
		                            GeneralNote = rq.LibraryItem.GeneralNote,
		                            BibliographicalNote = rq.LibraryItem.BibliographicalNote,
		                            TopicalTerms = rq.LibraryItem.TopicalTerms,
		                            AdditionalAuthors = rq.LibraryItem.AdditionalAuthors,
		                            CategoryId = rq.LibraryItem.CategoryId,
		                            ShelfId = rq.LibraryItem.ShelfId,
		                            GroupId = rq.LibraryItem.GroupId,
		                            Status = rq.LibraryItem.Status,
		                            IsDeleted = rq.LibraryItem.IsDeleted,
		                            IsTrained = rq.LibraryItem.IsTrained,
		                            CanBorrow = rq.LibraryItem.CanBorrow,
		                            TrainedAt = rq.LibraryItem.TrainedAt,
		                            CreatedAt = rq.LibraryItem.CreatedAt,
		                            UpdatedAt = rq.LibraryItem.UpdatedAt,
		                            UpdatedBy = rq.LibraryItem.UpdatedBy,
		                            CreatedBy = rq.LibraryItem.CreatedBy,
		                            // References
		                            Category = rq.LibraryItem.Category,
		                            Shelf = rq.LibraryItem.Shelf,
		                            LibraryItemInventory = rq.LibraryItem.LibraryItemInventory,
		                            LibraryItemReviews = rq.LibraryItem.LibraryItemReviews,
		                            LibraryItemAuthors = rq.LibraryItem.LibraryItemAuthors.Select(ba => new LibraryItemAuthor()
		                            {
		                                LibraryItemAuthorId = ba.LibraryItemAuthorId,
		                                LibraryItemId = ba.LibraryItemId,
		                                AuthorId = ba.AuthorId,
		                                Author = ba.Author
		                            }).ToList()
		                        },
		                        LibraryItemInstance = rq.LibraryItemInstance
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
                        ReturnCondition = brd.ReturnCondition,
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
                        },
                        BorrowDetailExtensionHistories = brd.BorrowDetailExtensionHistories,
                        Fines = brd.Fines
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
    
	public async Task<IServiceResult> GetAllActiveRecordByLibCardIdAsync(Guid libraryCardId)
    {
        try
        {
            // Build specification
            var baseSpec = new BaseSpecification<BorrowRecord>(br =>
	            br.LibraryCardId == libraryCardId && // With specific lib card
				br.BorrowRecordDetails.Any(brd =>
		            brd.Status != BorrowRecordStatus.Returned &&
		            brd.Status != BorrowRecordStatus.Lost &&
		            // Not include return fields
		            brd.ReturnDate == null &&
		            brd.ReturnConditionId == null));
            // Retrieve all with spec and selector
            var entities = (await _unitOfWork.Repository<BorrowRecord, int>()
                .GetAllWithSpecAndSelectorAsync(baseSpec, selector: br => new BorrowRecord()
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
                    BorrowRecordDetails = br.BorrowRecordDetails
	                    .Where(brd => brd.Status != BorrowRecordStatus.Returned && 
	                                  brd.Status != BorrowRecordStatus.Lost && 
	                                  brd.ReturnDate == null &&
	                                  brd.ReturnConditionId == null)
	                    .Select(brd => new BorrowRecordDetail()
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
                        ReturnCondition = brd.ReturnCondition,
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
                })).ToList();
            if (entities.Any())
            {
                // Map to dto
                var dtoList = _mapper.Map<List<BorrowRecordDto>>(entities);
                // Get all conditions 
                var conditionDtos = (await _conditionSvc.GetAllAsync()).Data as List<LibraryItemConditionDto>;
                // Convert to GetBorrowRequestDto
                var bRecDtoList = dtoList.Select(e => e.ToGetBorrowRecordDto(conditions: conditionDtos)).ToList();
                // Msg: Get data successfully
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), bRecDtoList);
            }
            
            // Msg: Data not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                new List<GetBorrowRecordDto>());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get all pending request by lib card id");
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
				throw new ForbiddenException("Not allow to access");
			}
						
			// Retrieve library card information
			var userSpec = new BaseSpecification<User>(u => u.LibraryCardId == dto.LibraryCardId);
			// Apply include
			userSpec.ApplyInclude(q => q.Include(u => u.LibraryCard)!);
			// Retrieve user with spec
			var userDto = (await _userSvc.GetWithSpecAsync(userSpec)).Data as UserDto;
			if (userDto == null || userDto.LibraryCard == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
					StringUtils.Format(errMsg, isEng ? "library card" : "thẻ thư viện"));
			}
			// Assign lib card
			var libCard = userDto.LibraryCard;

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
				var validateAmountRes = await _borrowReqSvc.Value.ValidateBorrowAmountAsync(
					totalItem: dto.BorrowRecordDetails.Count,
					libraryCardId: libCard.LibraryCardId,
					isCallFromRecordSvc: true);
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
					.ThenInclude(brd => brd.LibraryItem)
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
							case LibraryItemInstanceStatus.Lost:
								// Announce that item has not in borrowing status yet
								customErrs = DictionaryUtils.AddOrUpdate(customErrs,
									$"borrowRecordDetails[{i}].libraryItemInstanceId",
									isEng
										? "Item instance is in lost status"
										: "Bản sao đang ở trạng thái bị mất");
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
				// Assign borrow request to borrow record
				dto.BorrowRequest = borrowReqDto;
				// Assign library card for borrow record to handle sending email
				dto.LibraryCard = libCard;
				
				// Try to retrieve data for email sending
				foreach (var brd in dto.BorrowRecordDetails)
				{
					// Include library item instance with library item
					var instanceSpec = new BaseSpecification<LibraryItemInstance>(li =>
						li.LibraryItemInstanceId == brd.LibraryItemInstanceId);
					// Apply include
					instanceSpec.ApplyInclude(q => q.Include(i => i.LibraryItem));
					// Assign
					brd.LibraryItemInstance = (await _itemInstanceSvc.Value.GetWithSpecAsync(instanceSpec)).Data as LibraryItemInstanceDto ?? null!;
					
					// Include item condition
					brd.Condition = (await _conditionSvc.GetByIdAsync(brd.ConditionId)).Data as LibraryItemConditionDto ?? null!;
				}

				// Process send email
				await SendBorrowSuccessEmailAsync(
					email: userDto.Email,
					borrowRec: dto, 
					libName: _appSettings.LibraryName,
					libContact: _appSettings.LibraryContact);
				
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
				throw new ForbiddenException("Not allow to access");
			}

			// Retrieve library card information
			var userSpec = new BaseSpecification<User>(u => u.LibraryCardId == dto.LibraryCardId);
			// Apply include
			userSpec.ApplyInclude(q => q.Include(u => u.LibraryCard)!);
			// Retrieve user with spec
			var userDto = (await _userSvc.GetWithSpecAsync(userSpec)).Data as UserDto;
			if (userDto == null || userDto.LibraryCard == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
					StringUtils.Format(errMsg, isEng ? "library card" : "thẻ thư viện"));
			}
			// Assign lib card
			var libCard = userDto.LibraryCard;

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
			
			// Custom errors
			var customErrs = new Dictionary<string, string[]>();
			// Initialize field to determine the longest borrow days
			var longestBorrowDays = 0;
			// Initialize hash set to check duplicate instance
			var instanceIdHashSet = new HashSet<int>();
			// Initialize hashset to check whether instances are in the same item
			var libraryItemIdHashSet = new HashSet<int>();
			// Initialize fields to store handled item (use to count for total amount and check whether exceeding threshold)
			var handledInstances = new List<int>();
			// Initialize borrow request details to check exist in request items
			var borrowReqInDetailList = new List<BorrowRequestDetailDto>();
			// Initialize assigned reservations to check exist in request items
			var assignedReservationInDetailList = new List<ReservationQueueDto>();
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
							case LibraryItemInstanceStatus.Lost:
								// Announce that item has not in borrowing status yet
                                customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                                	$"borrowRecordDetails[{i}].libraryItemInstanceId",
                                	isEng
                                		? "Item instance is in lost status"
                                		: "Bản sao đang ở trạng thái bị mất");
								break;
						}
					}
					else
					{
						// Unknown error
						return new ServiceResult(ResultCodeConst.SYS_Warning0006,
							await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0006));
					}
					
					// Check whether user has already reserved item of this instance
					var reserveSpec = new BaseSpecification<ReservationQueue>(r =>
						r.LibraryItemInstanceId == itemInstanceDto.LibraryItemInstanceId && // With specific item instance id
						r.LibraryCardId == libCard.LibraryCardId); // User has reserved this item
					var userReservedItems = (await _reservationQueueSvc.Value
						.GetAllWithSpecAndSelectorAsync(reserveSpec, selector: s => new ReservationQueueDto()
						{
							QueueId = s.QueueId,
							QueueStatus = s.QueueStatus
						})).Data as List<ReservationQueueDto>;
					if (userReservedItems != null)
					{
						// Iterate each reservation to check for each queue status
						foreach(var queue in userReservedItems)
						{
							switch (queue.QueueStatus)
							{
								 case ReservationQueueStatus.Pending:
									 // Not exist this case as reservation has item instance id its status must be updated to assigned 
									 break;
								 case ReservationQueueStatus.Assigned:
									 // Add to assigned queue status to check for missing other assigned items
									 assignedReservationInDetailList.Add(queue);
									 // Add instance to handled list
									 handledInstances.Add(itemInstanceDto.LibraryItemInstanceId);
									 break;
								 case ReservationQueueStatus.Collected:
									 // This status equals to borrowed status of borrow record detail, hence borrowing detail will handled check return or lost

									 // var existNotReturnRecDetail = await _unitOfWork.Repository<BorrowRecordDetail, int>()
										//  .AnyAsync(brd => 
										// 	 brd.LibraryItemInstanceId == itemInstanceDto.LibraryItemInstanceId && // borrow record with specific item instance id
										// 	 brd.Status != BorrowRecordStatus.Returned && // Has not returned yet
										// 	 brd.Status != BorrowRecordStatus.Lost && // Has not marked as lost
										// 	 brd.BorrowRecord.LibraryCardId == libCard.LibraryCardId // With specific lib card
										// );
									 // if (existNotReturnRecDetail) // Exist in borrow record detail in borrowing status
									 // {
										//  // Add error 
										//  customErrs = DictionaryUtils.AddOrUpdate(customErrs,
										// 	 key: $"borrowRecordDetails[{i}].libraryItemInstanceId",
										// 	 msg: isEng 
										// 		 ? "This item has already existed in borrowing record of reader" 
										// 		 : "Tài liệu đã tồn tại trong lịch sử đang mượn của bạn đọc");
									 // }
									 break;
								 case ReservationQueueStatus.Expired or ReservationQueueStatus.Cancelled:
									 // Allow skip, but check other reserved this item later
									 break;
							}
						}
					}
					
					// Check whether exist in any reservation from other users
					var userReservedItemIds = userReservedItems?.Select(r => r.QueueId).ToList();
					var hasAlreadyReservedByOther = (await _reservationQueueSvc.Value.AnyAsync(r => 
						// Exclude user's reservation
						(userReservedItemIds == null || !userReservedItemIds.Contains(r.QueueId)) &&
						// With specific item instance id
						r.LibraryItemInstanceId == itemInstanceDto.LibraryItemInstanceId && 
						// With specific queue status
						r.QueueStatus == ReservationQueueStatus.Assigned)  // Exist reservation with assigned status
					).Data is true;
					if (hasAlreadyReservedByOther) // Exist other user has reserved this instance
					{
						// Add error
                        customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                        	key: $"borrowRecordDetails[{i}].libraryItemInstanceId",
                        	msg: isEng
                        		? "This instance has reserved by other reader. Not allow to borrow this"
                        		: "Bản sao đã được bạn đọc khác đặt trước, không thể mượn");
					}
					
					// Check whether user has already requested item of this instance
					var borrowReqSpec = new BaseSpecification<BorrowRequest>(br =>
						br.LibraryCardId == libCard.LibraryCardId && // User has request this item
						br.BorrowRequestDetails.Any(brd => brd.LibraryItemId == itemInstanceDto.LibraryItemId)); // With specific item id
					var userRequestedItems = (await _borrowReqSvc.Value
						.GetAllWithSpecAndSelectorAsync(borrowReqSpec, selector: s => new BorrowRequestDto()
						{
							BorrowRequestId = s.BorrowRequestId
						})).Data as List<BorrowRequestDto>;
					if (userRequestedItems != null)
					{
						// Iterate each borrow request to check for borrow status
						foreach (var req in userRequestedItems)
						{
							// Switch to check for status
							switch (req.Status)
							{
								case BorrowRequestStatus.Created:
									// Add to borrow request details list to check total with request items
									borrowReqInDetailList.Add(new ()
									{
										BorrowRequestId = req.BorrowRequestId,
										LibraryItemId = itemInstanceDto.LibraryItemId
									});
									// Add instance to handled list
									handledInstances.Add(itemInstanceDto.LibraryItemInstanceId);
									break;
								case BorrowRequestStatus.Borrowed:
									// This status equals to borrowed status of borrow record detail, hence borrowing detail will handled check return or lost
									break;
								case BorrowRequestStatus.Expired or BorrowRequestStatus.Cancelled:
									// Allow skip, but check other requests later
									break;
							}
						}
					}
					
					// Check whether user has already requested item of this instance
					var userRequestedItemIds = userRequestedItems?.Select(q => q.BorrowRequestId).ToList();
					var hasAlreadyRequestByOther = (await _borrowReqSvc.Value.AnyAsync(br =>
							(userRequestedItemIds == null || !userRequestedItemIds.Contains(br.BorrowRequestId)) && // Exclude all user's borrow request
							br.Status == BorrowRequestStatus.Created && // In requesting status
							br.BorrowRequestDetails.Any(brd => // From each borrow request
								brd.LibraryItem.LibraryItemInstances.Any(li => // Retrieve list instance of a single item 
									li.LibraryItemInstanceId == itemInstanceDto.LibraryItemInstanceId)) // Check exist instance id in that list
					)).Data is true;
					if (hasAlreadyRequestByOther)
					{
						// Add error
						customErrs = DictionaryUtils.AddOrUpdate(customErrs,
							key: $"borrowRecordDetails[{i}].libraryItemInstanceId",
							msg: isEng
								? "This instance has requested by other reader"
								: "Bản sao đã được bạn đọc khác yêu cầu mượn");
					}
					
					// Check whether user has already borrowed item of this instance
					var hasAlreadyBorrowedItem = await _unitOfWork.Repository<BorrowRecord, int>()
						.AnyAsync(new BaseSpecification<BorrowRecord>(
							br => br.BorrowRecordDetails.Any(brd =>
									  brd.LibraryItemInstance.LibraryItemId == itemInstanceDto.LibraryItemId && // With specific item
									  brd.Status != BorrowRecordStatus.Returned && // Exclude returned status 
									  brd.Status != BorrowRecordStatus.Lost) && // Exclude lost status
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

				// Determine borrow type
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
			
			// Build spec
			var reqSpec = new BaseSpecification<BorrowRequest>(br => 
				br.Status == BorrowRequestStatus.Created && // In created/pending status
				br.LibraryCardId == libCard.LibraryCardId); // With specific lib card
			// Retrieve all requesting item from DB to check missing any item when process request to record
			if ((await _borrowReqSvc.Value
				    .GetAllWithSpecAndSelectorAsync(reqSpec, selector: s => 
					    s.BorrowRequestDetails.Select(brd => brd.LibraryItemId))
			    ).Data is IEnumerable<IEnumerable<int>> nestedIds) // Only process when exist any requesting item
			{
				IEnumerable<int> allRequestingItemIds = nestedIds.SelectMany(x => x);
				
				// Check whether not exist any requesting items in borrow record details
				if (!borrowReqInDetailList.Any())
				{
					// Msg: Cannot create borrow record as exist {0} pending item requests awaiting processing
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0038);
					return new ServiceResult(ResultCodeConst.Borrow_Warning0038,
						StringUtils.Format(errMsg, allRequestingItemIds.Count().ToString()));
				}
				
				// Extract all request item ids detected while handling validate borrow record
				var extractedRequestItemIds = borrowReqInDetailList.Select(brd => brd.LibraryItemId).ToList();
				// Compare with all items existing in borrow record details
				foreach (var requestingItemId in allRequestingItemIds)
				{
					// Check whether exist in borrow record details requested
					if (!extractedRequestItemIds.Contains(requestingItemId))
					{
						// Retrieve library item by id
						if ((await _libItemSvc.Value.GetByIdAsync(requestingItemId)).Data is LibraryItemDto libItem)
						{
							// Msg: Cannot create borrow record as item {0} has been requested by reader but have not been processed yet								
							var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0037);
							return new ServiceResult(ResultCodeConst.Borrow_Warning0037,
								StringUtils.Format(errMsg, $"'{libItem.Title}'"));
						}
					}
				}
				
				// All requesting items have been processed in requested borrow record details
				// Then update borrow request status without save
				var borrowReqId = borrowReqInDetailList[0].BorrowRequestId;
				await _borrowReqSvc.Value.UpdateStatusWithoutSaveChangesAsync(
					id: borrowReqId,
					status: BorrowRequestStatus.Borrowed);
				// Assign borrow request to borrow record
				dto.BorrowRequestId = borrowReqId;
			}
			
			// Build spec
			var assignedReserveSpec = new BaseSpecification<ReservationQueue>(q => 
				q.QueueStatus == ReservationQueueStatus.Assigned && // In assigned status (item has been assigned to user and waiting to pick up)
				q.LibraryCardId == libCard.LibraryCardId); // With specific lib card
			// Retrieve all reservations with assigned status from DB to check missing any item when process reserve to record
			if ((await _reservationQueueSvc.Value
				    .GetAllWithSpecAndSelectorAsync(assignedReserveSpec, selector: s =>
					    s.QueueId)).Data is List<int> allQueueIds && allQueueIds.Any()) // Only process when exist any reservation
			{
				// Check whether not exist any reservation's instance in borrow record details
                if (!assignedReservationInDetailList.Any())
                {
                	// Msg: Unable to create borrow record as there are still {0} items have been reserved and successfully assigned to user, but have not been yet processed
                	var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0039);
                	return new ServiceResult(ResultCodeConst.Borrow_Warning0039,
                		StringUtils.Format(errMsg, allQueueIds.Count().ToString()));
                }
                
                // Extract all reservation ids detected while handling validate borrow record
                var extractedReservationIds = assignedReservationInDetailList.Select(brd => brd.QueueId).ToList();
                // Compare with all items existing in borrow record details
                foreach (var queueId in allQueueIds)
                {
	                var reserveSpec = new BaseSpecification<ReservationQueue>(q => q.QueueId == queueId);
	                // Apply including lib item
	                reserveSpec.ApplyInclude(q => q.Include(r => r.LibraryItem));
	                // Retrieve reservation with spec
	                var reservationDto = (await _reservationQueueSvc.Value.GetWithSpecAsync(reserveSpec)).Data as ReservationQueueDto;

	                // Check whether exist in borrow record details requested
	                if (!extractedReservationIds.Contains(queueId))
	                {
		                if (reservationDto != null)
		                {
			                // Msg: Cannot create borrow record as item {0} has been requested by reader but have not been processed yet								
			                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0037);
			                return new ServiceResult(ResultCodeConst.Borrow_Warning0037,
				                StringUtils.Format(errMsg, $"'{reservationDto.LibraryItem.Title}'"));
		                }
	                }
	                else if(extractedReservationIds.Contains(queueId) && reservationDto != null &&
	                        int.TryParse(reservationDto.LibraryItemInstanceId.ToString() ?? "0", out var validInstanceId))
	                {
		                // Update reservation queue status to collected and assign collected date
		                var updateRes = await _reservationQueueSvc.Value.UpdateReservationToCollectedWithoutSaveChangesAsync(
			                id: queueId,
			                libraryItemInstanceId: validInstanceId);
		                if (updateRes.Data is false) return updateRes;
	                }
	                else
	                {
		                // Unknown error
		                return new ServiceResult(ResultCodeConst.SYS_Warning0006,
			                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0006));
	                }
                }
			}
			
			// Check if invoke any errors
			if (customErrs.Any()) throw new UnprocessableEntityException("Invalid data", customErrs);

			// Validate borrow amount (eliminate all handled item due to exist in borrow request or reservation)
			var totalItemAfterHandled = dto.BorrowRecordDetails.Count - handledInstances.Count;
			var validateAmountRes = await _borrowReqSvc.Value.ValidateBorrowAmountAsync(
				totalItem: totalItemAfterHandled,
				libraryCardId: libCard.LibraryCardId,
				isCallFromRecordSvc: true);
			if (validateAmountRes != null) return validateAmountRes;
			
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
				// Assign library card for borrow record to handle sending email
				dto.LibraryCard = libCard;
				// Try to retrieve data for email sending
                foreach (var brd in dto.BorrowRecordDetails)
                {
                	// Include library item instance with library item
                	var instanceSpec = new BaseSpecification<LibraryItemInstance>(li =>
                		li.LibraryItemInstanceId == brd.LibraryItemInstanceId);
                	// Apply include
                	instanceSpec.ApplyInclude(q => q.Include(i => i.LibraryItem));
                	// Assign
                	brd.LibraryItemInstance = (await _itemInstanceSvc.Value.GetWithSpecAsync(instanceSpec)).Data as LibraryItemInstanceDto ?? null!;
                	
                	// Assign item condition
                	brd.Condition = (await _conditionSvc.GetByIdAsync(brd.ConditionId)).Data as LibraryItemConditionDto ?? null!;
                }
				
				// Process send email
				await SendBorrowSuccessEmailAsync(
					email: userDto.Email, 
					borrowRec: dto,
					libName: _appSettings.LibraryName,
					libContact: _appSettings.LibraryContact);
				
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
				var validateAmountRes = await _borrowReqSvc.Value.ValidateBorrowAmountAsync(
					totalItem: dto.BorrowRecordDetails.Count,
					libraryCardId: libCard.LibraryCardId);
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
							case LibraryItemInstanceStatus.Lost:
								// Announce that item has not in borrowing status yet
								customErrs = DictionaryUtils.AddOrUpdate(customErrs,
									$"borrowRecordDetails[{i}].libraryItemInstanceId",
									isEng
										? "Item instance is in lost status"
										: "Bản sao đang ở trạng thái bị mất");
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
			if (!borrowRecordDetailIds.All(existBorrowRecordDetailIds.Contains)) // Check whether all elements of list A are part of list B
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
				// TODO: Implement send email after extend borrow record success
				
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

	public async Task<IServiceResult> ProcessReturnAsync(
		string processedReturnByEmail, 
		int id, Guid libraryCardId, 
		BorrowRecordDto recordWithReturnItems,
		BorrowRecordDto recordWithLostItems,
		bool isConfirmMissing = false)
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
			
			// Check exist process return email
			var employeeDto = (await _employeeSvc.GetByEmailAsync(email: processedReturnByEmail)).Data as EmployeeDto;
			// Forbid to access if not found any employee match
			if (employeeDto == null) throw new ForbiddenException("Not allow to access");
			
			// Retrieve user information
			var userDto = (await _userSvc.GetWithSpecAsync(new BaseSpecification<User>(
				u => u.LibraryCardId == libraryCardId))).Data as UserDto;
			if (userDto == null)
			{
				// Msg: Not found {0}
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMsg, isEng ? "user" : "bạn đọc"));
			}
			
			// Build specification
			var baseSpec = new BaseSpecification<BorrowRecord>(br => 
				br.BorrowRecordId == id &&
				br.LibraryCardId == libraryCardId);
			// Apply include
			baseSpec.ApplyInclude(q => q
				.Include(br => br.BorrowRecordDetails)
					.ThenInclude(brd => brd.LibraryItemInstance)	
						.ThenInclude(li => li.LibraryItem)
				.Include(br => br.BorrowRecordDetails)
					.ThenInclude(brd => brd.BorrowDetailExtensionHistories)
				.Include(br => br.BorrowRecordDetails)
					.ThenInclude(brd => brd.Condition)
				.Include(br => br.LibraryCard)
			);
			// Retrieve borrow record by id 
			var existingEntity = await _unitOfWork.Repository<BorrowRecord, int>().GetWithSpecAsync(baseSpec);
			if (existingEntity == null)
			{
				// Msg: Not found {0}
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMsg, isEng ? "borrow record" : "lịch sử mượn"));
			}
			
			// Initialize custom errors
			var customErrs = new Dictionary<string, string[]>();
			// Initialize err key
			var errKey = StringUtils.ToCamelCase(nameof(BorrowRecord.BorrowRecordDetails));
			
			// Extract all lost record detail ids
			var lostRecordDetailIds = recordWithLostItems.BorrowRecordDetails.Select(brd => brd.BorrowRecordDetailId).ToList();
			
			// Try to retrieve all item need to be returned today or being overdue but not exist in return list
			var needToReturnRecDetails = existingEntity.BorrowRecordDetails
				.Where(brd => (brd.DueDate.Date == currentLocalDateTime.Date || brd.Status == BorrowRecordStatus.Overdue) &&
				              recordWithReturnItems.BorrowRecordDetails.All(q => // Exclude all not exist in return list
					              q.LibraryItemInstanceId != brd.LibraryItemInstanceId) && 
				              !lostRecordDetailIds.Contains(brd.BorrowRecordDetailId)) // Exclude all not exist in lost list
				.ToList();
			if (needToReturnRecDetails.Any() && !isConfirmMissing)
			{
				// Msg: Confirm all item need to be returned in day but not exist in return list
				return new ServiceResult(ResultCodeConst.Borrow_Warning0030,
					await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0030),
					new ConfirmMissingReturnItemDto()
					{
						BorrowRecordDetails = _mapper.Map<List<BorrowRecordDetailDto>>(needToReturnRecDetails)
					});
			}
			
			// Process handle items marked as lost
			// Check if any record detail marked as lost but exist in return list
			if (recordWithReturnItems.BorrowRecordDetails.Any(
				    brd => lostRecordDetailIds.Contains(brd.BorrowRecordDetailId)))
			{
				// Msg: Items marked as lost must not exist in return list
				return new ServiceResult(ResultCodeConst.Borrow_Warning0029,
					await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0029));
			}
			// Initialize lost item instance ids
			var lostItemInstanceIds = new List<int>();
			for (int i = 0; i < lostRecordDetailIds.Count; ++i)
			{
				// Try record detail mark as lost
				var recDetail = existingEntity.BorrowRecordDetails
					.FirstOrDefault(brd => brd.BorrowRecordDetailId == lostRecordDetailIds[i]);
				if (recDetail == null)
				{
					// Msg: Not found {0}
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					// Add error
					customErrs = DictionaryUtils.AddOrUpdate(customErrs,
						key: $"lostBorrowRecordDetails[{i}].BorrowRecordDetailId",
						msg: StringUtils.Format(errMsg, isEng 
							? "item marked as lost in borrow record details" 
							: "tài liệu được đánh dấu bị mất trong danh sách tài liệu mượn"));
				}
				else if((recDetail.Status != BorrowRecordStatus.Borrowing && recDetail.Status != BorrowRecordStatus.Overdue) ||
				        recDetail.LibraryItemInstance.Status != nameof(LibraryItemInstanceStatus.Borrowed))
				{
					// Add error
					customErrs = DictionaryUtils.AddOrUpdate(customErrs,
						key: $"lostBorrowRecordDetails[{i}].BorrowRecordDetailId",
						// Msg: Cannot change item status to lost as item isn't in borrowing status
						msg: await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0028));
				}
				else
				{
					// Check existing fine for lost item
					var lostFines = recordWithLostItems
						.BorrowRecordDetails.ElementAt(i)
						.Fines.ToList();
					if (lostFines == null! || !lostFines.Any())
					{
						// Add error
                        customErrs = DictionaryUtils.AddOrUpdate(customErrs,
	                        key: $"lostBorrowRecordDetails[{i}].BorrowRecordDetailId",
                            // Msg: Please add fines for item marked as lost
                            msg: await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0031));
					}
					else
					{
						// Extract all fine policy ids
						var finePolicyIds = lostFines.Select(f => f.FinePolicyId).ToList();
						// Build spec
                        var fineSpec = new BaseSpecification<FinePolicy>(f => 
	                        finePolicyIds.Contains(f.FinePolicyId) &&
                            f.ConditionType == FinePolicyConditionType.Lost);
                        // Count 
                        var countRes = (await _finePolicySvc.Value.CountAsync(fineSpec)).Data;
                        if (countRes == null)
                        {
                            // Add error
                            customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                                key: $"lostBorrowRecordDetails[{i}].BorrowRecordDetailId",
                                // Msg: Please add fines for item marked as lost
                                msg: await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0031));
                        }
                        // Try parse count result to integer
                        else if (int.TryParse(countRes.ToString(), out var validCount)) 
                        {
                            if (validCount == 0) // Not found any overdue fine policy 
                            {
                                // Add error
                                customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                                    key: $"lostBorrowRecordDetails[{i}].BorrowRecordDetailId",
                                    // Msg: Please add fines for item marked as lost
                                    msg: await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0031));
                            }
                            else if (validCount > 1 || 
                                     finePolicyIds.Count != finePolicyIds.Distinct().Count()) // Duplicate fine policy
                            {
	                            // Add error
	                            customErrs = DictionaryUtils.AddOrUpdate(customErrs,
		                            key: $"lostBorrowRecordDetails[{i}].BorrowRecordDetailId",
		                            // Msg: Exist duplicate or inappropriate fines for return item marked as lost
		                            msg: await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0032));
                            }
                            else
                            {
	                            // Process add fine for each return detail
                                foreach (var fine in lostFines)
                                {
	                                // Retrieve item's estimated price
	                                var estimatedPrice = recDetail.LibraryItemInstance.LibraryItem.EstimatedPrice;
	                                if (estimatedPrice == null || estimatedPrice == 0)
	                                {
		                                // Add error
		                                customErrs = DictionaryUtils.AddOrUpdate(customErrs,
			                                key: $"lostBorrowRecordDetails[{i}].BorrowRecordDetailId",
			                                // Msg: Estimated price of item not found. Please update it to process lost return
			                                msg: await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0033));
	                                }
	                                
                                    // Retrieve fine policy by id
                                    if ((await _finePolicySvc.Value.GetWithSpecAsync(
	                                        new BaseSpecification<FinePolicy>(f => f.FinePolicyId == fine.FinePolicyId))
                                        ).Data is FinePolicyDto fineDto)
                                    {
                                        recDetail.Fines.Add(new()
                                        {
                                            FinePolicyId = fineDto.FinePolicyId,
                                            FineAmount = estimatedPrice ?? decimal.Zero,
                                            Status = FineStatus.Pending,
                                            CreatedAt = currentLocalDateTime,
                                            ExpiryAt = currentLocalDateTime.AddDays(_borrowSettings.FineExpirationInDays),
                                            CreatedBy = employeeDto.EmployeeId
                                        });
                                    }
                                }
                            }
                        }
					}
					
					// Update borrow record status
					recDetail.Status = BorrowRecordStatus.Lost;
					// Add item instance id
					lostItemInstanceIds.Add(recDetail.LibraryItemInstanceId);
				}
			}
			
			// Process return item
			// Convert request details to list
			var borrowRecDetailList = recordWithReturnItems.BorrowRecordDetails.ToList();
			// Iterate each borrow record detail
			for (int i = 0; i < borrowRecDetailList.Count; ++i)
			{
				var brDetail = borrowRecDetailList[i];
				
				// Check exist library item instance
				var itemInstanceSpec = new BaseSpecification<LibraryItemInstance>(li =>
					li.LibraryItemInstanceId == brDetail.LibraryItemInstanceId);
				var itemInstanceDto = (await _itemInstanceSvc.Value.GetWithSpecAsync(itemInstanceSpec)).Data as LibraryItemInstanceDto;
				if (itemInstanceDto == null)
				{
					// Msg: Not found {0}
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					// Add error
					customErrs = DictionaryUtils.AddOrUpdate(customErrs,
						key: errKey,
						msg: StringUtils.Format(errMsg, isEng ? "item" : "tài liệu"));
				}
				else
				{
					// Try to retrieve item instance in borrow record details
					var existingBrDetail = existingEntity.BorrowRecordDetails.FirstOrDefault(brd => 
                    					brd.LibraryItemInstanceId == itemInstanceDto.LibraryItemInstanceId);
	                // Not found any record detail match request instance id
	                if (existingBrDetail == null)
	                {
	                    // Msg: Item {0} doesn't exist in borrow record
	                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0023);
	                    // Add error
	                    customErrs = DictionaryUtils.AddOrUpdate(customErrs,
	                        key: errKey,
	                        msg: StringUtils.Format(errMsg, $"'{itemInstanceDto.LibraryItem.Title}'"));
	                }
	                else
	                {
						// Msg: Cannot process return for item {0} as {1}
		                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0024);
	                    // Validate record detail status
	                    switch (existingBrDetail.Status)
	                    {
                    		case BorrowRecordStatus.Borrowing:
			                    // Skip to check other status
                    			break;
                    		case BorrowRecordStatus.Returned:
			                    // Add error
			                    customErrs = DictionaryUtils.AddOrUpdate(customErrs,
				                    key: $"{errKey}[{i}].LibraryItemInstanceId",
				                    msg: StringUtils.Format(errMsg, $"'{itemInstanceDto.LibraryItem.Title}'",
					                    isEng 
						                    ? "item has been returned" 
						                    : "tài liệu đã ở trạng thái được trả"));
			                    break;			                    
                    		case BorrowRecordStatus.Overdue:
			                    // Try to check for fines
			                    if (!brDetail.Fines.Any())
			                    {
				                    // Add error
				                    customErrs = DictionaryUtils.AddOrUpdate(customErrs,
					                    key: $"{errKey}[{i}].LibraryItemInstanceId",
					                    // Msg: Please add fines for overdue item
					                    msg: await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0025));
			                    }
			                    else
			                    {
				                    // Extract all fine policy of each record detail
				                    var finePolicyIds = brDetail.Fines.Select(f => f.FinePolicyId).ToList();
				                    // Build spec
				                    var fineSpec = new BaseSpecification<FinePolicy>(f => 
					                    finePolicyIds.Contains(f.FinePolicyId) &&
					                    f.ConditionType == FinePolicyConditionType.OverDue);
				                    // Count 
				                    var countRes = (await _finePolicySvc.Value.CountAsync(fineSpec)).Data;
				                    if (countRes == null)
				                    {
					                    // Add error
					                    customErrs = DictionaryUtils.AddOrUpdate(customErrs,
						                    key: $"{errKey}[{i}].LibraryItemInstanceId",
						                    // Msg: Please add fines for overdue item
						                    msg: await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0025));
				                    }
				                    // Try parse count result to integer
				                    else if (int.TryParse(countRes.ToString(), out var validCount)) 
				                    {
					                    if (validCount == 0) // Not found any overdue fine policy 
					                    {
						                    // Add error
                                            customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                                                key: $"{errKey}[{i}].LibraryItemInstanceId",
                                                // Msg: Please add fines for overdue item
                                                msg: await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0025));
					                    }
										else if (validCount > 1 ||
										         finePolicyIds.Count != finePolicyIds.Distinct().Count()) // Duplicate fine policy
					                    {	
						                    // Add error
						                    customErrs = DictionaryUtils.AddOrUpdate(customErrs,
							                    key: $"{errKey}[{i}].LibraryItemInstanceId",
							                    // Msg: Fine has been duplicated in return item
							                    msg: await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0026));
					                    }
				                    }
			                    }
                    			break;
                    		case BorrowRecordStatus.Lost:
			                    // Add error
			                    customErrs = DictionaryUtils.AddOrUpdate(customErrs,
				                    key: $"{errKey}[{i}].LibraryItemInstanceId",
				                    msg: StringUtils.Format(errMsg, $"'{itemInstanceDto.LibraryItem.Title}'",
					                    isEng 
						                    ? "item has been lost" 
						                    : "tài liệu đã ở trạng thái bị mất"));
                    			break;
	                    }
	                    
	                    // Check exist condition images id
	                    var imagePublicIds = brDetail.ImagePublicIds?.Split(",");
	                    if (imagePublicIds != null && imagePublicIds.Any()) // Only check when exist at least once
	                    {
		                    foreach(var publicId in imagePublicIds)
		                    {
			                    // Process check exist on cloud			
			                    var isImageOnCloud = (await _cloudSvc.IsExistAsync(publicId, FileType.Image)).Data is true;

			                    if (!isImageOnCloud) // Not found image or public id
			                    {
				                    // Add error
				                    customErrs = DictionaryUtils.AddOrUpdate(customErrs,
					                    key: $"{errKey}[{i}].LibraryItemInstanceId",
										// Msg: Not found image resource
					                    msg: await _msgService.GetMessageAsync(ResultCodeConst.Cloud_Warning0001));
			                    }
		                    }
	                    }
	                    
	                    // Check exist return condition
	                    var isReturnConditionExist = (await _conditionSvc.AnyAsync(c => c.ConditionId == brDetail.ReturnConditionId)).Data is true;
	                    if (!isReturnConditionExist)
	                    {
                            // Add error
                            customErrs = DictionaryUtils.AddOrUpdate(customErrs,
	                            key: $"{errKey}[{i}].LibraryItemInstanceId",
								// Msg: Please update item status for return item
	                            msg: StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0027)));
	                    }
	                    
	                    // Try parse library item instance status
	                    Enum.TryParse(typeof(LibraryItemInstanceStatus), existingBrDetail.LibraryItemInstance.Status, true, out var validStatus);
	                    if (validStatus == null)
	                    {
                    		// Mark as unknown error
                    		return new ServiceResult(ResultCodeConst.SYS_Warning0006,
                    			await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0006));
	                    }
	                    
	                    // Check for item instance status
	                    switch (validStatus)
	                    {
                    		case LibraryItemInstanceStatus.Borrowed:
                    			// Skip, continue to check for other status
                    			break;
                    		case LibraryItemInstanceStatus.InShelf:
                    			// Announce that item has not in borrowing status yet
                    			customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                    				key: $"{errKey}[{i}].libraryItemInstanceId",
                    				msg: isEng
                    					? "Item instance is in available status, cannot process return"
                    					: "Trạng thái của bản sao đang ở trạng thái có sẵn, không thể xử lí trả");
                    			break;
                    		case LibraryItemInstanceStatus.OutOfShelf:
                    			// Announce that item has not in borrowing status yet
                    			customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                    				key: $"{errKey}[{i}].libraryItemInstanceId",
                    				msg: isEng
                    					? "Item instance is in out-of-shelf status, cannot process return"
                    					: "Trạng thái của bản sao đang ở trong kho, không thể xử lí trả");
                    			break;
                    		case LibraryItemInstanceStatus.Reserved:
                    			// Announce that item has not in borrowing status yet
                    			customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                    				key: $"{errKey}[{i}].libraryItemInstanceId",
                    				msg: isEng
                    					? "Item instance is in reserved status, cannot process return"
                    					: "Bản sao đang ở trạng thái được đặt trước, không thể xử lí trả");
                    			break;
		                    case LibraryItemInstanceStatus.Lost:
			                    // Announce that item has not in borrowing status yet
                                customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                                	key: $"{errKey}[{i}].libraryItemInstanceId",
                                	msg: isEng
                                		? "Item instance is in lost status"
                                		: "Bản sao đang ở trạng thái bị mất");
			                    break;
	                    }
	                    
	                    // Process add return fields
	                    existingBrDetail.ReturnConditionId = brDetail.ReturnConditionId;
	                    existingBrDetail.Status = BorrowRecordStatus.Returned;
	                    existingBrDetail.ReturnDate = currentLocalDateTime;
	                    existingBrDetail.ConditionCheckDate = currentLocalDateTime;
	                    existingBrDetail.ImagePublicIds = !string.IsNullOrEmpty(brDetail.ImagePublicIds) 
							? brDetail.ImagePublicIds : null;
	                    // Add processed return by
	                    existingBrDetail.ProcessedReturnBy = employeeDto.EmployeeId;
	                    
	                    // Process add fines for return items
	                    foreach (var fine in brDetail.Fines)
	                    {
		                    // Retrieve fine policy by id
		                    if ((await _finePolicySvc.Value.GetWithSpecAsync(
			                        new BaseSpecification<FinePolicy>(f => f.FinePolicyId == fine.FinePolicyId))
		                        ).Data is FinePolicyDto fineDto)
		                    {
			                    // Check for existing fine value
			                    if (fineDto.FixedFineAmount == 0 || fineDto.FineAmountPerDay == 0)
			                    {
				                    // Msg: Not found price value for fine policy {0}
				                    errMsg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0034); 
				                    return new ServiceResult(ResultCodeConst.Borrow_Warning0034,
					                    StringUtils.Format(errMsg, $"'{fineDto.FinePolicyTitle}'"));
			                    }
								
			                    // Determine fine policy condition type
			                    decimal? fineAmount;
								switch (fineDto.ConditionType)
			                    {
				                    case FinePolicyConditionType.Damage or FinePolicyConditionType.OverDue:
					                    // Assign default (fixed fine amount)
					                    fineAmount = fineDto.FixedFineAmount;
					                    break;
				                    default:
					                    // Msg: Fine policy is invalid for returning item
					                    return new ServiceResult(ResultCodeConst.Borrow_Warning0035,
						                    await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0035));
			                    }
			                    
								// Add fine to record detail
			                    existingBrDetail.Fines.Add(new()
                                {
                                    FinePolicyId = fineDto.FinePolicyId,
                                    FineAmount = fineAmount ?? 0,
                                    Status = FineStatus.Pending,
                                    CreatedAt = currentLocalDateTime,
                                    ExpiryAt = currentLocalDateTime.AddDays(_borrowSettings.FineExpirationInDays),
                                    CreatedBy = employeeDto.EmployeeId
                                });
		                    }
	                    }
	                }
				}
			}
			
			// Check whether invoke any errors
			if (customErrs.Any()) throw new UnprocessableEntityException("Invalid data", customErrs);

			if (borrowRecDetailList.Any()) // Exist at least one borrow record detail to update inventory
			{
				// Process update lost items' instance status
                await _itemInstanceSvc.Value.UpdateRangeStatusAndInventoryWithoutSaveChangesAsync(
                	// Extract all return items' instance id
                	libraryItemInstanceIds: borrowRecDetailList.Select(brd => brd.LibraryItemInstanceId).ToList(),
                	// Update to out of shelf status (from Borrowed -> OutOfShelf)
                	status: LibraryItemInstanceStatus.OutOfShelf,
                	// Not update from request
                	isProcessBorrowRequest: false);
			}
			if (lostItemInstanceIds.Any()) // Exist at least one lost item to update inventory
			{
				// Process update return items' instance status
				await _itemInstanceSvc.Value.UpdateRangeStatusAndInventoryWithoutSaveChangesAsync(
					// Extract all lost items' instance id
					libraryItemInstanceIds: lostItemInstanceIds,
					// Update to lost status (from Borrowed -> Lost)
					status: LibraryItemInstanceStatus.Lost,
					// Not update from request
					isProcessBorrowRequest: false);
			}
			
			// Process update entity
			await _unitOfWork.Repository<BorrowRecord, int>().UpdateAsync(existingEntity);
			// Save DB
			var isSaved = await _unitOfWork.SaveChangesWithTransactionAsync() > 0;
			if (isSaved)
			{
				// Convert borrow record to dto
				var borrowRecDto = _mapper.Map<BorrowRecordDto>(existingEntity);
				
				// Retrieve all return items in existing borrow record
				var returnItemIds = borrowRecDetailList.Select(brd => brd.LibraryItemInstanceId).ToList();
				var returnItemToSend = borrowRecDto.BorrowRecordDetails
					.Where(brd => returnItemIds.Contains(brd.LibraryItemInstanceId))
					.ToList();
				// Retrieve all lost items in existing borrow record
				var lostItemsToSend = borrowRecDto.BorrowRecordDetails
					.Where(brd => lostItemInstanceIds.Contains(brd.LibraryItemInstanceId))
					.ToList();
				// Try to add fine policy for return items (if any)
				if (returnItemToSend.Any() && 
				    returnItemToSend.Any(r => r.Fines.Any()))
				{
					foreach (var fine in returnItemToSend.SelectMany(f => f.Fines))
					{
						fine.FinePolicy = (await _finePolicySvc.Value.GetByIdAsync(fine.FinePolicyId)).Data as FinePolicyDto ?? null!;
					}

					foreach (var item in returnItemToSend)
					{
						// Add items' fine policy
						foreach (var fine in item.Fines)
						{
							fine.FinePolicy = (await _finePolicySvc.Value.GetByIdAsync(fine.FinePolicyId)).Data as FinePolicyDto ?? null!;
						}
						
						// Add return condition
						if (item.ReturnConditionId != null &&
						    int.TryParse(item.ReturnConditionId.ToString() ?? "0", out var returnConditionId))
						{
							item.ReturnCondition = (await _conditionSvc.GetByIdAsync(returnConditionId)).Data as LibraryItemConditionDto ?? null!;
						} 
					}
				}
				// Try to add fine policy for lost items (if any)
				if (lostItemsToSend.Any() && 
				    lostItemsToSend.Any(r => r.Fines.Any()))
				{
					foreach (var fine in lostItemsToSend.SelectMany(f => f.Fines))
					{
						fine.FinePolicy = (await _finePolicySvc.Value.GetByIdAsync(fine.FinePolicyId)).Data as FinePolicyDto ?? null!;
					}
				}
				
				// Process success return item success email
				await SendReturnSuccessEmailAsync(
					email: userDto.Email,
					libCard: borrowRecDto.LibraryCard,
					returnItems: returnItemToSend,
					lostItems: lostItemsToSend,
					libName: _appSettings.LibraryName,
					libContact: _appSettings.LibraryContact);
				
				// TODO: Process assign item to pending reservation
				
				// Msg: Total {0} items have been returned successfully
				var msg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Success0008);
				return new ServiceResult(ResultCodeConst.Borrow_Success0008,
					StringUtils.Format(msg, borrowRecDetailList.Count.ToString()));
			}
			
			// Msg: Failed to process return items
			return new ServiceResult(ResultCodeConst.Borrow_Fail0010,
				await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Fail0010));
		}
		catch (ForbiddenException)
		{
			throw;
		}
		catch(UnprocessableEntityException)
		{
			throw;
		}
		catch (Exception ex)
		{
			_logger.Error(ex.Message);
			throw new Exception("Error invoke when process return library item");
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

	public async Task<IServiceResult> CountAllActiveRecordByLibCardIdAsync(Guid libraryCardId)
	{
		try
		{
			// Build specification
			var baseSpec = new BaseSpecification<BorrowRecord>(br =>
				br.LibraryCardId == libraryCardId); // With specific lib card
			// Count all active records
			var countRes = await _unitOfWork.Repository<BorrowRecord, int>()
				.GetAllWithSpecAndSelectorAsync(baseSpec, selector: s => 
					s.BorrowRecordDetails.Count(brd =>
						brd.Status != BorrowRecordStatus.Returned && // In returned status
						brd.Status != BorrowRecordStatus.Lost && // In lost status
						// Not include return fields
						brd.ReturnConditionId == null &&
						brd.ReturnDate == null
					));
			// Response
			return new ServiceResult(ResultCodeConst.SYS_Success0002,
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), countRes.Sum());
		}
		catch (Exception ex)
		{
			_logger.Error(ex.Message);
			throw new Exception("Error invoke when process count all active record by lib card id");
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
	
	private async Task<bool> SendBorrowSuccessEmailAsync(
		string email, BorrowRecordDto borrowRec,
		string libName, string libContact)
	{
		try
		{
			// Email subject
			var subject = "[ELIBRARY] Thông báo mượn tài liệu thành công";
		
			// Process send email
			var emailMessageDto = new EmailMessageDto( // Define email message
				// Define Recipient
				to: new List<string>() { email },
				// Define subject
				subject: subject,
				// Add email body content
				content: GetBorrowSuccessEmailBody(
					borrowRec: borrowRec,
					libName: libName,
					libContact:libContact)
			);
			
			// Process send email
			return await _emailSvc.SendEmailAsync(emailMessageDto, isBodyHtml: true);
		}
		catch (Exception ex)
		{
			_logger.Error(ex.Message);
			throw new Exception("Error invoke when process send borrow success email");
		}
	}
	
	private async Task<bool> SendReturnSuccessEmailAsync(
		string email, LibraryCardDto libCard, 
		List<BorrowRecordDetailDto> returnItems,
		List<BorrowRecordDetailDto> lostItems,
		string libName, string libContact)
	{
		try
		{
			// Email subject
			var subject = "[ELIBRARY] Thông báo trả tài liệu thành công";
			
			// Process send email
			var emailMessageDto = new EmailMessageDto( // Define email message
                // Define Recipient
                to: new List<string>() { email },
                // Define subject
                subject: subject,
                // Add email body content
                content: GetReturnSuccessEmailBody(
                    libCard: libCard,
                    returnItems: returnItems,
                    lostItems: lostItems,
                    libName: libName,
                    libContact:libContact)
            );
			
			// Process send email
			return await _emailSvc.SendEmailAsync(emailMessageDto, isBodyHtml: true);
		}
		catch (Exception ex)
		{
			_logger.Error(ex.Message);
			throw new Exception("Error invoke when process send borrow record return success email");
		}
	}

	private string GetReturnSuccessEmailBody(
	    LibraryCardDto libCard,
	    List<BorrowRecordDetailDto> returnItems,
	    List<BorrowRecordDetailDto> lostItems,
	    string libName, string libContact)
	{
	    // Initialize all main content of email
	    string headerMessage = string.Empty;
	    string mainMessage = string.Empty;
	    string returnSection = string.Empty;
	    string lostSection = string.Empty;
	    
	    // Initialize vietnamese culture info
	    var culture = new CultureInfo("vi-VN");
	    
	    // Process returned items
	    if (returnItems.Any())
	    {
	        headerMessage = "Thông Báo Hoàn Trả Tài Liệu Thành Công";
	        mainMessage = "Các tài liệu bạn đã hoàn trả đã được ghi nhận như sau:";

	        var returnItemList = string.Join("", returnItems.Select(item => {
	            string fineDetails = string.Empty;
	            if (item.Fines.Any())
	            {
	                fineDetails = string.Join("", item.Fines.Select(fine => 
	                    $"""
	                    <li>
	                        <p><strong>Số Tiền Phạt:</strong> <span class="status-text">{fine.FineAmount.ToString("C0", culture)}</span></p>
	                        <p><strong>Nội Dung Phí Phạt:</strong> {fine.FinePolicy.FinePolicyTitle}</p>
	                        <p><strong>Loại Phí Phạt:</strong> {fine.FinePolicy.ConditionType.GetDescription()}</p>
	                        <p><strong>Ghi Chú:</strong> {(string.IsNullOrEmpty(fine.FineNote) ? "Không có" : fine.FineNote)}</p>
	                        <p><strong>Trạng Thái:</strong> <span class="isbn">{fine.Status.GetDescription()}</span></p>
	                    </li>
	                    """));
	                fineDetails = $"""
	                    <p><strong>Các khoản phạt:</strong></p>
	                    <ul>{fineDetails}</ul>
	                    """;
	            }
	            
	            return $"""
	            <li>
	                <p><strong>Tiêu đề:</strong> <span class="title">{item.LibraryItemInstance.LibraryItem.Title}</span></p>
	                <p><strong>ISBN:</strong> <span class="isbn">{item.LibraryItemInstance.LibraryItem.Isbn}</span></p>
	                <p><strong>Năm Xuất Bản:</strong> {item.LibraryItemInstance.LibraryItem.PublicationYear}</p>
	                <p><strong>Nhà Xuất Bản:</strong> {item.LibraryItemInstance.LibraryItem.Publisher}</p>
	                <p><strong>Ngày Mượn:</strong> <span class="expiry-date">{item.BorrowRecord.BorrowDate:dd/MM/yyyy HH:mm}</span></p>
	                <p><strong>Ngày Hoàn Trả:</strong> <span class="expiry-date">{item.ReturnDate?.ToString("dd/MM/yyyy HH:mm")}</span></p>
	                <p><strong>Tình Trạng Mượn:</strong> <span class="isbn">{item.Condition?.VietnameseName}</span></p>
	                <p><strong>Tình Trạng Trả:</strong> <span class="isbn">{item.ReturnCondition?.VietnameseName}</span></p>
	                <p><strong>Số lần gia hạn:</strong> {item.BorrowDetailExtensionHistories?.Count ?? 0} lần
	                {fineDetails}
	            </li>
	            """;
	        }));

	        returnSection = $$"""
	            <p><strong>Thông Tin Tài Liệu Hoàn Trả:</strong></p>
	            <div class="details">
	                <ul>
	                    {{returnItemList}}
	                </ul>
	            </div>
	            """;
	    }
	    
	    // Process lost items (if any)
	    if (lostItems.Any())
	    {
	        // Append header and main messages for lost items if already processing return items
	        if (!string.IsNullOrEmpty(headerMessage))
	            headerMessage += " và Báo Mất Tài Liệu";
	        else
	            headerMessage = "Thông Báo Báo Mất Tài Liệu";

	        if (!string.IsNullOrEmpty(mainMessage))
	            mainMessage += " Ngoài ra, các tài liệu báo mất của bạn được ghi nhận như sau:";
	        else
	            mainMessage = "Các tài liệu báo mất của bạn được ghi nhận như sau:"; 

	        var lostItemList = string.Join("", lostItems.Select(item => {
	            string fineDetails = string.Empty;
	            if (item.Fines.Any())
	            {
	                fineDetails = string.Join("", item.Fines.Select(fine => 
	                    $"""
	                    <li>
	                        <p><strong>Số Tiền Phạt:</strong> <span class="status-text">{fine.FineAmount.ToString("C0", culture)}</span></p>
	                        <p><strong>Nội Dung Phí Phạt:</strong> {fine.FinePolicy.FinePolicyTitle}</p>
	                        <p><strong>Loại Phí Phạt:</strong> {fine.FinePolicy.ConditionType.GetDescription()}</p>
	                        <p><strong>Ghi Chú:</strong> {(string.IsNullOrEmpty(fine.FineNote) ? "Không có" : fine.FineNote)}</p>
	                        <p><strong>Trạng Thái:</strong> <span class="isbn">{fine.Status.GetDescription()}</span></p>
	                    </li>
	                    """));
	                fineDetails = $"""
	                    <p><strong>Các khoản phạt:</strong></p>
	                    <ul>{fineDetails}</ul>
	                    """;
	            }
	            
	            return $"""
	            <li>
	                <p><strong>Tiêu đề:</strong> <span class="title">{item.LibraryItemInstance.LibraryItem.Title}</span></p>
	                <p><strong>ISBN:</strong> <span class="isbn">{item.LibraryItemInstance.LibraryItem.Isbn}</span></p>
	                <p><strong>Năm Xuất Bản:</strong> {item.LibraryItemInstance.LibraryItem.PublicationYear}</p>
	                <p><strong>Nhà Xuất Bản:</strong> {item.LibraryItemInstance.LibraryItem.Publisher}</p>
	                <p><strong>Ngày Mượn:</strong> <span class="expiry-date">{item.BorrowRecord.BorrowDate:dd/MM/yyyy HH:mm}</span></p>
	                <p><strong>Số Lần Gia Hạn:</strong> {item.BorrowDetailExtensionHistories?.Count ?? 0} lần
	                <p><strong>Tình Trạng:</strong> <span class="status-text">{BorrowRecordStatus.Lost.GetDescription()}</span></p>
	                {fineDetails}
	            </li>
	            """;
	        }));

	        lostSection = $$"""
	            <p><strong>Thông Tin Tài Liệu Báo Mất:</strong></p>
	            <div class="details">
	                <ul>
	                    {{lostItemList}}
	                </ul>
	            </div>
	            """;
	    }
	    
	    // Build final email HTML content
	    return $$$"""
	        <!DOCTYPE html>
	        <html>
	        <head>
	            <meta charset="UTF-8">
	            <title>{{{headerMessage}}}</title>
	            <style>
	                body {
	                    font-family: Arial, sans-serif;
	                    line-height: 1.6;
	                    color: #333;
	                }
	                .header {
	                    font-size: 18px;
	                    color: #2c3e50;
	                    font-weight: bold;
	                }
	                .details {
	                    margin: 15px 0;
	                    padding: 10px;
	                    background-color: #f9f9f9;
	                    border-left: 4px solid #27ae60;
	                }
	                .details ul {
	                    list-style-type: disc;
	                    padding-left: 20px;
	                }
	                .details li {
	                    margin: 5px 0;
	                }
	                .footer {
	                    margin-top: 20px;
	                    font-size: 14px;
	                    color: #7f8c8d;
	                }
	                .isbn {
	                    color: #2980b9;
	                    font-weight: bold;
	                }
	                .title {
	                    color: #f39c12;
	                    font-weight: bold;
	                }
	                .expiry-date {
	                    color: #27ae60;
	                    font-weight: bold;
	                }
	                .status-text {
	                    color: #c0392b;
	                    font-weight: bold;
	                }
	            </style>
	        </head>
	        <body>
	            <p class="header">{{{headerMessage}}}</p>
	            <p>Xin chào {{{libCard.FullName}}},</p>
	            <p>{{{mainMessage}}}</p>
	            
	            {{{returnSection}}}
	            {{{lostSection}}}
	            
	            <p>Nếu có bất kỳ thắc mắc hoặc cần hỗ trợ, vui lòng liên hệ qua email: <strong>{{{libContact}}}</strong>.</p>
	            <p>Cảm ơn bạn đã sử dụng dịch vụ của thư viện.</p>
	            
	            <p class="footer"><strong>Trân trọng,</strong></p>
	            <p class="footer">{{{libName}}}</p>
	        </body>
	        </html>
	        """;
	}

	private string GetBorrowSuccessEmailBody(BorrowRecordDto borrowRec, string libName, string libContact)
	{
	    // Initialize all main content of email
	    string headerMessage = "Thông Báo Mượn Tài Liệu Thành Công";
	    string mainMessage = "Yêu cầu mượn tài liệu của bạn đã được xử lý thành công. Dưới đây là chi tiết giao dịch mượn:";
	    string topInfoSection = string.Empty;
	    string requestSection = string.Empty;
	    string recordSection = string.Empty;
	    
	    // Initialize borrow record section
	    topInfoSection = $$"""
	        <p><strong>Thông Tin Mượn:</strong></p>
	        <div class="details">
	            <ul>
	                <li><strong>Ngày Mượn:</strong> <span class="expiry-date">{{borrowRec.BorrowDate:dd/MM/yyyy HH:mm}}</span></li>
	                <li><strong>Hình Thức Mượn:</strong> {{borrowRec.BorrowType.GetDescription()}}</li>
	                <li><strong>Tổng Số Tài Liệu:</strong> {{borrowRec.TotalRecordItem}}</li>
	            </ul>
	        </div>
	        """;
	    
	    // Process Borrow Request details (if exist)
	    if (borrowRec.BorrowRequest != null && borrowRec.BorrowRequest.BorrowRequestDetails.Any())
	    {
	        var requestItemList = string.Join("", borrowRec.BorrowRequest.BorrowRequestDetails.Select(detail =>
	            $"""
	            <li>
	                <p><strong>Tiêu đề:</strong> <span class="title">{detail.LibraryItem.Title}</span></p>
	                <p><strong>ISBN:</strong> <span class="isbn">{detail.LibraryItem.Isbn}</span></p>
	                <p><strong>Năm Xuất Bản:</strong> {detail.LibraryItem.PublicationYear}</p>
	                <p><strong>Nhà Xuất Bản:</strong> {detail.LibraryItem.Publisher}</p>
	            </li>
	            """));
	    
	        requestSection = $$"""
	            <p><strong>Thông Tin Yêu Cầu Đặt Trước:</strong></p>
	            <div class="details">
	                <ul>
	                    {{requestItemList}}
	                </ul>
	            </div>
	            """;
	    }
	    
	    // Process Borrow Record Details 
	    if (borrowRec.BorrowRecordDetails.Any())
	    {
	        var recordItemList = string.Join("", borrowRec.BorrowRecordDetails.Select(detail => 
	            $"""
	            <li>
	                <p><strong>Barcode:</strong> <strong>{detail.LibraryItemInstance.Barcode}</strong></p>
	                <p><strong>Tiêu đề:</strong> <span class="title">{detail.LibraryItemInstance.LibraryItem.Title}</span></p>
	                <p><strong>ISBN:</strong> <span class="isbn">{detail.LibraryItemInstance.LibraryItem.Isbn}</span></p>
	                <p><strong>Năm Xuất Bản:</strong> {detail.LibraryItemInstance.LibraryItem.PublicationYear}</p>
	                <p><strong>Nhà Xuất Bản:</strong> {detail.LibraryItemInstance.LibraryItem.Publisher}</p>
	                <p><strong>Tình Trạng Khi Mượn:</strong> <span class="isbn">{detail.Condition.VietnameseName}</span></p>
	                <p><strong>Ngày Hết Hạn:</strong> <span class="expiry-date">{detail.DueDate:dd/MM/yyyy HH:mm}</span></p>
	                <p><strong>Số Lần Gia Hạn Tối Đa:</strong> {_borrowSettings.MaxBorrowExtension} lần (nếu có bạn đọc đặt trước thì chỉ được gia hạn 1 lần)</p>
	            </li>
	            """));
	    
	        recordSection = $$"""
	            <p><strong>Thông Tin Tài Liệu Mượn:</strong></p>
	            <div class="details">
	                <ul>
	                    {{recordItemList}}
	                </ul>
	            </div>
	            """;
	    }
	    
	    // Build final email HTML content
	    return $$"""
	        <!DOCTYPE html>
	        <html>
	        <head>
	            <meta charset="UTF-8">
	            <title>{{headerMessage}}</title>
	            <style>
	                body {
	                    font-family: Arial, sans-serif;
	                    line-height: 1.6;
	                    color: #333;
	                }
	                .header {
	                    font-size: 18px;
	                    color: #2c3e50;
	                    font-weight: bold;
	                }
	                .details {
	                    margin: 15px 0;
	                    padding: 10px;
	                    background-color: #f9f9f9;
	                    border-left: 4px solid #27ae60;
	                }
	                .details ul {
	                    list-style-type: disc;
	                    padding-left: 20px;
	                }
	                .details li {
	                    margin: 5px 0;
	                }
	                .footer {
	                    margin-top: 20px;
	                    font-size: 14px;
	                    color: #7f8c8d;
	                }
	                .isbn {
	                    color: #2980b9;
	                    font-weight: bold;
	                }
	                .title {
	                    color: #f39c12;
	                    font-weight: bold;
	                }
	                .expiry-date {
	                    color: #27ae60;
	                    font-weight: bold;
	                }
	                .status-text {
	                    color: #c0392b;
	                    font-weight: bold;
	                }
	            </style>
	        </head>
	        <body>
	            <p class="header">{{headerMessage}}</p>
	            <p>Xin chào {{borrowRec.LibraryCard.FullName}},</p>
	            <p>{{mainMessage}}</p>
	            
	            {{topInfoSection}}
	            {{requestSection}}
	            {{recordSection}}
	            
	            <p>Nếu có bất kỳ thắc mắc hoặc cần hỗ trợ, vui lòng liên hệ qua số <strong>{{libContact}}</strong>.</p>
	            <p>Cảm ơn bạn đã sử dụng dịch vụ của thư viện.</p>
	            
	            <p class="footer"><strong>Trân trọng,</strong></p>
	            <p class="footer">{{libName}}</p>
	        </body>
	        </html>
	        """;
	}

}