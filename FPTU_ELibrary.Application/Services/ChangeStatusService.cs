using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Specifications;
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
                    
                    // Save changes only if at least one update was made
                    if (hasChanges) await unitOfWork.SaveChangesAsync();
                }
            }catch (Exception ex)
            {
                _logger.Error(ex, "ChangeStatusService task failed with exception.");
            }
            
            _logger.Information("ChangeStatusService task doing background work.");
            
            // Delay 10s for each time execution
            await Task.Delay(10000, cancellationToken);
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
}