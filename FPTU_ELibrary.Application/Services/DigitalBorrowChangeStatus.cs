using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Specifications;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class DigitalBorrowChangeStatus : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _svcScopeFactory;
    public DigitalBorrowChangeStatus(
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
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _svcScopeFactory.CreateScope())
                {
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    // Track if there are any changes
                    bool hasChanges = false;
                    
                    //Update digital borrow status
                    hasChanges |= await UpdateDigitalBorrowStatus(unitOfWork);
                    
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
    private async Task<bool>UpdateDigitalBorrowStatus(IUnitOfWork unitOfWork)
    {
        bool hasChange = false;
        // Current local datetime
        var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(
            // Subtract 5 minutes compared to the actual expiration time to avoid paid in third party but failed to save in system
            DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(5)),
            // Vietnam timezone
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        
        var baseSpec = new BaseSpecification<DigitalBorrow>(t => t.ExpiryDate <= currentLocalDateTime && 
                                                               t.Status == BorrowDigitalStatus.Active);
        var entities = await unitOfWork.Repository<DigitalBorrow, int>()
            .GetAllWithSpecAsync(baseSpec);
        
        bool hasChanges = false;
        foreach (var digitalBorrow in entities)
        {
            digitalBorrow.Status = BorrowDigitalStatus.Expired;
            
            // Progress update 
            await unitOfWork.Repository<DigitalBorrow, int>().UpdateAsync(digitalBorrow);
            
            // Mark as changed
            hasChanges = true;
        }
        
        return hasChanges;
    }
}