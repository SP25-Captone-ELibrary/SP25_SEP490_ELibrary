using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Extensions;
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
                            var subject = $"[ELIBRARY] Nhắc nhở lịch đặt mượn sách - Lấy Trước Ngày {br.ExpirationDate:dd/MM/yyyy}";
                            
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
                            var subject = $"[ELIBRARY] Thông Báo Thẻ Thư Viện Sắp Hết Hạn - Ngày Hết Hạn {libCard.ExpiryDate:dd/MM/yyyy}";
                            
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
                     <title>Thông Báo Hết Hạn Thẻ Thư Viện</title>
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
                     <p class="header">Thông Báo Hết Hạn Thẻ Thư Viện</p>
                     <p>Xin chào {{libraryCard.FullName}},</p>
                     <p>Chúng tôi xin thông báo rằng thẻ thư viện của bạn sắp hết hạn vào ngày <span class="expiry-date">{{libraryCard.ExpiryDate:dd/MM/yyyy}}</span>. Để tiếp tục sử dụng các dịch vụ của thư viện mà không bị gián đoạn, vui lòng gia hạn thẻ trước ngày hết hạn.</p>
                     
                     <p><strong>Chi Tiết Thẻ Thư Viện:</strong></p>
                     <div class="details">
                         <ul>
                             <li><span class="barcode">Mã Thẻ Thư Viện:</span> {{libraryCard.Barcode}}</li>
                             <li><span class="expiry-date">Ngày Hết Hạn:</span> {{libraryCard.ExpiryDate:dd/MM/yyyy}}</li>
                             <li><span class="status-label">Trạng Thái Hiện Tại:</span> <span class="status-text">{{libraryCard.Status.GetDescription()}}</span></li>
                         </ul>
                     </div>
                     
                     <p>Để gia hạn thẻ thư viện, vui lòng đến trực tiếp thư viện hoặc liên hệ với chúng tôi qua email <strong>{{libContact}}</strong>.</p>
                     
                     <p>Nếu bạn có bất kỳ câu hỏi nào hoặc cần hỗ trợ, đừng ngần ngại liên hệ với chúng tôi.</p>
                     
                     <p><strong>Trân trọng,</strong></p>
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
                    <title>Nhắc Nhở Mượn Sách</title>
                </head>
                <body>
                    <p><strong>Nhắc Nhở Mượn Sách</strong></p>
                    <p>Xin chào {user.LastName} {user.FirstName},</p>
                    <p>Đây là lời nhắc thân thiện rằng bạn cần đến thư viện để nhận các tài liệu đã yêu cầu trước ngày hết hạn.</p>
                    
                    <p><strong>Chi Tiết Yêu Cầu Mượn:</strong></p>
                    <ul>
                        <li>Mã Yêu Cầu: {borrowReq.BorrowRequestId}</li>
                        <li>Ngày Hết Hạn Nhận Sách: {borrowReq.ExpirationDate:dd/MM/yyyy HH:mm}</li>
                        <li>Địa Điểm Nhận Sách: {libLocation}</li>
                    </ul>
                    
                    <p><strong>Các Tài Liệu Đã Yêu Cầu:</strong></p>
                    <ul>
                        {itemList}
                    </ul>
                    
                    <p>Vui lòng đến nhận các tài liệu trước ngày hết hạn để tránh việc yêu cầu bị tự động hủy.</p>
                    
                    <p>Nếu bạn có bất kỳ câu hỏi nào hoặc cần hỗ trợ, vui lòng liên hệ thư viện qua số {libContact}.</p>
                    
                    <p>Cảm ơn bạn đã sử dụng dịch vụ của thư viện!</p>
                    
                    <p><strong>Trân trọng,</strong></p>
                    <p>{libName}</p>
                </body>
                </html>
                """;
    }
    #endregion
}