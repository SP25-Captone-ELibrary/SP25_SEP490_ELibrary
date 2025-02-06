using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Specifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class ReminderService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _svcScopeFactory;

    public ReminderService(
        ILogger logger,
        IServiceScopeFactory svcScopeFactory)
    {
        _logger = logger;
        _svcScopeFactory = svcScopeFactory;
    }
    
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.Information("ReminderService is starting.");
        cancellationToken.Register(() => _logger.Information("ReminderService background task is stopping."));

        // Gets whether cancellation has been requested for this token
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _svcScopeFactory.CreateScope())
                {
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    var appSettingMonitor = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<AppSettings>>();

                    var borrowRemindersToSend = await GetAllBorrowReqReminderToSendAsync(unitOfWork);
                    foreach (var br in borrowRemindersToSend)
                    {
                        // Retrieve user info
                        var userInfo = await GetUserInfoFromLibCardAsync(br.LibraryCardId, unitOfWork);
                        if (userInfo != null)
                        {
                            // Email subject 
                            var subject = $"[REMINDER] Library Borrowing Reminder - Pickup Before {br.ExpirationDate:MMMM dd, yyyy}";
                            
                            // Progress send confirmation email
                            var emailMessageDto = new EmailMessageDto( // Define email message
                                // Define Recipient
                                to: new List<string>() { userInfo.Email },
                                // Define subject
                                subject: subject,
                                // Add email body content
                                content: GetBorrowReminderEmailBody(
                                    user: userInfo,
                                    borrowReq: br, 
                                    libName: appSettingMonitor.CurrentValue.LibraryName,
                                    libLocation: appSettingMonitor.CurrentValue.LibraryLocation,
                                    libContact:appSettingMonitor.CurrentValue.LibraryContact)
                            );
                            
                            // Process send email
                            var isSent = await emailSvc.SendEmailAsync(emailMessageDto, isBodyHtml: true);
                            if (isSent)
                            {
                                // Update borrow reminder status 
                                var isUpdated = await UpdateBorrowRequestReminderStatusAsync(br.BorrowRequestId, unitOfWork);
                                if(isUpdated) _logger.Information("Update reminder status for borrow success");
                                else _logger.Error("Fail to Update reminder borrow status");
                            }
                        }
                        
                        _logger.Error("Not found user information to send borrow reminders.");
                    }

                    var libCardRemindersToSend = await GetAllLibCardReminderToSendAsync(unitOfWork);
                    foreach (var libCard in libCardRemindersToSend)
                    {
                        // Retrieve user info
                        var userInfo = await GetUserInfoFromLibCardAsync(libCard.LibraryCardId, unitOfWork);
                        if (userInfo != null)
                        {
                            // Email subject 
                            var subject = $"[REMINDER] Library Card Reminder - Expiry at {libCard.ExpiryDate:MMMM dd, yyyy}";
                            
                            // Progress send confirmation email
                            var emailMessageDto = new EmailMessageDto( // Define email message
                                // Define Recipient
                                to: new List<string>() { userInfo.Email },
                                // Define subject
                                subject: subject,
                                // Add email body content
                                content: GetLibraryCardExpiryReminderEmailBody(
                                    libraryCard: libCard, 
                                    libName: appSettingMonitor.CurrentValue.LibraryName,
                                    libContact:appSettingMonitor.CurrentValue.LibraryContact)
                            );
                            
                            // Process send email
                            var isSent = await emailSvc.SendEmailAsync(emailMessageDto, isBodyHtml: true);
                            if (isSent)
                            {
                                // Update borrow reminder status 
                                var isUpdated = await UpdateLibraryCardReminderStatusAsync(libCard.LibraryCardId, unitOfWork);
                                if(isUpdated) _logger.Information("Update reminder status for library card success");
                                else _logger.Error("Fail to Update reminder library card status");
                            }
                        }
                        
                        _logger.Error("Not found user information to send library card reminders.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "ReminderService task failed with exception.");
            }
            
            _logger.Information("ReminderService task doing background work.");
        
            // Delay 10m for each time execution
            await Task.Delay(600000, cancellationToken);
        }
        
        _logger.Information("ReminderService background task is stopping.");
    }
    
    #region Library Card Tasks
    private async Task<User?> GetUserInfoFromLibCardAsync(Guid libraryCardId, IUnitOfWork unitOfWork)
    {
        // Build spec
        var baseSpec = new BaseSpecification<User>(u => u.LibraryCardId == libraryCardId);
        // Retrieve data with spec
        return await unitOfWork.Repository<User, Guid>().GetWithSpecAsync(baseSpec);
    }

    private async Task<List<LibraryCard>> GetAllLibCardReminderToSendAsync(IUnitOfWork unitOfWork)
    {
        // Current local datetime
        var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            // Vietnam timezone
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        
        // Calculate the reminder threshold (24 hours before expiration)
        var reminderThreshold = currentLocalDateTime.AddHours(24); 
        
        // Build specification 
        var baseSpec = new BaseSpecification<LibraryCard>(
            br => br.ExpiryDate <= reminderThreshold && // Expiration time before 24 hours compared to now
                  br.IsReminderSent == false); // Not send reminder yet
        
        // Retrieve by spec
        var libCards = 
            await unitOfWork.Repository<LibraryCard, Guid>().GetAllWithSpecAsync(baseSpec, tracked: false);
        
        // Response
        return libCards.ToList();
    }
    
    private async Task<bool> UpdateLibraryCardReminderStatusAsync(Guid libraryCardId, IUnitOfWork unitOfWork)
    {
        // Retrieve by id 
        var existingEntity = await unitOfWork.Repository<LibraryCard, Guid>().GetByIdAsync(libraryCardId);
        if (existingEntity != null)
        {
            existingEntity.IsReminderSent = true;
            
            // Progress update
            await unitOfWork.Repository<LibraryCard, Guid>().UpdateAsync(existingEntity);
            return await unitOfWork.SaveChangesAsync() > 0;
        }
        
        _logger.Error("Failed to update the library card reminder status");
        return false;
    }

    private string GetLibraryCardExpiryReminderEmailBody(LibraryCard libraryCard, string libName, string libContact)
    {
        return $$"""
                 <!DOCTYPE html>
                 <html>
                 <head>
                     <meta charset="UTF-8">
                     <title>Library Card Expiration Notice</title>
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
                             border-left: 4px solid #3498db;
                         }
                         .details li {
                             margin: 5px 0;
                         }
                         .barcode {
                             color: #2980b9;
                             font-weight: bold;
                         }
                         .expiry-date {
                             color: #27ae60;
                             font-weight: bold;
                         }
                         .status-label {
                             color: #e74c3c;
                             font-weight: bold;
                         }
                         .status-text {
                             color: #f39c12;
                             font-weight: bold;
                         }
                         .footer {
                             margin-top: 20px;
                             font-size: 14px;
                             color: #7f8c8d;
                         }
                     </style>
                 </head>
                 <body>
                     <p class="header">Library Card Expiration Notice</p>
                     <p>Dear {{libraryCard.FullName}},</p>
                     <p>We would like to inform you that your library card is set to expire soon on <span class="expiry-date">{{libraryCard.ExpiryDate:MMMM dd, yyyy}}</span>. To continue enjoying our library services without interruption, please extend your card before the expiration date.</p>
                     
                     <p><strong>Library Card Details:</strong></p>
                     <div class="details">
                         <ul>
                             <li><span class="barcode">Library Card Code:</span> {{libraryCard.Barcode}}</li>
                             <li><span class="expiry-date">Expiry Date:</span> {{libraryCard.ExpiryDate:MMMM dd, yyyy}}</li>
                             <li><span class="status-label">Current Status:</span> <span class="status-text">{{libraryCard.Status}}</span></li>
                         </ul>
                     </div>
                     
                     <p>To extend your library card, please visit our library or contact us at <strong>{{libContact}}</strong>.</p>
                     
                     <p>If you have any questions or require assistance, feel free to reach out to us.</p>
                     
                     <p>Thank you for being a valued member of our library!</p>
                     
                     p><strong>Best regards,</strong></p>
                    <p>{{libName}}</p>
                 </body>
                 </html>
                 """;
    }

    #endregion

    #region Borrow Tasks
    private async Task<List<BorrowRequest>> GetAllBorrowReqReminderToSendAsync(IUnitOfWork unitOfWork)
    {
        // Current local datetime
        var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            // Vietnam timezone
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        
        // Calculate the reminder threshold (24 hours before expiration)
        var reminderThreshold = currentLocalDateTime.AddHours(24); 
        
        // Build specification 
        var baseSpec = new BaseSpecification<BorrowRequest>(
            br => br.ExpirationDate <= reminderThreshold && // Expiration time before 24 hours compared to now
                  br.Status == BorrowRequestStatus.Created && // In created status
                  br.IsReminderSent == false); // Not send reminder yet
        // Apply include
        baseSpec.ApplyInclude(q => q
            .Include(br => br.BorrowRequestDetails)
                .ThenInclude(brd => brd.LibraryItem)
        );
        // Retrieve by spec
        var borrowReqs = 
            await unitOfWork.Repository<BorrowRequest, int>().GetAllWithSpecAsync(baseSpec, tracked: false);
        
        // Response
        return borrowReqs.ToList();
    }
    
    private async Task<bool> UpdateBorrowRequestReminderStatusAsync(int borrowRequestId, IUnitOfWork unitOfWork)
    {
        // Retrieve by id 
        var existingEntity = await unitOfWork.Repository<BorrowRequest, int>().GetByIdAsync(borrowRequestId);
        if (existingEntity != null)
        {
            existingEntity.IsReminderSent = true;
            
            // Progress update
            await unitOfWork.Repository<BorrowRequest, int>().UpdateAsync(existingEntity);
            return await unitOfWork.SaveChangesAsync() > 0;
        }
        
        _logger.Error("Failed to update the borrow request reminder status");
        return false;
    }

    private string GetBorrowReminderEmailBody(User user, BorrowRequest borrowReq, string libName, string libLocation, string libContact)
    {
        var itemList = string.Join("", borrowReq.BorrowRequestDetails.Select(detail => $"<li>{detail.LibraryItem.Title}</li>"));
    
        return $"""
                 <!DOCTYPE html>
                 <html>
                 <head>
                     <meta charset="UTF-8">
                     <title>Library Borrowing Reminder</title>
                 </head>
                 <body>
                     <p><strong>Library Borrowing Reminder</strong></p>
                     <p>Dear {user.LastName}, {user.FirstName},</p>
                     <p>This is a friendly reminder that your requested library item(s) must be picked up before the expiry date.</p>
                     
                     <p><strong>Borrow Request Details:</strong></p>
                     <ul>
                         <li>Request ID: {borrowReq.BorrowRequestId}</li>
                         <li>Pickup Expiry Date: {borrowReq.ExpirationDate:MMMM dd, yyyy HH:mm}</li>
                         <li>Pickup Location: {libLocation}</li>
                     </ul>
                     
                     <p><strong>Requested Items:</strong></p>
                     <ul>
                         {itemList}
                     </ul>
                     
                     <p>Please ensure you pick up the requested item(s) before the expiry date to avoid automatic cancellation.</p>
                     
                     <p>If you have any questions or need further assistance, please contact the library at {libContact}.</p>
                     
                     <p>Thank you for using our library services!</p>
                     
                     <p><strong>Best regards,</strong></p>
                     <p>{libName}</p>
                 </body>
                 </html>
                 """;
    }
    #endregion
}