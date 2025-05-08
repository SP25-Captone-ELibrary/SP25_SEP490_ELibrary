using System.Globalization;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Dashboard;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Dtos.WarehouseTrackings;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly ISystemMessageService _msgService;
    
    private readonly IUserService<UserDto> _userSvc;
    private readonly ICategoryService<CategoryDto> _cateSvc;
    private readonly ILibraryItemService<LibraryItemDto> _itemSvc;
    private readonly ILibraryCardService<LibraryCardDto> _cardSvc;
    private readonly ITransactionService<TransactionDto> _transSvc;
    private readonly IBorrowRecordService<BorrowRecordDto> _borrowRecSvc;
    private readonly ILibraryResourceService<LibraryResourceDto> _resourceSvc;
    private readonly IDigitalBorrowService<DigitalBorrowDto> _digitalBorrowSvc;
    private readonly IWarehouseTrackingService<WarehouseTrackingDto> _whTrackingSvc;
    private readonly IReservationQueueService<ReservationQueueDto> _reservationQueueSvc;
    private readonly ILibraryItemInventoryService<LibraryItemInventoryDto> _inventorySvc;
    private readonly ILibraryItemInstanceService<LibraryItemInstanceDto> _itemInstanceSvc;
    private readonly IBorrowRequestDetailService<BorrowRequestDetailDto> _borrowReqDetailSvc;
    private readonly IBorrowRecordDetailService<BorrowRecordDetailDto> _borrowRecDetailSvc;
    private readonly IWarehouseTrackingDetailService<WarehouseTrackingDetailDto> _whTrackingDetailSvc;
    private readonly IBorrowDetailExtensionHistoryService<BorrowDetailExtensionHistoryDto> _extensionHistorySvc;
    private readonly IDigitalBorrowExtensionHistoryService<DigitalBorrowExtensionHistoryDto> _digitalBorrowExtensionSvc;

    public DashboardService(
        ILogger logger,
        IMapper mapper,
        ISystemMessageService msgService,
        IUserService<UserDto> userSvc,
        ICategoryService<CategoryDto> cateSvc,
        ILibraryItemService<LibraryItemDto> itemSvc,
        ILibraryCardService<LibraryCardDto> cardSvc,
        ITransactionService<TransactionDto> transSvc,
        IBorrowRecordService<BorrowRecordDto> borrowRecSvc,
        ILibraryResourceService<LibraryResourceDto> resourceSvc,
        IDigitalBorrowService<DigitalBorrowDto> digitalBorrowSvc,
        IDigitalBorrowExtensionHistoryService<DigitalBorrowExtensionHistoryDto> digitalBorrowExtensionSvc,
        IWarehouseTrackingService<WarehouseTrackingDto> whTrackingSvc,
        IReservationQueueService<ReservationQueueDto> reservationQueueSvc,
        ILibraryItemInventoryService<LibraryItemInventoryDto> inventorySvc,
        ILibraryItemInstanceService<LibraryItemInstanceDto> itemInstanceSvc,
        IBorrowRequestDetailService<BorrowRequestDetailDto> borrowReqDetailSvc,
        IBorrowRecordDetailService<BorrowRecordDetailDto> borrowRecDetailSvc,
        IWarehouseTrackingDetailService<WarehouseTrackingDetailDto> whTrackingDetailSvc,
        IBorrowDetailExtensionHistoryService<BorrowDetailExtensionHistoryDto> extensionHistorySvc
    )
    {
        _mapper = mapper;
        _logger = logger;
        _msgService = msgService;
        _itemSvc = itemSvc;
        _cateSvc = cateSvc;
        _cardSvc = cardSvc;
        _userSvc = userSvc;
        _transSvc = transSvc;
        _resourceSvc = resourceSvc;
        _borrowRecSvc = borrowRecSvc;
        _inventorySvc = inventorySvc;
        _whTrackingSvc = whTrackingSvc;
        _itemInstanceSvc = itemInstanceSvc;
        _digitalBorrowSvc = digitalBorrowSvc;
        _borrowReqDetailSvc = borrowReqDetailSvc;
        _digitalBorrowExtensionSvc = digitalBorrowExtensionSvc;
        _borrowRecDetailSvc = borrowRecDetailSvc;
        _whTrackingDetailSvc = whTrackingDetailSvc;
        _reservationQueueSvc = reservationQueueSvc;
        _extensionHistorySvc = extensionHistorySvc;
    }
    
    public async Task<IServiceResult> GetDashboardOverviewAsync(DateTime? startDate, DateTime? endDate, TrendPeriod period)
    {
        try
        {
            // Get current local datetime in Vietnam timezone.
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Calculate valid date range based on period
            (DateTime validStartDate, DateTime validEndDate) = GetValidDateRange(startDate, endDate, period, currentLocalDateTime);
            
            // Initialize dashboard overview
            var dashboardOverView = new DashboardOverviewDto();
            
            #region Dashboard overview
            // Count total instance units
            var countTotalInstanceSpec = new BaseSpecification<LibraryItemInstance>();
            if(startDate != null || endDate != null)
            {
                countTotalInstanceSpec.AddFilter(li => li.CreatedAt.Date >= validStartDate.Date && 
                                                       li.CreatedAt.Date <= validEndDate.Date);
            }
            if ((await _itemInstanceSvc.CountAsync(countTotalInstanceSpec)).Data is int countTotalInstanceRes)
            {
                dashboardOverView.TotalInstanceUnits = countTotalInstanceRes;
            }
            
            // Count available units
            var countAvailableSpec = new BaseSpecification<LibraryItemInstance>(l => 
                l.Status == nameof(LibraryItemInstanceStatus.InShelf) || 
                l.Status == nameof(LibraryItemInstanceStatus.OutOfShelf));
            if(startDate != null || endDate != null)
            {
                countAvailableSpec.AddFilter(li => li.CreatedAt.Date >= validStartDate.Date && 
                                                   li.CreatedAt.Date <= validEndDate.Date);
            }
            if((await _itemInstanceSvc.CountAsync(countAvailableSpec)).Data is int countAvailableRes)
            {
                dashboardOverView.TotalAvailableUnits = countAvailableRes;
            }
            
            // Calculate total items
            var countItemSpec = new BaseSpecification<LibraryItem>();
            if(startDate != null || endDate != null)
            {
                countItemSpec.AddFilter(li => li.CreatedAt.Date >= validStartDate.Date && 
                                               li.CreatedAt.Date <= validEndDate.Date);
            }
            if((await _itemSvc.CountAsync(countItemSpec)).Data is int countItemRes) dashboardOverView.TotalItemUnits = countItemRes;
            
            // Calculate total digital
            var countResourceSpec = new BaseSpecification<LibraryResource>();
            if(startDate != null || endDate != null)
            {
                countResourceSpec.AddFilter(lr => lr.CreatedAt.Date >= validStartDate.Date && 
                                                 lr.CreatedAt.Date <= validEndDate.Date);
            }
            if((await _resourceSvc.CountAsync()).Data is int countResourceRes) dashboardOverView.TotalDigitalUnits = countResourceRes;
            
            // Calculate total overdue units
            var overDueSpec = new BaseSpecification<BorrowRecordDetail>(brd => 
                brd.Status == BorrowRecordStatus.Overdue && 
                // Has not returned this item yet
                brd.ReturnConditionId == null && 
                brd.ReturnDate == null);
            if(startDate != null || endDate != null)
            {
                overDueSpec.AddFilter(brd => brd.DueDate >= validStartDate.Date && 
                                             brd.DueDate <= validEndDate.Date);
            }
            if ((await _borrowRecDetailSvc.CountAsync(overDueSpec)).Data is int overDueUnits) dashboardOverView.TotalOverdueUnits = overDueUnits;
            
            // Calculate total borrowing units
            var countTotalBorrowingSpec = new BaseSpecification<BorrowRecordDetail>(brd => 
                brd.Status == BorrowRecordStatus.Borrowing);
            if(startDate != null || endDate != null)
            {
                countTotalBorrowingSpec.AddFilter(brd => brd.BorrowRecord.BorrowDate.Date >= validStartDate.Date && 
                                                         brd.BorrowRecord.BorrowDate.Date <= validEndDate.Date);
            }
            if ((await _borrowRecDetailSvc.CountAsync(countTotalBorrowingSpec)).Data is int totalBorrowingRes)
            {
                dashboardOverView.TotalBorrowingUnits = totalBorrowingRes;
            }
            
            // Calculate total lost units
            var countTotalLostSpec = new BaseSpecification<BorrowRecordDetail>(brd => brd.Status == BorrowRecordStatus.Lost);
            if(startDate != null || endDate != null)
            {
                countTotalLostSpec.AddFilter(brd => brd.BorrowRecord.BorrowDate >= validStartDate.Date && 
                                                    brd.BorrowRecord.BorrowDate <= validEndDate.Date);
            }
            if ((await _borrowRecDetailSvc.CountAsync(countTotalLostSpec)).Data is int totalLostRes)
            {
                dashboardOverView.TotalLostUnits = totalLostRes;
            }
            
            // Calculate members (exclude all users have role as admin)
            var countUserSpec = new BaseSpecification<User>(u => u.Role.EnglishName != nameof(Role.Administration));
            if((await _userSvc.CountAsync(countUserSpec)).Data is int usersCountRes) dashboardOverView.TotalPatrons = usersCountRes;
            #endregion

            #region Inventory & stock management
            // Build up category inventory summary (display with pie chart)
            var inventoryAndStockDto = new DashboardInventoryAndStockDto();
            
            // Retrieve all categories
            var categorySpec = new BaseSpecification<Category>();
            var categories = (await _cateSvc.GetAllWithSpecAsync(categorySpec)).Data as List<CategoryDto>;
            if (categories != null && categories.Any())
            {
                // Initialize inventory stock summary
                inventoryAndStockDto.InventoryStockSummary = new();
                
                // Iterate each category to build up pie chart data
                foreach (var category in categories)
                {
                    // Initialize inventory category summary
                    var summary = new InventoryCategorySummaryDto();
                    
                    // Retrieve all warehouse tracking details based on category
                    var whDetailSpec = new BaseSpecification<WarehouseTrackingDetail>(w => 
                        w.CategoryId == category.CategoryId &&
                        w.WarehouseTracking.TrackingType == TrackingType.StockIn);
                    // Add date range filter (if any)
                    if (startDate != null || endDate != null)
                    {
                        whDetailSpec.AddFilter(w => w.CreatedAt.Date >= validStartDate.Date && w.CreatedAt.Date <= validEndDate.Date);    
                    }
                    var whTrackingDetails = (await _whTrackingDetailSvc.GetAllWithSpecAsync(whDetailSpec)
                        ).Data as List<WarehouseTrackingDetailDto>;
                    // Count total stock-in items
                    summary.TotalStockInItem = whTrackingDetails?.Count ?? 0;
                    // Count total instance item <- Sum of each warehouse tracking detail's item total
                    summary.TotalInstanceItem = whTrackingDetails != null && whTrackingDetails.Count != 0
                        ? whTrackingDetails.Select(wtd => wtd.ItemTotal).Sum() 
                        : 0;
                    // Count total cataloged item <- Any tracking detail request along with libraryItemId > 0 or libraryItemId != null
                    summary.TotalCatalogedItem = whTrackingDetails?.Count(wtd => wtd.LibraryItemId != null && wtd.LibraryItemId > 0) ?? 0;
                    // Count total instance of cataloged item  
                    summary.TotalCatalogedInstanceItem = whTrackingDetails?
                        .Where(wtd => wtd.LibraryItemId != null && wtd.LibraryItemId > 0 && wtd.HasGlueBarcode)
                        .Select(wtd => wtd.ItemTotal).Sum() ?? 0;
                    // Assign category
                    summary.Category = category;
                    
                    // Add to dashboard inventory stock category list
                    inventoryAndStockDto.InventoryStockCategorySummary.Add(summary);
                    // Accumulate to inventory stock
                    inventoryAndStockDto.InventoryStockSummary.TotalStockInItem += summary.TotalStockInItem;
                    inventoryAndStockDto.InventoryStockSummary.TotalInstanceItem += summary.TotalInstanceItem;
                    inventoryAndStockDto.InventoryStockSummary.TotalCatalogedItem += summary.TotalCatalogedItem;
                    inventoryAndStockDto.InventoryStockSummary.TotalCatalogedInstanceItem += summary.TotalCatalogedInstanceItem;
                }
            }
            #endregion

            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                new
                {
                    DashboardOverView = dashboardOverView,
                    DashboardInventoryAndStock = inventoryAndStockDto
                });
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get dashboard data");
        }
    }

    public async Task<IServiceResult> GetDashboardCirculationAndBorrowingAnalyticsAsync(
        DateTime? startDate, DateTime? endDate, TrendPeriod period)
    {
        try
        {
            // Determine current system lang 
            var lang = (SystemLanguage?) EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage) ?? SystemLanguage.English;
            
            // Get current local datetime in Vietnam timezone.
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Calculate valid date range based on period
            (DateTime validStartDate, DateTime validEndDate) = GetValidDateRange(startDate, endDate, period, currentLocalDateTime);
            
            // Retrieve all categories
            var categorySpec = new BaseSpecification<Category>();
            var categories = (await _cateSvc.GetAllWithSpecAsync(categorySpec)).Data as List<CategoryDto>;
            
            // Initialize dashboard circulation
            var dashboardCirculation = new DashboardCirculationDto();
            
            // Initialize total units
            var totalBorrowingUnits = 0;
            var totalOverdueUnits = 0;
            var totalLostUnits = 0;
            var totalBorrowFailed = 0;
            var totalRequestUnits = 0;
            
            // Sum request units
            // Count total request
            var countTotalRequestSpec = new BaseSpecification<BorrowRequestDetail>(r => 
                r.BorrowRequest.Status == BorrowRequestStatus.Created);
            if(startDate != null || endDate != null)
            {
                countTotalRequestSpec.AddFilter(brd => brd.BorrowRequest.RequestDate.Date >= validStartDate.Date && 
                                                       brd.BorrowRequest.RequestDate.Date <= validEndDate.Date);
            }
            if ((await _borrowReqDetailSvc.CountAsync(countTotalRequestSpec)).Data is int totalRequestRes)
            {
                // Assign to dashboard value
                dashboardCirculation.TotalRequestUnits = totalRequestRes;
                // Assign to request units
                totalRequestUnits = totalRequestRes;
            }
            
            // Count total borrowing
            var countBorrowingSpec = new BaseSpecification<BorrowRecordDetail>(brd => brd.Status == BorrowRecordStatus.Borrowing);
            if(startDate != null || endDate != null)
            {
                countBorrowingSpec.AddFilter(brd => brd.BorrowRecord.BorrowDate.Date >= validStartDate.Date && 
                                                    brd.BorrowRecord.BorrowDate.Date <= validEndDate.Date);
            }
            if ((await _borrowRecDetailSvc.CountAsync(countBorrowingSpec)).Data is int totalBorrowingRes) totalBorrowingUnits = totalBorrowingRes;
            
             // Count total overdue
            var countOverdueSpec = new BaseSpecification<BorrowRecordDetail>(brd => brd.Status == BorrowRecordStatus.Overdue);
            if(startDate != null || endDate != null)
            {
                countOverdueSpec.AddFilter(brd => brd.DueDate >= validStartDate.Date && 
                                                  brd.DueDate <= validEndDate.Date);
            }
            if((await _borrowRecDetailSvc.CountAsync(countOverdueSpec)).Data is int totalOverdueRes)
            {
                // Assign to dashboard value
                dashboardCirculation.TotalOverdue = totalOverdueRes;
                // Assign to overdue units
                totalOverdueUnits = totalOverdueRes;
            }
            
            // Count total lost
            var countLostSpec = new BaseSpecification<BorrowRecordDetail>(brd => brd.Status == BorrowRecordStatus.Lost);
            if(startDate != null || endDate != null)
            {
                countLostSpec.AddFilter(brd => brd.BorrowRecord.BorrowDate.Date >= validStartDate.Date && 
                                               brd.BorrowRecord.BorrowDate.Date <= validEndDate.Date);
            }
            if ((await _borrowRecDetailSvc.CountAsync(countLostSpec)).Data is int totalLostRes)
            {
                // Assign to dashboard value
                dashboardCirculation.TotalLost = totalLostRes;
                // Assign to lost units
                totalLostUnits = totalLostRes;
            }
            
            // Count total borrow failed
            var countBorrowFailedSpec = new BaseSpecification<ReservationQueue>(r => 
                r.IsReservedAfterRequestFailed &&
                r.QueueStatus == ReservationQueueStatus.Pending);
            if(startDate != null || endDate != null)
            {
                countBorrowFailedSpec.AddFilter(brd => brd.ReservationDate >= validStartDate.Date && 
                                                       brd.ReservationDate <= validEndDate.Date);
            }
            if ((await _reservationQueueSvc.CountAsync(countBorrowFailedSpec)).Data is int totalFailedRes)
            {
                // Assign total borrow failed
                dashboardCirculation.TotalBorrowFailed = totalFailedRes;
                dashboardCirculation.TotalReservedUnits = totalFailedRes;
                // Assign to total borrow failed 
                totalBorrowFailed = totalFailedRes;
            }
            
            // Calculate total borrowed units
            var totalBorrowedUnits = totalBorrowingUnits + totalOverdueUnits + totalLostUnits;
            // Process calculate rates
            if (totalBorrowedUnits == 0)
            {
                dashboardCirculation.OverdueRates = totalOverdueUnits > 0 ? 100 : 0; // 100% failure if failures exist
                dashboardCirculation.LostRates = totalLostUnits > 0 ? 100 : 0; // 100% failure if failures exist
                dashboardCirculation.BorrowFailedRates = totalBorrowFailed > 0 ? 100 : 0; // 100% failure if failures exist
            }
            
            #region Overdue
            // Recalculate overdue rates if total borrowed exceeds than 0
            if (totalBorrowedUnits > 0)
            {
                // Calculate overdue rate
                dashboardCirculation.OverdueRates = (double)totalOverdueUnits / totalBorrowedUnits * 100;
                // Format double value
                dashboardCirculation.OverdueRates = Math.Truncate(dashboardCirculation.OverdueRates * 100) / 100;
            }
            // Calculate borrow failed summary for each category
            if (categories != null && categories.Any())
            {
                foreach (var category in categories)
                {
                    // Initialize overdue category summary 
                    var overdueCateSummary = new OverdueCategorySummaryDto();
                    
                    // Add filter
                    countOverdueSpec.AddFilter(brd => brd.LibraryItemInstance.LibraryItem.CategoryId == category.CategoryId);
                    if(startDate != null || endDate != null)
                    {
                        countOverdueSpec.AddFilter(brd => brd.DueDate.Date >= validStartDate.Date && 
                                                          brd.DueDate.Date <= validEndDate.Date);
                    }
                    // Recount total overdue
                    int.TryParse((await _borrowRecDetailSvc.CountAsync(countOverdueSpec)).Data?.ToString() ?? "0", out totalOverdueRes);
                    
                    // Assign count val
                    overdueCateSummary.TotalOverdue = totalOverdueRes;
                    
                    if (totalBorrowedUnits == 0)
                    {
                        overdueCateSummary.OverdueRates = totalOverdueRes > 0 ? 100 : 0; // 100% failure if failures exist
                    }
                    else
                    {
                        // Calculate overdue rate
                        overdueCateSummary.OverdueRates = (double)totalOverdueRes / totalBorrowedUnits * 100;
                        // Format double value
                        overdueCateSummary.OverdueRates = Math.Truncate(overdueCateSummary.OverdueRates * 100) / 100;
                    }
                    
                    // Assign category
                    overdueCateSummary.Category = category;
                    
                    // Add to summary list
                    dashboardCirculation.CategoryOverdueSummary.Add(overdueCateSummary);
                }
            }
            #endregion

            #region Lost
            // Recalculate lost rates if total borrowed exceeds than 0
            if (totalBorrowedUnits > 0)
            {
                // Calculate overdue rate
                dashboardCirculation.LostRates = (double)totalLostUnits / totalBorrowedUnits * 100;
                // Format double value
                dashboardCirculation.LostRates = Math.Truncate(dashboardCirculation.LostRates * 100) / 100;
            }
            // Calculate borrow failed summary for each category
            if (categories != null && categories.Any())
            {
                foreach (var category in categories)
                {
                    // Initialize lost category summary 
                    var lostCateSummary = new LostCategorySummaryDto();
                        
                    // Add filter
                    countLostSpec.AddFilter(brd => brd.LibraryItemInstance.LibraryItem.CategoryId == category.CategoryId);
                    if(startDate != null || endDate != null)
                    {
                        countLostSpec.AddFilter(brd => brd.BorrowRecord.BorrowDate.Date >= validStartDate.Date && 
                                                       brd.BorrowRecord.BorrowDate.Date <= validEndDate.Date);
                    }
                    // Recount total lost
                    int.TryParse((await _borrowRecDetailSvc.CountAsync(countLostSpec)).Data?.ToString() ?? "0", out totalLostRes);
                    // Assign count val
                    lostCateSummary.TotalLost = totalLostRes;
                        
                    if (totalBorrowingUnits == 0)
                    {
                        dashboardCirculation.LostRates = totalLostRes > 0 ? 100 : 0; // 100% failure if failures exist
                    }
                    else
                    {
                        // Calculate lost rate
                        lostCateSummary.LostRates = (double)totalLostRes / totalBorrowedUnits * 100;
                        // Format double value
                        lostCateSummary.LostRates = Math.Truncate(lostCateSummary.LostRates * 100) / 100;
                    }
                        
                    // Assign category
                    lostCateSummary.Category = category;
                        
                    // Add to summary list
                    dashboardCirculation.CategoryLostSummary.Add(lostCateSummary);
                }
            }
            #endregion

            #region Borrow Failed
            // Recalculate borrow failed rates if total borrowed exceeds than 0
            if (totalBorrowedUnits > 0)
            {
                // Calculate borrow failed rate
                dashboardCirculation.BorrowFailedRates = (double)totalBorrowFailed / (totalBorrowFailed + totalBorrowedUnits + totalRequestUnits) * 100;
                // Format double value
                dashboardCirculation.BorrowFailedRates = Math.Truncate(dashboardCirculation.BorrowFailedRates * 100) / 100;
            }
            // Calculate borrow failed summary for each category
            if (categories != null && categories.Any())
            {
                foreach (var category in categories)
                {
                    // Initialize borrowed fail summary 
                    var borrowFailedSummary = new BorrowFailedCategorySummaryDto();
                    
                    // Add filter for each category
                    var borrowFailedForCategorySpec = new BaseSpecification<ReservationQueue>(r => 
                        r.IsReservedAfterRequestFailed &&
                        r.QueueStatus == ReservationQueueStatus.Pending &&
                        r.LibraryItem.CategoryId == category.CategoryId);
                    if(startDate != null || endDate != null)
                    {
                        borrowFailedForCategorySpec.AddFilter(brd => brd.ReservationDate >= validStartDate.Date && 
                                                                     brd.ReservationDate <= validEndDate.Date);
                    }
                    // Recount total failed
                    int.TryParse((await _reservationQueueSvc.CountAsync(borrowFailedForCategorySpec)).Data?.ToString() ?? "0", out totalFailedRes);
                    // Assign count val
                    borrowFailedSummary.TotalBorrowFailed = totalFailedRes;
                    // Determine total borrow units equal to 0
                    if (totalBorrowingUnits == 0)
                    {
                        borrowFailedSummary.BorrowFailedRates = totalFailedRes > 0 ? 100 : 0; // 100% failure if failures exist
                    }
                    else
                    {
                        // Calculate borrow failed rate
                        borrowFailedSummary.BorrowFailedRates = (double)totalFailedRes / (totalFailedRes + totalBorrowedUnits + totalRequestUnits) * 100;
                        // Format double value
                        borrowFailedSummary.BorrowFailedRates = Math.Truncate(borrowFailedSummary.BorrowFailedRates * 100) / 100;
                    }
                    // Assign category
                    borrowFailedSummary.Category = category;
                    
                    // Add to summary list
                    dashboardCirculation.CategoryBorrowFailedSummary.Add(borrowFailedSummary);
                }
            }
            #endregion
            
            // Check whether is year comparision
            if (period != TrendPeriod.YearComparision)
            {
                // Retrieve borrow dates
                var borrowSpec = new BaseSpecification<BorrowRecordDetail>(
                    br => br.BorrowRecord.BorrowDate.Date >= validStartDate.Date &&
                          br.BorrowRecord.BorrowDate.Date <= validEndDate.Date);
                var borrowDates = (await _borrowRecDetailSvc.GetAllWithSpecAndSelectorAsync(
                    specification: borrowSpec,
                    selector: s => s.BorrowRecord.BorrowDate)).Data as List<DateTime>;
                var borrowTrends = GetTrendData(
                    dates: borrowDates,
                    startDate: validStartDate,
                    endDate: validEndDate,
                    period: period, lang: lang);
    
                // Retrieve return dates
                var returnSpec = new BaseSpecification<BorrowRecordDetail>(
                    br => br.ReturnDate.HasValue &&
                          br.ReturnDate.Value.Date >= validStartDate.Date && br.ReturnDate.Value.Date <= validEndDate.Date);
                var returnDates = (await _borrowRecDetailSvc.GetAllWithSpecAndSelectorAsync(
                    specification: returnSpec,
                    selector: s => s.ReturnDate)).Data as List<DateTime?>;
                var hasValueReturnDates = returnDates?
                     .Where(dt => dt.HasValue)
                     .Select(dt => dt!.Value)
                     .ToList()?? new List<DateTime>();
                var returnTrends = GetTrendData(
                    dates: hasValueReturnDates,
                    startDate: validStartDate,
                    endDate: validEndDate,
                    period: period, lang: lang);
    
                // Assign borrow and return trends
                dashboardCirculation.BorrowTrends = borrowTrends;
                dashboardCirculation.ReturnTrends = returnTrends;
            }
            else
            {
                int currentYear = currentLocalDateTime.Year;
                int lastYear = currentYear - 1;
        
                // Define start and end dates for both years
                DateTime startCurrentYear = new DateTime(currentYear, 1, 1);
                DateTime endCurrentYear = new DateTime(currentYear, 12, 31);
                DateTime startLastYear = new DateTime(lastYear, 1, 1);
                DateTime endLastYear = new DateTime(lastYear, 12, 31);
        
                // Retrieve borrow and return trends for current and last year
                var borrowCurrentYear = await GetTrendDataForYear(
                    startDate: startCurrentYear,
                    endDate: endCurrentYear,
                    lang: lang);
                var borrowLastYear = await GetTrendDataForYear(
                    startDate: startLastYear,
                    endDate: endLastYear,
                    lang: lang);
                
                // Assign borrow and return trends
                dashboardCirculation.BorrowTrends = borrowLastYear;
                dashboardCirculation.ReturnTrends = borrowCurrentYear;
                
            }
            
            return new ServiceResult(
                resultCode: ResultCodeConst.SYS_Success0002,
                message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                data: dashboardCirculation);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error processing dashboard circulation and borrowing analytics");
        }
    }

    public async Task<IServiceResult> GetDashboardDigitalResourceAnalyticsAsync(
        LibraryResourceSpecification spec,
        DateTime? startDate, DateTime? endDate, TrendPeriod period)
    {
        try
        {
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Calculate valid date range based on period
            (DateTime validStartDate, DateTime validEndDate) = GetValidDateRange(startDate, endDate, period, currentLocalDateTime);
            
            // Initialize dashboard digital resource
            var dashboardDigitalResource = new DashboardDigitalResourceDto();
            
            // Count total digital resources
            var countResourceSpec = new BaseSpecification<LibraryResource>();
            // Add filter date range
            if (startDate != null || endDate != null)
            {
                countResourceSpec.AddFilter(db => db.CreatedAt.Date >= validStartDate.Date && db.CreatedAt.Date <= validEndDate.Date);
            }
            if ((await _resourceSvc.CountAsync(countResourceSpec)).Data is int totalResourceRes)
            {
                dashboardDigitalResource.TotalDigitalResource = totalResourceRes;
            }
            
            // Count total active digital borrowings
            var activeBorrowSpec = new BaseSpecification<DigitalBorrow>(db => db.Status == BorrowDigitalStatus.Active);
            // Add filter date range
            if (startDate != null || endDate != null)
            {
                activeBorrowSpec.AddFilter(db => db.RegisterDate.Date >= validStartDate.Date && db.RegisterDate.Date <= validEndDate.Date);
            }
            if ((await _digitalBorrowSvc.CountAsync(activeBorrowSpec)).Data is int totalActiveBorrow)
            {
                dashboardDigitalResource.TotalActiveDigitalBorrowing = totalActiveBorrow;
            }
            
            // Initialize calculation fields
            var totalBorrows = 0;
            var borrowsWithExtension = 0;
            var totalExtensions = 0;
            
            // Count total existing digital borrow
            var totalSpec = new BaseSpecification<DigitalBorrow>();
            // Add filter date range
            if (startDate != null || endDate != null)
            {
                totalSpec.AddFilter(db => db.RegisterDate.Date >= validStartDate.Date && db.RegisterDate.Date <= validEndDate.Date);
            }
            // Count with spec
            if ((await _digitalBorrowSvc.CountAsync(totalSpec)).Data is int totalBorrowRes)
            {
                totalBorrows = totalBorrowRes;
            }
            
            // Count borrows with extension
            var totalWithExtensionSpec = new BaseSpecification<DigitalBorrow>(db => db.ExtensionCount > 0);
            // Add filter date range
            if (startDate != null || endDate != null)
            {
                totalWithExtensionSpec.AddFilter(db => db.RegisterDate.Date >= validStartDate.Date && db.RegisterDate.Date <= validEndDate.Date);
            }
            // Count with spec
            if ((await _digitalBorrowSvc.CountAsync(totalWithExtensionSpec)).Data is int borrowsWithExtensionRes)
            {
                borrowsWithExtension = borrowsWithExtensionRes;
            }
            
            // Count total extensions
            var sumSpec = new BaseSpecification<DigitalBorrowExtensionHistory>();
            // Add filter date range
            if (startDate != null || endDate != null)
            {
                sumSpec.AddFilter(db => db.DigitalBorrow.RegisterDate.Date >= validStartDate.Date &&
                                        db.DigitalBorrow.RegisterDate.Date <= validEndDate.Date);
            }
            // Apply include
            sumSpec.ApplyInclude(q => q.Include(d => d.DigitalBorrow));
            // Retrieve all digital extension his
            var digitalExtensionHistories = await _digitalBorrowExtensionSvc.GetAllWithSpecAsync(sumSpec);
            if (digitalExtensionHistories.Data is List<DigitalBorrowExtensionHistoryDto> histories)
            {
                // Map to entity
                var hisEntities = _mapper.Map<List<DigitalBorrowExtensionHistory>>(histories);
                totalExtensions = hisEntities.Distinct(new DigitalBorrowExtensionHistoryComparer()).Count();
            }
            
            // Calculate extensions rate
            if (totalBorrows > 0)
            {
                dashboardDigitalResource.ExtensionRatePercentage = (double)borrowsWithExtension / totalBorrows * 100;
            }
            else
            {
                dashboardDigitalResource.ExtensionRatePercentage = 0;
            }

            if (borrowsWithExtension > 0)
            {
                dashboardDigitalResource.AverageExtensionsPerBorrow = (double)totalExtensions / borrowsWithExtension;
            }
            else
            {
                dashboardDigitalResource.AverageExtensionsPerBorrow = 0;
            }

            // Add filter
            spec.AddFilter(r => r.DigitalBorrows.Any());
            
            // Add filter date range
            if (startDate != null || endDate != null)
            {
                spec.AddFilter(db => db.CreatedAt.Date >= validStartDate.Date && db.CreatedAt.Date <= validEndDate.Date);
            }
            
            // Retrieve top borrowing resources
            // Add order by total of digital borrow history
            spec.AddOrderByDescending(r => r.DigitalBorrows.Count);
            
            // Count total resource
            var totalResourceWithSpec = 0;
            var countRes = (await _resourceSvc.CountAsync(spec)).Data;
            if (countRes is int totalCountRes) {totalResourceWithSpec = totalCountRes;}
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalResourceWithSpec / spec.PageSize);
            
            // Set pagination to specification after count total resource 
            if (spec.PageIndex > totalPage
                || spec.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
                spec.PageIndex = 1; // Set default to first page
            }
            
            // Apply pagination
            spec.ApplyPaging(skip: spec.PageSize * (spec.PageIndex - 1), take: spec.PageSize);
            
            // Initialize resp collection
            var topBorrowResources = new List<GetTopBorrowResourceDto>();
            // Get all with spec
            var entities = (await _resourceSvc.GetAllWithSpecFromDashboardAsync(spec)).Data as List<LibraryResourceDto>;
            if (entities != null && entities.Any())
            {
                // Iterate each resource to build up data
                foreach (var resource in entities)
                {
                    // Initialize top digital resource response 
                    var dto = new GetTopBorrowResourceDto();
                    
                    // Count total extensions
                    sumSpec = new BaseSpecification<DigitalBorrowExtensionHistory>(d => d.DigitalBorrow.ResourceId == resource.ResourceId);
                    // Add filter date range
                    if (startDate != null || endDate != null)
                    {
                        sumSpec.AddFilter(db => db.DigitalBorrow.RegisterDate.Date >= validStartDate.Date &&
                                                db.DigitalBorrow.RegisterDate.Date <= validEndDate.Date);
                    }
                    // Apply include
                    sumSpec.ApplyInclude(q => q.Include(d => d.DigitalBorrow));
                    // Retrieve all digital extension his
                    digitalExtensionHistories = await _digitalBorrowExtensionSvc.GetAllWithSpecAsync(sumSpec);
                    if (digitalExtensionHistories.Data is List<DigitalBorrowExtensionHistoryDto> extendHistories)
                    {
                        // Map to entity
                        var hisEntities = _mapper.Map<List<DigitalBorrowExtensionHistory>>(extendHistories);
                        totalExtensions = hisEntities.Distinct(new DigitalBorrowExtensionHistoryComparer()).Count();
                    }
                    else
                    {
                        totalExtensions = 0;
                    }
                    
                    // Retrieve all digital borrows by resource id
                    var digitalBrSpec = new BaseSpecification<DigitalBorrow>(db => db.ResourceId == resource.ResourceId);
                    // Add filter date rang
                    if (startDate != null || endDate != null)
                    {
                        digitalBrSpec.AddFilter(db => db.RegisterDate.Date >= validStartDate.Date && db.RegisterDate.Date <= validEndDate.Date);
                    }
                    
                    // Retrieve all with spec
                    var digitalBorrows = (await _digitalBorrowSvc.GetAllWithSpecFromDashboardAsync(digitalBrSpec)).Data as List<DigitalBorrowDto>;
                    if (digitalBorrows != null && digitalBorrows.Any())
                    {
                        dto.TotalBorrowed = digitalBorrows.Count;
                        dto.TotalExtension = digitalBorrows.Sum(db => db.ExtensionCount);
                        dto.AverageBorrowDuration = digitalBorrows.Average(b => (b.ExpiryDate - b.RegisterDate).TotalDays);
                        dto.ExtensionRate = dto.TotalBorrowed > 0 
                            ? (double)totalExtensions / dto.TotalBorrowed * 100 
                            : 0; 
                        dto.LastBorrowDate = digitalBorrows.Any()
                            ? digitalBorrows.OrderByDescending(db => db.RegisterDate).First().RegisterDate
                            : null;

                        // Round average borrow duration
                        dto.AverageBorrowDuration = Math.Ceiling(dto.AverageBorrowDuration);
                        // Format double value 
                        dto.AverageBorrowDuration = Math.Truncate(dto.AverageBorrowDuration * 100) / 100;
                        dto.ExtensionRate = Math.Truncate(dto.ExtensionRate * 100) / 100;
                    }
                    
                    // Assign library resource
                    dto.LibraryResource = resource;
                    // Add to top resource borrow list 
                    topBorrowResources.Add(dto);
                }
            }
        
            // Pagination result 
            dashboardDigitalResource.TopBorrowLibraryResources = new PaginatedResultDto<GetTopBorrowResourceDto>(
                topBorrowResources,
                spec.PageIndex, spec.PageSize, totalPage, totalResourceWithSpec);
            
            // Format double value 
            dashboardDigitalResource.ExtensionRatePercentage = Math.Truncate(dashboardDigitalResource.ExtensionRatePercentage * 100) / 100;
            dashboardDigitalResource.AverageExtensionsPerBorrow = Math.Truncate(dashboardDigitalResource.AverageExtensionsPerBorrow * 100) / 100;
            
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), dashboardDigitalResource);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when get dashboard digital resource analytics");
        }
    }

    public async Task<IServiceResult> GetDashboardFinancialAndTransactionAnalyticsAsync(
        TransactionSpecification spec,
        DateTime? startDate, DateTime? endDate,
        TrendPeriod period,
        TransactionType? transactionType = null)
    {
        try
        {
            // Determine current system lang 
            var lang = (SystemLanguage?) EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage) ?? SystemLanguage.English;
            
            // Current local date time
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Force not existing period
            if (period != TrendPeriod.Weekly && period != TrendPeriod.Monthly)
            {
                // Set default
                period = TrendPeriod.Monthly;
            }
            
            // Calculate valid date range based on period
            (DateTime validStartDate, DateTime validEndDate) = GetValidDateRange(startDate, endDate, period, currentLocalDateTime);
            
            // Check whether request has custom date range
            bool hasCustom = startDate.HasValue || endDate.HasValue;
            
            int startMonth = hasCustom ? validStartDate.Month : 1;
            int startDay = hasCustom ? validStartDate.Day : 1;
            int endMonth = hasCustom ? validEndDate.Month : 12;
            int endDay = hasCustom ? validEndDate.Day : 31;
            
            // Retrieve last year and this year
            var currentYear = currentLocalDateTime.Year;
            var lastYear = currentYear - 1;
            
            // Define date ranges for the current year and last year
            var startCurrentYear = new DateTime(currentYear, startMonth, startDay);
            var endCurrentYear = new DateTime(currentYear, endMonth, endDay);
            var startLastYear = new DateTime(lastYear, startMonth, startDay);
            var endLastYear = new DateTime(lastYear, endMonth, endDay);
            
            // Initialize dashboard financial and transaction detail list
            var details = new List<DashboardFinancialAndTransactionDetailDto>();
            // Retrieve all transaction status
            var transactionStatuses = Enum.GetValues(typeof(TransactionStatus)).Cast<TransactionStatus>().ToList();
            // Retrieve all transaction type
            var transactionTypes = Enum.GetValues(typeof(TransactionType)).Cast<TransactionType>().ToList();
            // Iterate each transaction type to retrieve for transaction detail
            foreach (var tType in transactionTypes)
            {
                // Initialize detail
                var detail = new DashboardFinancialAndTransactionDetailDto();
                
                // Retrieve data for current year (optional filter with transaction type)
                var transactionSpecCurrent = new BaseSpecification<Transaction>(t => 
                    t.TransactionDate.HasValue && 
                    t.TransactionDate.Value >= startCurrentYear &&
                    t.TransactionDate.Value <= endCurrentYear &&
                    t.TransactionStatus == TransactionStatus.Paid &&
                    t.TransactionType == tType);
                // Add filter date range (if any)
                if (startDate != null || endDate != null)
                {
                    transactionSpecCurrent.AddFilter(db => db.TransactionDate.HasValue &&
                        db.TransactionDate.Value.Date >= validStartDate.Date &&
                        db.TransactionDate.Value.Date <= validEndDate.Date);
                }
                var transactionsCurrent = (await _transSvc.GetAllWithSpecAndSelectorAsync(
                    specification: transactionSpecCurrent,
                    selector: t => new ValueTuple<DateTime?, decimal>(t.TransactionDate, t.Amount))).Data as List<(DateTime?, decimal)>;
                
                // Retrieve transaction data for last year (optional filter with transaction type)
                var transactionSpecLast = new BaseSpecification<Transaction>(t =>
                    t.TransactionDate.HasValue &&
                    t.TransactionDate.Value >= startLastYear &&
                    t.TransactionDate.Value <= endLastYear &&
                    t.TransactionStatus == TransactionStatus.Paid &&
                    t.TransactionType == tType);
                // Add filter date range (if any)
                if (startDate != null || endDate != null)
                {
                    transactionSpecLast.AddFilter(db => db.TransactionDate.HasValue &&
                        db.TransactionDate.Value.Date >= validStartDate.Date &&
                        db.TransactionDate.Value.Date <= validEndDate.Date);
                }
                var transactionsLast = (await _transSvc.GetAllWithSpecAndSelectorAsync(
                    specification: transactionSpecLast,
                    selector: t => new ValueTuple<DateTime?, decimal>(t.TransactionDate, t.Amount))).Data as List<(DateTime?, Decimal)>;
                
                // Process data to generate bar chart data (full timeline)
                var trendCurrent = GetTransactionTrendData(
                    transactions: transactionsCurrent,
                    startDate: startCurrentYear, endDate: endCurrentYear,
                    period: period, lang: lang);
                var trendLast = GetTransactionTrendData(
                    transactions: transactionsLast,
                    startDate: startCurrentYear, endDate: endCurrentYear,
                    period: period, lang: lang);
                
                // Calculate overall revenue for each period
                var cateTotalRevenueCurr = trendCurrent?.Sum(t => (decimal)t.Value) ?? 0;
                var cateTotalRevenueLast = trendLast?.Sum(t => (decimal)t.Value) ?? 0;
                
                // Count all by transaction type 
                var totalTransactions = 0;
                var countTotalTransactionRes = (await _transSvc.CountAsync(new BaseSpecification<Transaction>(t => 
                    t.TransactionDate.HasValue &&
                    t.TransactionDate.Value >= validStartDate &&
                    t.TransactionDate.Value <= validEndDate &&
                    t.TransactionType == tType))).Data;
                if(countTotalTransactionRes is int validTotalTransactions) { totalTransactions = validTotalTransactions; } 
                
                // Iterate each transaction status
                foreach (var transactionStatus in transactionStatuses)
                {
                    // Build spec
                    var baseSpec = new BaseSpecification<Transaction>(t =>  
                        t.TransactionDate.HasValue && 
                        t.TransactionDate.Value >= validStartDate && 
                        t.TransactionDate.Value <= validEndDate && 
                        t.TransactionType == tType && 
                        t.TransactionStatus == transactionStatus
                    );
                    
                    // Process count
                    var countTransactionStatusRes = (await _transSvc.CountAsync(baseSpec)).Data;
                    // Parse to integer
                    var validInt = countTransactionStatusRes is int validCount ? validCount : 0;

                    if (totalTransactions > 0)
                    {
                        // Determine transaction status
                        switch (transactionStatus)
                        {
                            case TransactionStatus.Pending:
                                // Calculate pending percentage
                                detail.PendingPercentage = (double)validInt / totalTransactions * 100;
                                // Format double value
                                detail.PendingPercentage = Math.Truncate(detail.PendingPercentage * 100) / 100;
                                break;
                            case TransactionStatus.Paid:
                                // Calculate paid percentage
                                detail.PaidPercentage = (double)validInt / totalTransactions * 100;
                                // Format double value
                                detail.PaidPercentage = Math.Truncate(detail.PaidPercentage * 100) / 100;
                                break;
                            case TransactionStatus.Expired:
                                // Calculate expired percentage
                                detail.ExpiredPercentage = (double)validInt / totalTransactions * 100;
                                // Format double value
                                detail.ExpiredPercentage = Math.Truncate(detail.ExpiredPercentage * 100) / 100;
                                break;
                            case TransactionStatus.Cancelled:
                                // Calculate cancelled percentage
                                detail.CancelledPercentage = (double)validInt / totalTransactions * 100;
                                // Format double value
                                detail.CancelledPercentage = Math.Truncate(detail.CancelledPercentage * 100) / 100;
                                break;
                        }
                    }
                }
                
                // Assign detail
                detail.TransactionType = tType;
                detail.ThisYear = trendCurrent ?? new();
                detail.LastYear = trendLast ?? new();
                detail.TotalRevenueThisYear = cateTotalRevenueCurr;
                detail.TotalRevenueLastYear = cateTotalRevenueLast;
                // Append detail
                details.Add(detail);
            }

            // Aggregate trend current with trend last year
            var aggregatedTrendCurrent = details.SelectMany(d => d.ThisYear).ToList();
            var aggregatedLastCurrent = details.SelectMany(d => d.LastYear).ToList();
            
            // Calculate overall revenue for each period
            var totalRevenueCurrent = aggregatedTrendCurrent?.Sum(t => (decimal)t.Value) ?? 0;
            var totalRevenueLast = aggregatedLastCurrent?.Sum(t => (decimal)t.Value) ?? 0;
            
            // Add filter date range
            if (startDate != null || endDate != null)
            {
                spec.AddFilter(db => db.TransactionDate.HasValue &&
                                     db.TransactionDate.Value.Date >= validStartDate.Date &&
                                     db.TransactionDate.Value.Date <= validEndDate.Date);
            }
            // Retrieve current transactions
            spec.AddOrderByDescending(t => t.TransactionDate ?? t.CreatedAt);
            // Add filter 
            spec.AddFilter(t => t.TransactionStatus == TransactionStatus.Paid); // Must be paid
            // Apply include
            spec.ApplyInclude(q => q.Include(t => t.User));
            // Count total transaction
            var totalTransactionWithSpec = 0;
            var countRes = (await _transSvc.CountAsync(spec)).Data;
            if (countRes is int totalCountRes) {totalTransactionWithSpec = totalCountRes;}
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalTransactionWithSpec / spec.PageSize);
            
            // Set pagination to specification after count total resource 
            if (spec.PageIndex > totalPage
                || spec.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
                spec.PageIndex = 1; // Set default to first page
            }
            
            // Apply pagination
            spec.ApplyPaging(skip: spec.PageSize * (spec.PageIndex - 1), take: spec.PageSize);
            
            // Get all with spec
            var entities = (await _transSvc.GetAllWithSpecFromDashboardAsync(spec)).Data as List<TransactionDto>;
            
            // Convert to get transaction dto collection
            var getTransactionList = entities != null && entities.Any()
                ? entities.Select(e => e.ToGetTransactionDto()).ToList()
                : new();
            
            // Pagination result 
            var paginationRes = new PaginatedResultDto<GetTransactionDto>(
                getTransactionList,
                spec.PageIndex, spec.PageSize, totalPage, totalTransactionWithSpec);
            
            // Response
            return new ServiceResult(
                resultCode: ResultCodeConst.SYS_Success0002,
                message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                data: new DashboardFinancialAndTransactionDto()
                {
                    Details = details,
                    TotalRevenueThisYear = totalRevenueCurrent,
                    TotalRevenueLastYear = totalRevenueLast,
                    LatestTransactions = paginationRes
                });
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get dashboard financial and transaction");
        }
    }

    public async Task<IServiceResult> GetAllOverdueBorrowAsync(BorrowRecordDetailSpecification spec,
        DateTime? startDate, DateTime? endDate, TrendPeriod period)
    {
        try
        {
            // Current local date time
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Calculate valid date range based on period
            (DateTime validStartDate, DateTime validEndDate) = GetValidDateRange(startDate, endDate, period, currentLocalDateTime);
            
            // Add spec filter
            spec.AddFilter(brd => brd.Status == BorrowRecordStatus.Overdue);
            // Add date range filter
            if (startDate != null || endDate != null)
            {
                spec.AddFilter(brd => brd.DueDate.Date >= validStartDate.Date && 
                                      brd.DueDate.Date <= validEndDate.Date);
            }
            // Add order
            spec.AddOrderByDescending(d => d.DueDate);
            // Apply include
            spec.ApplyInclude(q => q
                .Include(b => b.LibraryItemInstance)
                    .ThenInclude(b => b.LibraryItem)
                        .ThenInclude(li => li.LibraryItemAuthors)
                            .ThenInclude(li => li.Author)
                .Include(b => b.BorrowRecord)
                    .ThenInclude(b => b.LibraryCard)
            );
            
            // Count total transaction
            var totalOverdueWithSpec = 0;
            var countRes = (await _borrowRecDetailSvc.CountAsync(spec)).Data;
            if (countRes is int totalCountRes) {totalOverdueWithSpec = totalCountRes;}
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalOverdueWithSpec / spec.PageSize);
            
            // Set pagination to specification after count total resource 
            if (spec.PageIndex > totalPage
                || spec.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
                spec.PageIndex = 1; // Set default to first page
            }
            
            // Apply pagination
            spec.ApplyPaging(skip: spec.PageSize * (spec.PageIndex - 1), take: spec.PageSize);
            
            // Get all with spec
            var entities = (await _borrowRecDetailSvc.GetAllWithSpecFromDashboardAsync(spec)).Data as List<BorrowRecordDetailDto>;
            if (entities != null && entities.Any())
            {
                // Convert to dto
                var dtoList = _mapper.Map<List<BorrowRecordDetailDto>>(entities);
                
                // Convert to dashboard record detail list
                var responseList = dtoList.Select(r => 
                    r.ToDashboardBorrowRecordDetailDto(r.BorrowRecord.LibraryCard, r.LibraryItemInstance.LibraryItem)).ToList();
                
                // Pagination result 
                var paginationRes = new PaginatedResultDto<DashboardBorrowRecordDetailDto>(
                    responseList, spec.PageIndex, spec.PageSize, totalPage, totalOverdueWithSpec);
                
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Success0002,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                    data: paginationRes);
            }
            
            return new ServiceResult(
                resultCode: ResultCodeConst.SYS_Warning0004,
                message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                data: new List<BorrowRecordDetailDto>());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get pending overdue borrow");
        }
    }

    public async Task<IServiceResult> GetLatestBorrowAsync(BorrowRecordDetailSpecification spec,
        DateTime? startDate, DateTime? endDate, TrendPeriod period)
    {
        // Current local date time
        var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        
        // Calculate valid date range based on period
        (DateTime validStartDate, DateTime validEndDate) = GetValidDateRange(startDate, endDate, period, currentLocalDateTime);
        
        // Add spec filter
        spec.AddFilter(brd => brd.Status == BorrowRecordStatus.Borrowing);
        // Add date range filter
        if(startDate != null || endDate != null)
        {
            spec.AddFilter(brd => brd.BorrowRecord.BorrowDate.Date >= validStartDate.Date && 
                                          brd.BorrowRecord.BorrowDate.Date <= validEndDate.Date);
        }
        // Add order
        spec.AddOrderByDescending(d => d.DueDate);
        // Apply include
        spec.ApplyInclude(q => q
            .Include(b => b.LibraryItemInstance)
                .ThenInclude(b => b.LibraryItem)
                    .ThenInclude(li => li.LibraryItemAuthors)
                        .ThenInclude(li => li.Author)
            .Include(b => b.BorrowRecord)
                .ThenInclude(b => b.LibraryCard)
        );
        
        // Count total transaction
        var totalBorrowingWithSpec = 0;
        var countRes = (await _borrowRecDetailSvc.CountAsync(spec)).Data;
        if (countRes is int totalCountRes) {totalBorrowingWithSpec = totalCountRes;}
        // Count total page
        var totalPage = (int)Math.Ceiling((double)totalBorrowingWithSpec / spec.PageSize);
        
        // Set pagination to specification after count total resource 
        if (spec.PageIndex > totalPage
            || spec.PageIndex < 1) // Exceed total page or page index smaller than 1
        {
            spec.PageIndex = 1; // Set default to first page
        }
        
        // Apply pagination
        spec.ApplyPaging(skip: spec.PageSize * (spec.PageIndex - 1), take: spec.PageSize);
        
        // Get all with spec
        var entities = (await _borrowRecDetailSvc.GetAllWithSpecFromDashboardAsync(spec)).Data as List<BorrowRecordDetailDto>;
        if (entities != null && entities.Any())
        {
            // Convert to dto
            var dtoList = _mapper.Map<List<BorrowRecordDetailDto>>(entities);
                
            // Convert to dashboard record detail list
            var responseList = dtoList.Select(r => 
                r.ToDashboardBorrowRecordDetailDto(r.BorrowRecord.LibraryCard, r.LibraryItemInstance.LibraryItem)).ToList();
                
            // Pagination result 
            var paginationRes = new PaginatedResultDto<DashboardBorrowRecordDetailDto>(
                responseList, spec.PageIndex, spec.PageSize, totalPage, totalBorrowingWithSpec);
                
            return new ServiceResult(
                resultCode: ResultCodeConst.SYS_Success0002,
                message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                data: paginationRes);
        }
        
        return new ServiceResult(
            resultCode: ResultCodeConst.SYS_Warning0004,
            message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
            data: new List<BorrowRecordDetailDto>());
    }

    public async Task<IServiceResult> GetAssignableReservationAsync(ReservationQueueSpecification spec,
        DateTime? startDate, DateTime? endDate, TrendPeriod period)
    {
        // Current local date time
        var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        
        // Calculate valid date range based on period
        (DateTime validStartDate, DateTime validEndDate) = GetValidDateRange(startDate, endDate, period, currentLocalDateTime);
        
        return await _reservationQueueSvc.GetAllAssignableForDashboardAsync(
            startDate: startDate ?? validStartDate,
            endDate: endDate ?? validEndDate,
            period: period,
            pageIndex: spec.PageIndex,
            pageSize: spec.PageSize);
    }

    public async Task<IServiceResult> GetTopCirculationItemsAsync(TopCirculationItemSpecification spec,
        DateTime? startDate, DateTime? endDate, TrendPeriod period)
    {
        try
        {
            // Determine current system lang 
            var lang = (SystemLanguage?) EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage) ?? SystemLanguage.English;
            
            // Current local date time
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Calculate valid date range based on period
            (DateTime validStartDate, DateTime validEndDate) = GetValidDateRange(startDate, endDate, period, currentLocalDateTime);
            
            // Retrieve all library items with spec
            var getItemRes = await _itemSvc.GetAllWithSpecAndSelectorAsync(spec, be => new LibraryItem()
                {
                    LibraryItemId = be.LibraryItemId,
                    Title = be.Title,
                    SubTitle = be.SubTitle,
                    Responsibility = be.Responsibility,
                    Edition = be.Edition,
                    EditionNumber = be.EditionNumber,
                    Language = be.Language,
                    OriginLanguage = be.OriginLanguage,
                    Summary = be.Summary,
                    CoverImage = be.CoverImage,
                    PublicationYear = be.PublicationYear,
                    Publisher = be.Publisher,
                    PublicationPlace = be.PublicationPlace,
                    ClassificationNumber = be.ClassificationNumber,
                    CutterNumber = be.CutterNumber,
                    Isbn = be.Isbn,
                    Ean = be.Ean,
                    EstimatedPrice = be.EstimatedPrice,
                    PageCount = be.PageCount,
                    PhysicalDetails = be.PhysicalDetails,
                    Dimensions = be.Dimensions,
                    AccompanyingMaterial = be.AccompanyingMaterial,
                    Genres = be.Genres,
                    GeneralNote = be.GeneralNote,
                    BibliographicalNote = be.BibliographicalNote,
                    TopicalTerms = be.TopicalTerms,
                    AdditionalAuthors = be.AdditionalAuthors,
                    CategoryId = be.CategoryId,
                    ShelfId = be.ShelfId,
                    GroupId = be.GroupId,
                    Status = be.Status,
                    IsDeleted = be.IsDeleted,
                    IsTrained = be.IsTrained,
                    CanBorrow = be.CanBorrow,
                    TrainedAt = be.TrainedAt,
                    CreatedAt = be.CreatedAt,
                    UpdatedAt = be.UpdatedAt,
                    UpdatedBy = be.UpdatedBy,
                    CreatedBy = be.CreatedBy,
                    // References
                    Category = be.Category,
                    Shelf = be.Shelf,
                    LibraryItemInventory = be.LibraryItemInventory,
                    LibraryItemReviews = be.LibraryItemReviews,
                    LibraryItemAuthors = be.LibraryItemAuthors.Select(ba => new LibraryItemAuthor()
                    {
                        LibraryItemAuthorId = ba.LibraryItemAuthorId,
                        LibraryItemId = ba.LibraryItemId,
                        AuthorId = ba.AuthorId,
                        Author = ba.Author
                    }).ToList()
                });
            if (getItemRes.Data != null && getItemRes.Data is List<LibraryItem> itemEntities && itemEntities.Any())
            {
                // Convert to dto collection
                var itemList = _mapper.Map<List<LibraryItemDto>>(itemEntities);
                // Initialize dashboard response
                var response = new DashboardTopCirculationItemDto();
                // Initialize top circulation items
                var topCirculationItemList = new List<GetTopCirculationItemDto>();
                // Iterate each item to calculate and filter circulation
                foreach (var item in itemList)
                {
                    // Initialize service response
                    IServiceResult svcResponse;
                    
                    // Initialize get top circulation item
                    var topCirculationItem = new GetTopCirculationItemDto();
                    
                    // Calculate borrow count
                    var countBorrowSpec = new BaseSpecification<BorrowRecordDetail>(brd => 
                        brd.LibraryItemInstance.LibraryItemId == item.LibraryItemId &&
                        brd.Status == BorrowRecordStatus.Borrowing); // Is borrowing
                    // Add filter date range
                    if (startDate != null || endDate != null)
                    {
                        countBorrowSpec.AddFilter(brd => brd.BorrowRecord.BorrowDate.Date >= validStartDate.Date &&
                                                         brd.BorrowRecord.BorrowDate.Date <= validEndDate.Date);
                    }
                    // Count with spec
                    svcResponse = await _borrowRecDetailSvc.CountAsync(countBorrowSpec);
                    // Convert data of response to integer
                    topCirculationItem.BorrowSuccessCount = svcResponse.Data != null ? Convert.ToInt32(svcResponse.Data) : 0;
                    
                    // Calculate borrow failed count
                    var countBorrowFailedSpec = new BaseSpecification<ReservationQueue>(r => 
                        r.LibraryItemId == item.LibraryItemId && 
                        r.IsReservedAfterRequestFailed == true && // Request failed
                        r.QueueStatus == ReservationQueueStatus.Pending); // Is pending
                    // Filter reservation date
                    if (startDate != null || endDate != null)
                    {
                        countBorrowFailedSpec.AddFilter(r => r.ReservationDate.Date >= validStartDate.Date && 
                                                             r.ReservationDate.Date <= validEndDate.Date);
                    }
                    // Count with spec
                    svcResponse = await _reservationQueueSvc.CountAsync(countBorrowFailedSpec);
                    // Convert data of response to integer
                    topCirculationItem.BorrowFailedCount = svcResponse.Data != null ? Convert.ToInt32(svcResponse.Data) : 0;
                    
                    // Calculate request count
                    var countBorrowReqSpec = new BaseSpecification<BorrowRequestDetail>(r => 
                        r.LibraryItemId == item.LibraryItemId && 
                        r.BorrowRequest.Status == BorrowRequestStatus.Created); // Is pending
                    // Filter reservation date
                    if (startDate != null || endDate != null)
                    {
                        countBorrowReqSpec.AddFilter(r => r.BorrowRequest.RequestDate.Date >= validStartDate.Date && 
                                                          r.BorrowRequest.RequestDate.Date <= validEndDate.Date);
                    }
                    // Count with spec
                    svcResponse = await _borrowReqDetailSvc.CountAsync(countBorrowReqSpec);
                    // Convert data of response to integer
                    topCirculationItem.BorrowRequestCount = svcResponse.Data != null ? Convert.ToInt32(svcResponse.Data) : 0;
                    
                    // Calculate satisfaction units (inShelf + outOfShelf)
                    var countSatisfactionSpec = new BaseSpecification<LibraryItemInstance>(li =>
                        li.LibraryItemId == item.LibraryItemId &&
                        (
                            li.Status == nameof(LibraryItemInstanceStatus.InShelf) ||
                            li.Status == nameof(LibraryItemInstanceStatus.OutOfShelf)
                        )
                    );
                    // Filter reservation date
                    if (startDate != null || endDate != null)
                    {
                        countSatisfactionSpec.AddFilter(r => r.CreatedAt.Date >= validStartDate.Date && 
                                                             r.CreatedAt.Date <= validEndDate.Date);
                    }
                    // Count with spec
                    svcResponse = await _itemInstanceSvc.CountAsync(countSatisfactionSpec);
                    // Convert data of response to integer
                    topCirculationItem.TotalSatisfactionUnits = svcResponse.Data != null ? Convert.ToInt32(svcResponse.Data) : 0;
                    
                    // Calculate satisfaction rate
                    var needUnits = topCirculationItem.BorrowRequestCount + topCirculationItem.BorrowFailedCount;
                    topCirculationItem.SatisfactionRate = needUnits > 0 
                        ? (double)topCirculationItem.TotalSatisfactionUnits / needUnits * 100 
                        : 100;
                    // Format double value
                    var truncatedRate = Math.Truncate(topCirculationItem.SatisfactionRate * 100) / 100;
                    // Set default if exceed 100% 
                    topCirculationItem.SatisfactionRate = Math.Min(truncatedRate, 100);
                    
                    // Initialize fields to calculate extension rate
                    var totalExtendedBorrow = 0;
                    var totalBorrowed = 0;
                    // Calculate borrows that have been extended
                    var countExtendedBorrowSpec = new BaseSpecification<BorrowRecordDetail>(br =>
                        br.LibraryItemInstance.LibraryItemId == item.LibraryItemId &&
                        br.BorrowDetailExtensionHistories.Any());
                    // Add filter date range
                    if (startDate != null || endDate != null)
                    {
                        countBorrowSpec.AddFilter(brd => brd.BorrowRecord.BorrowDate.Date >= validStartDate.Date &&
                                                        brd.BorrowRecord.BorrowDate.Date <= validEndDate.Date);
                    }
                    // Count with spec
                    svcResponse = await _borrowRecDetailSvc.CountAsync(countExtendedBorrowSpec);
                    // Convert data of response to integer
                    totalExtendedBorrow = svcResponse.Data != null ? Convert.ToInt32(svcResponse.Data) : 0;
                    
                    // Calculate borrowed
                    var borrowedSpec = new BaseSpecification<BorrowRecordDetail>(br =>
                        br.LibraryItemInstance.LibraryItemId == item.LibraryItemId);
                    // Add filter date range
                    if (startDate != null || endDate != null)
                    {
                        borrowedSpec.AddFilter(brd => brd.BorrowRecord.BorrowDate.Date >= validStartDate.Date &&
                                                      brd.BorrowRecord.BorrowDate.Date <= validEndDate.Date);
                    }
                    // Count with spec
                    svcResponse = await _borrowRecDetailSvc.CountAsync(borrowedSpec);
                    // Convert data of response to integer
                    totalBorrowed = svcResponse.Data != null ? Convert.ToInt32(svcResponse.Data) : 0;
                    
                    // Calculate extension rate
                    if (totalBorrowed == 0)
                    {
                        topCirculationItem.BorrowExtensionRate = 0;
                    }
                    else
                    {
                        topCirculationItem.BorrowExtensionRate = (double) totalExtendedBorrow / totalBorrowed * 100;
                    }
                    
                    // Initialize available and need chart instance
                    var availableAndNeedChart = new AvailableVsNeedChartItemDto();
                    // Assign available and need units
                    availableAndNeedChart.AvailableUnits = topCirculationItem.TotalSatisfactionUnits;
                    availableAndNeedChart.NeedUnits = topCirculationItem.BorrowRequestCount + topCirculationItem.BorrowFailedCount;
                        
                    // // Calculate average need satisfaction rate
                    // if (availableAndNeedChart.NeedUnits > 0)
                    // {
                    //     // Calculate rate value and round with 2 digits
                    //     double rate = Math.Round((double)availableAndNeedChart.AvailableUnits / availableAndNeedChart.NeedUnits * 100, 2);
                    //     // Cap the rate at 100%
                    //     availableAndNeedChart.AverageNeedSatisfactionRate = rate > 100 ? 100 : rate;
                    // }
                    // else
                    // {
                    //     // Set default as 100
                    //     availableAndNeedChart.AverageNeedSatisfactionRate = 100;
                    // }
                     
                    // Assign chart data to circulation dto
                    topCirculationItem.AvailableVsNeedChart = availableAndNeedChart;
                    
                    // Retrieve borrow dates
                    var borrowSpec = new BaseSpecification<BorrowRecordDetail>(br =>
                              br.BorrowRecord.BorrowDate.Date >= validStartDate.Date &&
                              br.BorrowRecord.BorrowDate.Date <= validEndDate.Date &&
                              br.LibraryItemInstance.LibraryItemId == item.LibraryItemId);
                    var borrowDates = (await _borrowRecDetailSvc.GetAllWithSpecAndSelectorAsync(
                        specification: borrowSpec,
                        selector: s => s.BorrowRecord.BorrowDate)).Data as List<DateTime>;
                    var borrowTrends = GetTrendData(
                        dates: borrowDates,
                        startDate: validStartDate,
                        endDate: validEndDate,
                        period: period, lang: lang);
    
                    // Retrieve return dates
                    var reserveSpec = new BaseSpecification<ReservationQueue>(r =>
                        r.LibraryItemId == item.LibraryItemId &&
                        r.ReservationDate.Date >= validStartDate.Date && 
                        r.ReservationDate.Date <= validEndDate.Date);
                    var reservationDates = (await _reservationQueueSvc.GetAllWithSpecAndSelectorAsync(
                        specification: reserveSpec,
                        selector: s => s.ReservationDate)).Data as List<DateTime>;
                    var reservationTrends = GetTrendData(
                        dates: reservationDates,
                        startDate: validStartDate,
                        endDate: validEndDate,
                        period: period, lang: lang);
    
                    // Assign borrow and reservation trends
                    topCirculationItem.BorrowTrends = borrowTrends;
                    topCirculationItem.ReservationTrends = reservationTrends;
                    // Assign item
                    topCirculationItem.LibraryItem = item.ToLibraryItemDetailDto();      
                    // Add to list 
                    topCirculationItemList.Add(topCirculationItem);
                }
                
                // Retrieve all categories
                if ((await _cateSvc.GetAllAsync()).Data is List<CategoryDto> categories && categories.Any())
                {
                    // Iterate each category to process add barchart
                    foreach (var category in categories)
                    {
                        // Initialize available and need barchart
                        var availableAndBarchart = new AvailableVsNeedChartCategoryDto();

                        // Build count total request spec
                        var totalRequestSpec = new BaseSpecification<BorrowRequestDetail>(br => 
                            br.LibraryItem.CategoryId == category.CategoryId && 
                            br.BorrowRequest.Status == BorrowRequestStatus.Created); // Is pending
                        // Apply include
                        totalRequestSpec.ApplyInclude(q => q
                            .Include(b => b.LibraryItem)
                            .Include(b => b.BorrowRequest)
                        );
                        // Filter request date
                        if (startDate != null || endDate != null)
                        {
                            totalRequestSpec.AddFilter(r => r.BorrowRequest.RequestDate.Date >= validStartDate.Date && 
                                                            r.BorrowRequest.RequestDate.Date <= validEndDate.Date);
                        }
                        // Apply specification and count data
                        if ((await _borrowReqDetailSvc.CountAsync(totalRequestSpec)).Data is int validRequestNum)
                            availableAndBarchart.TotalRequest = validRequestNum;
                        
                        // Build count total reservation
                        var totalReserveSpec = new BaseSpecification<ReservationQueue>(r => 
                            r.LibraryItem.CategoryId == category.CategoryId &&
                            r.QueueStatus == ReservationQueueStatus.Pending); // Is pending
                        // Apply include
                        totalReserveSpec.ApplyInclude(q => q.Include(r => r.LibraryItemInstance!));
                        // Filter request date
                        if (startDate != null || endDate != null)
                        {
                            totalReserveSpec.AddFilter(r => r.ReservationDate.Date >= validStartDate.Date && 
                                                            r.ReservationDate.Date <= validEndDate.Date);
                        }
                        // Apply specification and count data
                        if ((await _reservationQueueSvc.CountAsync(totalReserveSpec)).Data is int validReservedNum)
                            availableAndBarchart.TotalReserved = validReservedNum;
                        
                        // Build count out of shelf instances
                        var totalOutOfShelfSpec = new BaseSpecification<LibraryItemInstance>(li => 
                            li.LibraryItem.CategoryId == category.CategoryId &&
                            li.Status == nameof(LibraryItemInstanceStatus.OutOfShelf));
                        // Filter date range
                        if (startDate != null || endDate != null)
                        {
                            totalOutOfShelfSpec.AddFilter(r => r.CreatedAt.Date >= validStartDate.Date && 
                                                               r.CreatedAt.Date <= validEndDate.Date);
                        }
                        // Apply specification and count data
                        if((await _itemInstanceSvc.CountAsync(totalOutOfShelfSpec)).Data is int validOutOfShelfNum)
                            availableAndBarchart.TotalOutOfShelf = validOutOfShelfNum;
                        
                        // Build count in shelf instances
                        var totalInShelfSpec = new BaseSpecification<LibraryItemInstance>(li => 
                            li.LibraryItem.CategoryId == category.CategoryId &&
                            li.Status == nameof(LibraryItemInstanceStatus.InShelf));
                        // Filter date range
                        if (startDate != null || endDate != null)
                        {
                            totalInShelfSpec.AddFilter(r => r.CreatedAt.Date >= validStartDate.Date && 
                                                            r.CreatedAt.Date <= validEndDate.Date);
                        }
                        // Apply specification and count data
                        if((await _itemInstanceSvc.CountAsync(totalInShelfSpec)).Data is int validInShelfNum)
                            availableAndBarchart.TotalInShelf = validInShelfNum;
                        
                        var availableUnits = availableAndBarchart.TotalInShelf + availableAndBarchart.TotalOutOfShelf;
                        var needUnits = availableAndBarchart.TotalReserved + availableAndBarchart.TotalRequest;
                        // Calculate satisfaction rate
                        availableAndBarchart.AverageNeedSatisfactionRate = needUnits > 0 
                            ? (double)availableUnits / needUnits * 100 
                            : 0;
                        // Format double value
                        var truncatedRate = Math.Truncate(availableAndBarchart.AverageNeedSatisfactionRate * 100) / 100;
                        // Set default if exceed 100% 
                        availableAndBarchart.AverageNeedSatisfactionRate = Math.Min(truncatedRate, 100);
                        
                        availableAndBarchart.AvailableUnits = availableUnits;
                        availableAndBarchart.NeedUnits = needUnits;
                        // Assign category
                        availableAndBarchart.Category = category;
                        // Append to response
                        response.AvailableVsNeedChartCategories.Add(availableAndBarchart);
                    }
                    
                    // Calculate total available and need units
                    var totalAvailableUnits = topCirculationItemList.Sum(i => i.AvailableVsNeedChart.AvailableUnits);
                    var totalNeedUnits = topCirculationItemList.Sum(i => i.AvailableVsNeedChart.NeedUnits);
                    
                    // Calculate total available need chart
                    response.AvailableVsNeedChartSummary = new()
                    {
                        AvailableUnits = totalAvailableUnits,
                        NeedUnits = totalNeedUnits,
                    };
                }
                
                // Apply sorting 
                if (spec.Sort != null)
                {
                    // Check is descending sorting 
                    var isDescending = spec.Sort.StartsWith("-");
                    if (isDescending)
                    {
                        spec.Sort = spec.Sort.Trim('-');
                    }

                    spec.Sort = spec.Sort.ToUpper();
            
                    // Define sorting pattern
                    var sortMappings = new Dictionary<string, Func<GetTopCirculationItemDto, object>>()
                    {
                        { "BORROWSUCCESSCOUNT", x => x.BorrowSuccessCount },
                        { "BORROWFAILEDCOUNT", x => x.BorrowFailedCount },
                        { "BORROWREQUESTCOUNT", x => x.BorrowRequestCount },
                        { "TOTALSATISFACTIONUNITS", x => x.TotalSatisfactionUnits },
                        { "SATISFACTIONRATE", x => x.SatisfactionRate },
                        { "BORROWEXTENSIONRATE", x => x.BorrowExtensionRate },
                    };
        
                    // Get sorting pattern
                    if (sortMappings.TryGetValue(spec.Sort.ToUpper(), 
                            out var sortExpression))
                    {
                        if(isDescending) topCirculationItemList = topCirculationItemList.OrderByDescending(sortExpression).ToList();
                        else topCirculationItemList = topCirculationItemList.OrderBy(sortExpression).ToList();    
                    }
                }
                else
                {
                    // Default sorting using multiple fields to improve data reliability
                    // First, order by the AverageNeedSatisfactionRate (lower means more need)
                    // Then, use various circulation metrics to further prioritize items
                    topCirculationItemList = topCirculationItemList
                        .OrderBy(x => x.SatisfactionRate)
                        .ThenByDescending(x => x.BorrowFailedCount)
                        .ThenByDescending(x => x.BorrowSuccessCount)
                        .ThenByDescending(x => x.BorrowRequestCount)
                        .ToList();
                }
                
                // Count total library items
                var totalActualItem = topCirculationItemList.Count;
                // Count total page
                var totalPage = (int)Math.Ceiling((double) totalActualItem / spec.PageSize);
                
                // Set pagination to specification after count total item
                if (spec.PageIndex > totalPage
                    || spec.PageIndex < 1) // Exceed total page or page index smaller than 1
                {
                    spec.PageIndex = 1; // Set default to first page
                }
                
                // Apply pagination
                topCirculationItemList = topCirculationItemList.Skip((spec.PageIndex - 1) * spec.PageSize).Take(spec.PageSize).ToList();
                
                // Pagination result
                var paginationRes = new PaginatedResultDto<GetTopCirculationItemDto>(topCirculationItemList,
                    spec.PageIndex, spec.PageSize, totalPage, totalActualItem);
                // Assign to response
                response.TopBorrowItems = paginationRes;
    
                // Get data successfully
                return new ServiceResult(
                    resultCode: ResultCodeConst.SYS_Success0002,
                    message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                    data: response);
            }
    
            // Data not found or empty
            return new ServiceResult(
                resultCode: ResultCodeConst.SYS_Warning0004,
                message: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                data: new List<GetTopCirculationItemDto>());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get top borrow items for dashboard");
        }
    }
    
    /// <summary>
    /// Determine valid start and end dates based on specific trend period
    /// </summary>
    private (DateTime start, DateTime end) GetValidDateRange(DateTime? startDate, DateTime? endDate, TrendPeriod period, DateTime currentLocal)
    {
        DateTime defaultStart = period switch
        {
            TrendPeriod.Daily => currentLocal.AddDays(-30),
            TrendPeriod.Weekly => currentLocal.AddDays(-90),
            TrendPeriod.Monthly => currentLocal.AddMonths(-12),
            _ => currentLocal.AddDays(-30)
        };

        DateTime validStart = startDate ?? defaultStart;
        DateTime validEnd = endDate ?? currentLocal;

        // If start date is after end date, swap them
        if (validStart > validEnd)
        {
            (validStart, validEnd) = (validEnd, validStart);
        }
        return (validStart, validEnd);
    }

    /// <summary>
    /// Build trend data (daily, weekly, monthly)
    /// </summary>
    private List<TrendDataDto> GetTrendData(
        List<DateTime>? dates,
        DateTime startDate,
        DateTime endDate,
        TrendPeriod period,
        SystemLanguage lang)
    {
        // Initialize dictionary for the grouping
        Dictionary<DateTime, int> periodCounts = new Dictionary<DateTime, int>();

        if (dates != null && dates.Any()) // If exist any borrow or return dates
        {
            // Determine period value to count for borrow or return values 
            periodCounts = period switch
            {
                // Daily group
                TrendPeriod.Daily => dates.GroupBy(d => d.Date)
                    .ToDictionary(g => g.Key, g => g.Count()),
                // Weekly group
                TrendPeriod.Weekly =>
                    dates.GroupBy(d => GetWeekStart(d))
                        .ToDictionary(g => g.Key, g => g.Count()),
                // Monthly group
                TrendPeriod.Monthly =>
                    dates.GroupBy(d => new DateTime(d.Year, d.Month, 1)) // Start from first date from the month
                        .ToDictionary(g => g.Key, g => g.Count()),
                // Default as daily group
                _ => dates.GroupBy(d => d.Date)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        // Build full-period trend data by iterating from start to end date
        // Initialize full-period trend data collection
        var fullTrends = new List<TrendDataDto>();
        // Determine trend period
        switch (period)
        {
            // Daily
            case TrendPeriod.Daily:
                // Iterate from start date to end date with loop step = 1 day
                for (DateTime dt = startDate.Date; dt <= endDate.Date; dt = dt.AddDays(1))
                {
                    periodCounts.TryGetValue(dt, out int count);
                    fullTrends.Add(new TrendDataDto
                    {
                        PeriodLabel = dt.ToString("dd/MM/yyyy"),
                        Value = count
                    });
                }
                break;
            // Weekly
            case TrendPeriod.Weekly:
                // Determine first Monday
                DateTime firstWeek = GetWeekStart(startDate);
                // Iterate week by week (endDate has been added more 12 months from current)
                for (DateTime weekStart = firstWeek; weekStart <= endDate; weekStart = weekStart.AddDays(7))
                {
                    periodCounts.TryGetValue(weekStart, out int count);
                    
                    // "Week 10 (01/08 - 07/08)"
                    var prefixOfLabel = lang == SystemLanguage.English ? "Week" : "Tuần";
                    var label = $"{prefixOfLabel} {CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                        weekStart, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday)} " +
                                   $"({weekStart:dd/MM} - {weekStart.AddDays(6):dd/MM})";
                    fullTrends.Add(new TrendDataDto
                    {
                        PeriodLabel = label,
                        Value = count
                    });
                }
                break;
            // Monthly
            case TrendPeriod.Monthly:
                // Retrieve current month
                DateTime currentMonth = new DateTime(startDate.Year, startDate.Month, 1);
                // Retrieve last month of the year
                DateTime lastMonth = new DateTime(endDate.Year, endDate.Month, 1);
                // Iterate until current month reach last month
                while (currentMonth <= lastMonth)
                {
                    periodCounts.TryGetValue(currentMonth, out int count);
                    fullTrends.Add(new TrendDataDto
                    {
                        PeriodLabel = $"{currentMonth.Month:00}-{currentMonth.Year}",
                        Value = count
                    });
                    // Increase to next month
                    currentMonth = currentMonth.AddMonths(1);
                }
                break;
        }

        return fullTrends;
    }

    /// <summary>
    /// Build trend data comparision between last year and current year
    /// </summary>
    private async Task<List<TrendDataDto>> GetTrendDataForYear(
        DateTime startDate, 
        DateTime endDate, 
        SystemLanguage lang)
    {
        // Retrieve borrow record based with specific start and end date range
        var borrowSpec = new BaseSpecification<BorrowRecordDetail>(br => 
            br.BorrowRecord.BorrowDate >= startDate && br.BorrowRecord.BorrowDate <= endDate);
        var borrowDates = (await _borrowRecDetailSvc.GetAllWithSpecAndSelectorAsync(
            specification: borrowSpec,
            selector: s => s.BorrowRecord.BorrowDate)).Data as List<DateTime>;
        
        // Group by month
        var groupedData = borrowDates?.GroupBy(date => date.Month)
            .Select(g => new TrendDataDto
            {
                PeriodLabel = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key),
                Value = g.Count()
            })
            .ToDictionary(x => x.PeriodLabel, x => x.Value) ?? new Dictionary<string, int>();
    
        // Determine current lang
        var isEng = lang == SystemLanguage.English;
        // Set culture based on language
        var culture = isEng ? new CultureInfo("en-US") : new CultureInfo("vi-VN");
        // Initialize full-period trend
        var fullPeriods = new List<TrendDataDto>();
        for (int month = 1; month <= 12; month++)
        {
            // Retrieve month name based on culture info
            var monthName = culture.DateTimeFormat.GetMonthName(month);
            fullPeriods.Add(new TrendDataDto
            {
                PeriodLabel = monthName,
                Value = groupedData.ContainsKey(monthName) ? groupedData[monthName] : 0
            });
        }
    
        return fullPeriods;
    }

    
    /// <summary>
    /// Get the Monday of the week for the given date
    /// </summary>
    private DateTime GetWeekStart(DateTime date)
    {
        // DayOfWeek enum: Sunday = 0, Monday = 1, ..., Saturday = 6
        int daysToMonday = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.Date.AddDays(-daysToMonday);
    }

    /// <summary>
    /// Build trend data comparision between last year and current year for revenue analytics  
    /// </summary>
    private List<BarchartTrendDataDto> GetTransactionTrendData(
        List<(DateTime? TransactionDate, decimal Amount)>? transactions,
        DateTime startDate,
        DateTime endDate,
        TrendPeriod period,
        SystemLanguage lang)
    {
        // Dictionary to hold aggregated revenue
        // key = month (1 to 12) for monthly
        // key = week number for weekly
        var groupedData = new Dictionary<int, decimal>();

        // Initialize full-period trends
        var fullTrends = new List<BarchartTrendDataDto>();
        
        // Check whether existing any paid transaction
        if (transactions != null && transactions.Any())
        {
            // Determine trend period
            switch (period)
            {
                // Weekly
                case TrendPeriod.Weekly:
                    groupedData = transactions
                        .GroupBy(t => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                            ((DateTime)t.TransactionDate!),
                            CalendarWeekRule.FirstFourDayWeek,
                            DayOfWeek.Monday))
                        .ToDictionary(g => g.Key, g => g.Sum(t => (decimal)t.Amount));
                    break;
                // Monthly
                case TrendPeriod.Monthly:
                    groupedData = transactions
                        .GroupBy(t => ((DateTime)t.TransactionDate!).Month)
                        .ToDictionary(g => g.Key, g => g.Sum(t => (decimal)t.Amount));
                    break;
            }
                
        }
        
        // Generate full timeline with default revenue = 0
        if (period == TrendPeriod.Weekly)
        {
            // Determine first Monday
            DateTime firstWeek = GetWeekStart(startDate);
            // Iterate week by week
            for (DateTime weekStart = firstWeek; weekStart <= endDate; weekStart = weekStart.AddDays(7))
            {
                // Determine week number
                var weekNum = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                    weekStart, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                // Retrieve revenue by week num key
                var revenue = groupedData.ContainsKey(weekNum) ? groupedData[weekNum] : 0;
                // "Week 10 (01/08 - 07/08)"
                var prefixOfLabel = lang == SystemLanguage.English ? "Week" : "Tuần";
                var label = $"{prefixOfLabel} {weekNum}" + $"({weekStart:dd/MM} - {weekStart.AddDays(6):dd/MM})";
                fullTrends.Add(new BarchartTrendDataDto
                {
                    PeriodLabel = label,
                    Value = revenue
                });
            }
        }
        else if (period == TrendPeriod.Monthly)
        {
            // Determine current lang
            var isEng = lang == SystemLanguage.English;
            // Set culture based on language
            var culture = isEng ? new CultureInfo("en-US") : new CultureInfo("vi-VN");
            for (int month = 1; month <= 12; month++)
            {
                // Retrieve revenue by month num key
                var revenue = groupedData.ContainsKey(month) ? groupedData[month] : 0;
                // Retrieve month name based on culture info
                var monthName = culture.DateTimeFormat.GetMonthName(month);
                fullTrends.Add(new BarchartTrendDataDto
                {
                    PeriodLabel = monthName,
                    Value = revenue
                });
            }
        }

        return fullTrends;
    }
}