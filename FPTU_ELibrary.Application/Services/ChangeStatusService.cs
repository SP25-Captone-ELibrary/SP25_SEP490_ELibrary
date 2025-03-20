using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using iTextSharp.text.pdf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class ChangeStatusService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _svcScopeFactory;

    public ChangeStatusService(
        ILogger logger,
        IServiceScopeFactory svcScopeFactory)
    {
        _logger = logger;
        _svcScopeFactory = svcScopeFactory;
    }
    
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.Information("ChangeStatusService is starting.");
        cancellationToken.Register(() => _logger.Information("ChangeStatusService background task is stopping."));

        // Gets whether cancellation has been requested for this token
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _svcScopeFactory.CreateScope())
                {
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var monitor = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<BorrowSettings>>();
                    var reservationSvc = scope.ServiceProvider.GetRequiredService<IReservationQueueService<ReservationQueueDto>>();
                    var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();              
                    
                    // Track if there are any changes
                    bool hasChanges = false;
                    
                    // Update transaction expired status
                    hasChanges |= await UpdateAllTransactionExpiredStatusAsync(unitOfWork);
                    // Update library card expired status
                    hasChanges |= await UpdateAllLibraryCardExpiredStatusAsync(unitOfWork);
                    // Update library card suspended status
                    hasChanges |= await UpdateAllLibraryCardSuspendedStatusAsync(unitOfWork);
                    // Update borrow request expired status
                    hasChanges |= await UpdateAllBorrowRequestExpiredStatusAsync(unitOfWork, borrowSettings: monitor.CurrentValue);
                    // Update borrow record expired status
                    hasChanges |= await UpdateAllBorrowRecordExpiredStatusAsync(unitOfWork);
                    // Update digital borrow expired status
                    hasChanges |= await UpdateAllDigitalBorrowExpiredStatusAsync(unitOfWork);
                    // Update reservation expired status
                    hasChanges |= await UpdateAllReservationExpiredStatusAsync(
                        emailSvc: emailSvc,
                        reservationSvc: reservationSvc, 
                        unitOfWork: unitOfWork,
                        borrowSettings: monitor.CurrentValue);
                    // Assign item's instance to reservations (if any)
                    hasChanges |= await AssignItemToReservationAsync(
                        reservationSvc: reservationSvc,
                        unitOfWork: unitOfWork);
                    
                    // Save changes only if at least one update was made
                    if (hasChanges) await unitOfWork.SaveChangesAsync();
                }
            }catch (Exception ex)
            {
                _logger.Error(ex, "ChangeStatusService task failed with exception.");
            }
            
            _logger.Information("ChangeStatusService task doing background work.");
            
            // Delay 30s for each time execution
            await Task.Delay(30000, cancellationToken);
        }
    }

    #region Library Card Tasks
    private async Task<bool> UpdateAllLibraryCardExpiredStatusAsync(IUnitOfWork unitOfWork)
    {
        // Initialize has changes field
        bool hasChanges = false;
        
        // Current local datetime
        var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            // Vietnam timezone
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        
        // Build specification
        var baseSpec = new BaseSpecification<LibraryCard>(br => br.ExpiryDate <= currentLocalDateTime // Exp date exceed than current date 
                                                                  && br.Status == LibraryCardStatus.Active); // In active status
        var entities = await unitOfWork.Repository<LibraryCard, Guid>()
            .GetAllWithSpecAsync(baseSpec);
        foreach (var libCard in entities)
        {
            libCard.Status = LibraryCardStatus.Expired;
            
            // Progress update 
            await unitOfWork.Repository<LibraryCard, Guid>().UpdateAsync(libCard);
            
            // Mark as changed
            hasChanges = true;
        }

        return hasChanges;
    }
    
    private async Task<bool> UpdateAllLibraryCardSuspendedStatusAsync(IUnitOfWork unitOfWork)
    {
        // Initialize has changes field
        bool hasChanges = false;
        
        // Current local datetime
        var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            // Vietnam timezone
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        
        // Build specification
        var baseSpec = new BaseSpecification<LibraryCard>(lc => lc.SuspensionEndDate != null  // Exp date exceed than current date 
                                                                && lc.SuspensionEndDate <= currentLocalDateTime
                                                                && lc.Status == LibraryCardStatus.Suspended); // In suspended status
        var entities = await unitOfWork.Repository<LibraryCard, Guid>()
            .GetAllWithSpecAsync(baseSpec);
        foreach (var libCard in entities)
        {
            // Default update to active status
            libCard.Status = LibraryCardStatus.Active;
            
            // Check whether card is expired
            if(libCard.ExpiryDate <= currentLocalDateTime) libCard.Status = LibraryCardStatus.Expired;
            
            // Set default value
            libCard.TotalMissedPickUp = 0;
            libCard.SuspensionEndDate = null;
            
            // Progress update 
            await unitOfWork.Repository<LibraryCard, Guid>().UpdateAsync(libCard);
            
            // Mark as changed
            hasChanges = true;
        }

        return hasChanges;
    }
    #endregion
    
    #region Borrow Tasks
    private async Task<bool> UpdateAllBorrowRequestExpiredStatusAsync(IUnitOfWork unitOfWork, BorrowSettings borrowSettings)
    {
        // Initialize has changes field
        bool hasChanges = false;
        
        // Current local datetime
        var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            // Vietnam timezone
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        
        // Build specification
        var baseSpec = new BaseSpecification<BorrowRequest>(br => br.ExpirationDate <= currentLocalDateTime // Exp date exceed than current date 
                                                                  && br.Status == BorrowRequestStatus.Created); // In created status
        // Apply include
        baseSpec.ApplyInclude(q => q
            .Include(b => b.BorrowRequestDetails)
                .ThenInclude(b => b.LibraryItem)
        );
        // Retrieve all with spec
        var entities = await unitOfWork.Repository<BorrowRequest, int>()
            .GetAllWithSpecAsync(baseSpec);
        foreach (var borrowReq in entities)
        {
            borrowReq.Status = BorrowRequestStatus.Expired;
            
            // Update library card status
            var libCard = await unitOfWork.Repository<LibraryCard, Guid>().GetByIdAsync(borrowReq.LibraryCardId);
            if (libCard != null)
            {
                // Increase total missed
                libCard.TotalMissedPickUp += 1;
                
                // Change status to suspended if exceed than threshold
                if (libCard.TotalMissedPickUp >= borrowSettings.TotalMissedPickUpAllow)
                {
                    libCard.Status = LibraryCardStatus.Suspended;
                    libCard.SuspensionEndDate = currentLocalDateTime.AddDays(borrowSettings.EndSuspensionInDays);
                }
                
                // Update card
                await unitOfWork.Repository<LibraryCard, Guid>().UpdateAsync(libCard);
            }
            
            // Update inventory amount after borrow request expired
            foreach (var brd in borrowReq.BorrowRequestDetails)
            {
                // Retrieve library item inventory
                var itemInventory = await unitOfWork.Repository<LibraryItemInventory, int>().GetByIdAsync(brd.LibraryItemId);
                if (itemInventory != null)
                {
                    // Check whether requested units is greater than 0
                    if (itemInventory.RequestUnits > 0)
                    {
                        // Reduce request units
                        itemInventory.RequestUnits--;
                        // Increase available units
                        itemInventory.AvailableUnits++;
                        // Process update inventory
                        await unitOfWork.Repository<LibraryItemInventory, int>().UpdateAsync(itemInventory);
                    }
                }
            }
            
            // Progress update 
            await unitOfWork.Repository<BorrowRequest, int>().UpdateAsync(borrowReq);
            
            
            // Mark as changed
            hasChanges = true;
        }

        return hasChanges;
    }

    private async Task<bool> UpdateAllBorrowRecordExpiredStatusAsync(IUnitOfWork unitOfWork)
    {
        // Initialize has changes field
        bool hasChanges = false;
        
        // Current local datetime
        var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            // Vietnam timezone
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        
        // Build specification
        var baseSpec = new BaseSpecification<BorrowRecordDetail>(br =>
            br.DueDate <= currentLocalDateTime && // Exceed than due date (expected return date)
            br.Status == BorrowRecordStatus.Borrowing); // Is in borrowing status
        // Retrieve all with spec
        var entities = await unitOfWork.Repository<BorrowRecordDetail, int>()
            .GetAllWithSpecAsync(baseSpec);
        foreach (var brd in entities)
        {
            // Change borrow status to expired
            brd.Status = BorrowRecordStatus.Overdue;
            
            // Progress update 
            await unitOfWork.Repository<BorrowRecordDetail, int>().UpdateAsync(brd);
            
            // Mark as changed
            hasChanges = true;
        }
        
        return hasChanges;
    }

    private async Task<bool> UpdateAllDigitalBorrowExpiredStatusAsync(IUnitOfWork unitOfWork)
    {
        // Initialize has changes field
        bool hasChanges = false;
        
        // Current local datetime
        var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            // Vietnam timezone
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        
        // Build specification
        var baseSpec = new BaseSpecification<DigitalBorrow>(br =>
            br.ExpiryDate <= currentLocalDateTime && // Exceed than due date (expected return date)
            br.Status == BorrowDigitalStatus.Active); // Is in borrowing status
        // Retrieve all with spec
        var entities = await unitOfWork.Repository<DigitalBorrow, int>()
            .GetAllWithSpecAsync(baseSpec);
        foreach (var br in entities)
        {
            // Change borrow status to expired
            br.Status = BorrowDigitalStatus.Expired;
            
            // Progress update 
            await unitOfWork.Repository<DigitalBorrow, int>().UpdateAsync(br);
            
            // Mark as changed
            hasChanges = true;
        }
        
        return hasChanges;
    }
    #endregion

    #region Transaction
    private async Task<bool> UpdateAllTransactionExpiredStatusAsync(IUnitOfWork unitOfWork)
    {
        // Initialize has changes field
        bool hasChanges = false;
        
        // Current local datetime
        var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(
            // Subtract 5 minutes compared to the actual expiration time to avoid paid in third party but failed to save in system
            DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(5)),
            // Vietnam timezone
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        
        // Build specification
        var baseSpec = new BaseSpecification<Transaction>(t => t.ExpiredAt <= currentLocalDateTime && 
                                                               t.TransactionStatus == TransactionStatus.Pending);
        var entities = await unitOfWork.Repository<Transaction, int>()
            .GetAllWithSpecAsync(baseSpec);
        foreach (var transaction in entities)
        {
            transaction.TransactionStatus = TransactionStatus.Expired;
            
            // Progress update 
            await unitOfWork.Repository<Transaction, int>().UpdateAsync(transaction);
            
            // Mark as changed
            hasChanges = true;
        }
        
        return hasChanges;
    }
    #endregion
    
    #region Reservation Tasks
    private async Task<bool> UpdateAllReservationExpiredStatusAsync(
        IEmailService emailSvc,
        IReservationQueueService<ReservationQueueDto> reservationSvc,
        IUnitOfWork unitOfWork, BorrowSettings borrowSettings)
    {
        // Initialize has changes field
        bool hasChanges = false;
        
        // Initialize collection of item instance ids need to be assigned
        var instanceToAssignedIds = new List<int>();
        
        // Current local datetime
        var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            // Vietnam timezone
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        // Build specification
        var baseSpec = new BaseSpecification<ReservationQueue>(br => br.ExpiryDate <= currentLocalDateTime // Exp date exceed than current date 
                                                                  && br.QueueStatus == ReservationQueueStatus.Assigned); // In assigned status
        // Apply include
        baseSpec.ApplyInclude(q => q
            .Include(r => r.LibraryItemInstance)
            .Include(r => r.LibraryItem)
            .Include(r => r.LibraryCard)
        );
        // Retrieve all with spec
        var entities = await unitOfWork.Repository<ReservationQueue, int>()
            .GetAllWithSpecAsync(baseSpec);
        // Iterate each reservation to update expired status
        foreach (var reservation in entities)
        {
            reservation.QueueStatus = ReservationQueueStatus.Expired;
                        
            // Update library card status
            var libCard = await unitOfWork.Repository<LibraryCard, Guid>().GetByIdAsync(reservation.LibraryCardId);
            if (libCard != null)
            {
                // Increase total missed
                libCard.TotalMissedPickUp += 1;
                
                // Change status to suspended if exceed than threshold
                if (libCard.TotalMissedPickUp >= borrowSettings.TotalMissedPickUpAllow)
                {
                    libCard.Status = LibraryCardStatus.Suspended;
                    libCard.SuspensionEndDate = currentLocalDateTime.AddDays(borrowSettings.EndSuspensionInDays);
                }
                
                // Update card
                await unitOfWork.Repository<LibraryCard, Guid>().UpdateAsync(libCard);
            }
            
            // Retrieve library item instance
            var instanceSpec = new BaseSpecification<LibraryItemInstance>(li => 
                li.LibraryItemInstanceId == reservation.LibraryItemInstanceId);
            // Apply include
            instanceSpec.ApplyInclude(q => q
                .Include(l => l.LibraryItem)
                    .ThenInclude(li => li.LibraryItemInventory!)
            );
            // Retrieve with spec
            var itemInstance = await unitOfWork.Repository<LibraryItemInstance, int>().GetWithSpecAsync(instanceSpec);
            if (itemInstance != null)
            {
                // Retrieve item's inventory
                var itemInventory = itemInstance.LibraryItem.LibraryItemInventory;
                if (itemInventory != null) // Update inventory amount
                {
                    // Check whether reserved units is greater than 0
                    if (itemInventory.ReservedUnits > 0)
                    {
                        // Update instance status
                        itemInstance.Status = nameof(LibraryItemInstanceStatus.OutOfShelf);
                        // Reduce request units
                        itemInventory.ReservedUnits--;
                    
                        // Add instance to assigned list
                        instanceToAssignedIds.Add(itemInstance.LibraryItemInstanceId);
                        
                        // Process update item instance
                        await unitOfWork.Repository<LibraryItemInstance, int>().UpdateAsync(itemInstance);
                        
                        // Mark as changed
                        hasChanges = true;
                    }
                }
            }
        }

        if (hasChanges && instanceToAssignedIds.Any())
        {
            // Process save changes to DB
            var isSaved = await unitOfWork.SaveChangesWithTransactionAsync() > 0;
            if (isSaved)
            {
                // Process reassigned item instance to other reservations (if any)
                await reservationSvc.AssignReturnItemAsync(libraryItemInstanceIds: instanceToAssignedIds);
            }
        }
        
        return false; // Always return false to avoid save change many times
    }

    private async Task<bool> AssignItemToReservationAsync(
        IReservationQueueService<ReservationQueueDto> reservationSvc,
        IUnitOfWork unitOfWork)
    {
        // Build item instance spec
        var instanceSpec = new BaseSpecification<LibraryItemInstance>(li => 
            (
                li.Status == nameof(LibraryItemInstanceStatus.OutOfShelf) || // Is in out-of-shelf status
                li.Status == nameof(LibraryItemInstanceStatus.InShelf) // Is in-shelf status
            ) && // Is in-shelf status
            li.LibraryItem.Status == LibraryItemStatus.Published &&  // Instance's item has been published yet
            li.IsCirculated == true); // Ensure the instance has been circulated (borrowed) 
        // Retrieve all instance with spec
        var itemInstances = (await unitOfWork.Repository<LibraryItemInstance, int>()
            .GetAllWithSpecAsync(instanceSpec)).ToList();
        if (itemInstances.Any())
        {
            // Iterate each available item to assign to reservations (if any)
            foreach (var instance in itemInstances)
            {
                // Retrieve all reservations with pending status
                var reserveSpec = new BaseSpecification<ReservationQueue>(r =>
                    r.QueueStatus == ReservationQueueStatus.Pending && // Is pending status
                    r.LibraryItemInstanceId == null && // Not assigned with any instance
                    r.LibraryItem.LibraryItemInstances.Any(li => 
                        li.LibraryItemInstanceId == instance.LibraryItemInstanceId) && // Instance exist in reservation's item
                    r.ExpiryDate == null && // Not exist expiry date
                    // Exclude all cancellation fields
                    r.CancellationReason == null &&
                    r.CancelledBy == null
                );
                // Retrieve first with spec
                var reservation = await unitOfWork.Repository<ReservationQueue, int>().GetWithSpecAsync(reserveSpec);
                if (reservation != null) // Required exist at least once reservation match to process assign instance
                {
                    // Try to assign item instance to reservations (if any)
                    await reservationSvc.AssignReturnItemAsync([instance.LibraryItemInstanceId]);
                }
            }
        }
        
        return false; // Always return false to avoid save change many times
    }
    #endregion
    
    // TODO: Implement send email for expired items
    #region Send Email Handling
    private async Task<bool> SendOverdueReservationPickupEmailAsync(
        IEmailService emailSvc,
        string email,
        ReservationQueue reservation, BorrowSettings borrowSettings,
    	string libName, string libContact)
    {
    	try
    	{
    		// Email subject
    		var subject = "[ELIBRARY] Thông Báo Quá Hạn Nhận Tài Liệu";
    	
    		// Process send email
    		var emailMessageDto = new EmailMessageDto( // Define email message
    			// Define Recipient
    			to: new List<string>() { email },
    			// Define subject
    			subject: subject,
    			// Add email body content
    			content: GetOverduePickupEmailBody(
                    reservation: reservation,
                    borrowSettings: borrowSettings,
    				libName: libName,
    				libContact:libContact)
    		);
    		
    		// Process send email
    		return await emailSvc.SendEmailAsync(emailMessageDto, isBodyHtml: true);
    	}
    	catch (Exception ex)
    	{
    		_logger.Error(ex.Message);
    		throw new Exception("Error invoke when process send overdue reservation pickup email");
    	}
    }
    
    private string GetOverduePickupEmailBody(
        ReservationQueue reservation, BorrowSettings borrowSettings,
        string libName, string libContact)
    {
        // Initialize email content
        string headerMessage = "Thông Báo Quá Hạn Nhận Tài Liệu";
        string mainMessage = "Bạn có một tài liệu đã được giữ chỗ cho bạn, nhưng thời hạn nhận đã hết hạn. Dưới đây là chi tiết tài liệu:";
        
        // Reservation details section
        string reservationInfoSection = $$"""
            <p><strong>Thông Tin Đặt Trước:</strong></p>
            <div class="details">
                <ul>
                    <li><strong>Mã Đặt Trước:</strong> <span class="reservation-code">{{reservation.ReservationCode}}</span></li>
                    <li><strong>Ngày Đặt:</strong> <span class="reservation-date">{{reservation.ReservationDate:dd/MM/yyyy HH:mm}}</span></li>
                    <li><strong>Ngày Hết Hạn:</strong> <span class="expiry-date">{{reservation.ExpiryDate:dd/MM/yyyy HH:mm}}</span></li>
                    <li><strong>Trạng Thái Đặt:</strong> <span class="status-text">{{reservation.QueueStatus.GetDescription()}}</span></li>
                    <li><strong>Đã thông báo hết hạn:</strong> <span class="status-text">{{(reservation.IsNotified ? "Đã gửi email thông báo" : "Chưa gửi thông báo")}}</span></li>
                </ul>
            </div>
            """;
        
	    // Expected available dates section (if provided)
        string expectedDateSection = $$"""
            <p><strong>Dự Kiến Ngày Có Sẵn:</strong></p>
            <div class="details">
                <ul>
                    <li><strong>Ngày Dự Kiến Có Sẵn (Tối thiểu):</strong> <span class="expiry-date">{{reservation.ExpectedAvailableDateMin?.ToString("dd/MM/yyyy HH:mm") ?? "N/A"}}</span></li>
                    <li><strong>TNgày Dự Kiến Có Sẵn (Tối đa):</strong> <span class="expiry-date">{{reservation.ExpectedAvailableDateMax?.ToString("dd/MM/yyyy HH:mm") ?? "N/A"}}</span></li>
                </ul>
            </div>
            """;
	    
        // Build library item details section
        string libraryItemDetails = $$"""
            <p><strong>Thông Tin Tài Liệu:</strong></p>
            <div class="details">
                <ul>
                    <li><strong>Tiêu Đề:</strong> <span class="title">{{reservation.LibraryItem.Title}}</span></li>
                    <li><strong>ISBN:</strong> <span class="isbn">{{reservation.LibraryItem.Isbn ?? "Không có"}}</span></li>
                    <li><strong>Năm Xuất Bản:</strong> {{reservation.LibraryItem.PublicationYear}}</li>
                    <li><strong>Nhà Xuất Bản:</strong> {{reservation.LibraryItem.Publisher ?? "Không có"}}</li>
                </ul>
            </div>
            """;

        // Build item instance details
        string instanceDetails = string.Empty;
        if (reservation.LibraryItemInstance != null)
        {
            instanceDetails = $$"""
                <p><strong>Thông Tin Bản Sao Đã Gán:</strong></p>
                <div class="details">
                    <ul>
                        <li><strong>Mã Vạch:</strong> <strong>{{reservation.LibraryItemInstance.Barcode}}</strong></li>
                        <li><strong>Tình Trạng:</strong> {{reservation.LibraryItemInstance.Status}}</li>
                    </ul>
                </div>
                """;
        }
        
	    // Calculate remaining allowed missed pickups
        int allowedMissedPickups = borrowSettings.TotalMissedPickUpAllow;
        int remainingMisses = allowedMissedPickups - reservation.LibraryCard.TotalMissedPickUp;
        remainingMisses = remainingMisses < 0 ? 0 : remainingMisses;

        // Library policy
        string policySection;
        if (remainingMisses == 0)
        {
            policySection = $$"""
                <p><strong>Chính Sách Thư Viện:</strong></p>
                <div class="details">
                    <ul>
                        <li>Do bạn đã bỏ lỡ lịch hẹn nhận tài liệu đủ <strong>{{allowedMissedPickups}}</strong> lần, thẻ của bạn hiện đã bị treo trong <strong>{{borrowSettings.EndSuspensionInDays}} ngày.</strong></li>
                        <li>Trong thời gian treo, bạn sẽ không được sử dụng các dịch vụ của thư viện (mượn, đặt trước, ...).</li>
                    </ul>
                </div>
                """;
        }
        else
        {
            policySection = $$"""
                <p><strong>Chính Sách Thư Viện:</strong></p>
                <div class="details">
                    <ul>
                        <li>Mỗi thẻ chỉ được phép bỏ lỡ lịch hẹn nhận tài liệu tối đa <strong>{{allowedMissedPickups}}</strong> lần.</li>
                        <li>Hiện tại, bạn đã bỏ lỡ <strong>{{reservation.LibraryCard.TotalMissedPickUp}}</strong> lần.</li>
                        <li>Nếu bạn bỏ lỡ thêm <strong>{{remainingMisses}}</strong> lần nữa, thẻ của bạn sẽ bị treo trong <strong>{{borrowSettings.EndSuspensionInDays}} ngày</strong> và bạn sẽ không được sử dụng các dịch vụ của thư viện.</li>
                    </ul>
                </div>
                """;
        }
	    
        // Build HTML content
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
                        color: #c0392b;
                        font-weight: bold;
                    }
                    .details {
                        margin: 15px 0;
                        padding: 10px;
                        background-color: #f9f9f9;
                        border-left: 4px solid #e74c3c;
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
                <p>Xin chào {{reservation.LibraryCard.FullName}},</p>
                <p>{{mainMessage}}</p>
                
                {{reservationInfoSection}}
                {{expectedDateSection}}
                {{libraryItemDetails}}
                {{instanceDetails}}
                {{policySection}}
                
                <p>Nếu bạn đã nhận được tài liệu hoặc cần hỗ trợ, vui lòng liên hệ qua email: <strong>{{libContact}}</strong>.</p>
                <p>Cảm ơn bạn đã sử dụng dịch vụ của thư viện.</p>
                
                <p class="footer"><strong>Trân trọng,</strong></p>
                <p class="footer">{{libName}}</p>
            </body>
            </html>
            """;
	}
    #endregion
}