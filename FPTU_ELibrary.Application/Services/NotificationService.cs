using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.Notifications;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Hubs;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using MapsterMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ELibrary.Application.Services.IServices;

public class NotificationService : GenericService<Notification, NotificationDto, int>,
    INotificationService<NotificationDto>
{
    private readonly IHubContext<NotificationHub> _hubContext;
    
    private readonly IUserService<UserDto> _userService;
    private readonly IEmployeeService<EmployeeDto> _employeeService;
    public NotificationService(
        IHubContext<NotificationHub> hubContext,
        ILogger logger,
        ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        IMapper mapper,IEmployeeService<EmployeeDto> employeeService,
        IUserService<UserDto> userService) 
        : base(msgService, unitOfWork, mapper, logger)
    {
        _hubContext = hubContext;
        _employeeService = employeeService;
        _userService = userService;
    }
    
    public override async Task<IServiceResult> GetAllWithSpecAsync(ISpecification<Notification> spec, bool tracked = true)
    {
        try
        {
            // Try to parse specification to NotificationSpecification
            var notificationSpec = spec as NotificationSpecification;
            // Check if specification is null
            if (notificationSpec == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }
            
            if (notificationSpec.IsCallFromManagement)
            {
                // Apply include
                notificationSpec.ApplyInclude(q => q
                    .Include(n => n.NotificationRecipients)
                        .ThenInclude(nr => nr.Recipient)
                    .Include(n => n.CreatedByNavigation)
                );
            }
            else
            {
                // Apply include
                notificationSpec.ApplyInclude(q => q
                    .Include(n => n.NotificationRecipients)
                        .ThenInclude(nr => nr.Recipient)
                    .Include(n => n.CreatedByNavigation)
                );
            }
            
            // Count total actual items in DB
            var totalNotification = await _unitOfWork.Repository<Notification, int>().CountAsync(notificationSpec);
            var totalPage = (int)Math.Ceiling((double)totalNotification / notificationSpec.PageSize);

            // Set pagination to specification after count total notification 
            if (notificationSpec.PageIndex > totalPage 
                || notificationSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
                notificationSpec.PageIndex = 1; // Set default to first page
            }
            
            notificationSpec.ApplyPaging(
                skip: notificationSpec.PageSize * (notificationSpec.PageIndex - 1),
                take: notificationSpec.PageSize
            );

            var entities = (await _unitOfWork.Repository<Notification, int>()
                .GetAllWithSpecAsync(notificationSpec, false)).ToList();
            if (entities.Any())
            {
                // Check whether is not call from management -> exclude all recipients are not belong to user
                if (!notificationSpec.IsCallFromManagement && !string.IsNullOrEmpty(notificationSpec.Email))
                {
                    var email = notificationSpec.Email;
                    foreach (var notification in entities)
                    {
                        if (notification.NotificationRecipients.Any())
                        {
                            notification.NotificationRecipients = notification.NotificationRecipients
                                .Where(r => Equals(r.Recipient.Email, email))
                                .Select(n => new NotificationRecipient()
                                {
                                    NotificationRecipientId = n.NotificationRecipientId,
                                    NotificationId = n.NotificationId,
                                    RecipientId = n.RecipientId,
                                    IsRead = n.IsRead
                                })
                                .ToList();
                        }
                    }
                }
                
                var paginationResultDto = new PaginatedResultDto<NotificationDto>(
                    _mapper.Map<IEnumerable<NotificationDto>>(entities),
                    notificationSpec.PageIndex, notificationSpec.PageSize, totalPage, totalNotification);

                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }

            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                _mapper.Map<IEnumerable<NotificationDto>>(entities));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress get all data");
        }
    }

    public override async Task<IServiceResult> UpdateAsync(int id, NotificationDto dto)
    {
        try
        {
            // Determine current system lang 
            var lang = (SystemLanguage?) EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Retrieve notification by id
            var existingEntity = await _unitOfWork.Repository<Notification, int>().GetByIdAsync(id);
            if (existingEntity == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, isEng
                        ? "notification to process update"
                        : "thông báo để tiến hành sửa đổi"));
            }
            
            // Change props
            existingEntity.Title = dto.Title;
            existingEntity.Message = dto.Message;
            existingEntity.NotificationType = dto.NotificationType;
            
            // Process update
            await _unitOfWork.Repository<Notification, int>().UpdateAsync(existingEntity);
            
            // Check if has changed or not
            if (!_unitOfWork.Repository<Notification, int>().HasChanges(existingEntity))
            {
                // Mark as update success
                return new ServiceResult(ResultCodeConst.SYS_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
            }
            // Save DB
            if (await _unitOfWork.SaveChangesAsync() > 0)
            {
                // Mark as update success
                return new ServiceResult(ResultCodeConst.SYS_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
            }
            
            // Mark as failed to update
            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update notification");
        }
    }

    public async Task<IServiceResult> GetByIdAsync(int id, string? email = null)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<Notification>(q => 
                q.NotificationId == id &&
                (
                    string.IsNullOrEmpty(email) || 
                    q.IsPublic || 
                    q.NotificationRecipients.Any(n => n.Recipient.Email == email) 
                ));

            if (string.IsNullOrEmpty(email)) // Call from management
            {
                // Apply include
                baseSpec.ApplyInclude(q => q
                    .Include(n => n.NotificationRecipients)
                    .ThenInclude(nr => nr.Recipient)
                    .Include(n => n.CreatedByNavigation)
                );
            }
            else
            {
                // Apply include
                baseSpec.ApplyInclude(q => q
                    .Include(n => n.CreatedByNavigation)
                );
            }
            
            // Retrieve notification with spec
            var existingEntity = await _unitOfWork.Repository<Notification, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                // Data not found or empty
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
            }

            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), _mapper.Map<NotificationDto>(existingEntity));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when get notification by id");
        }
    }
    
    public async Task<IServiceResult> CreateNotificationAsync(
        string createdByEmail, NotificationDto dto, List<string>? recipients)
    {
        // Create parallel thread to handle create notification 
        // Version 1: response when finished creating notification
        
        try
        {
            // Check exist employee
            var baseSpec = new BaseSpecification<Employee>(e => Equals(e.Email, createdByEmail));
            var employeeDto = (await _employeeService.GetWithSpecAsync(baseSpec)).Data as EmployeeDto;
            // Not found any employee match
            if (employeeDto == null) throw new ForbiddenException("Not allow to access"); // Forbid to access
            
            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(dto);
            // Check for valid validations
            if (validationResult != null && !validationResult.IsValid)
            {
                // Convert ValidationResult to ValidationProblemsDetails.Errors
                var errors = validationResult.ToProblemDetails().Errors;
                throw new UnprocessableEntityException("Invalid Validations", errors);
            }

            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Add create date
            dto.CreateDate = currentLocalDateTime;
            // Add create by
            dto.CreatedBy = employeeDto.EmployeeId;
            
            // Add notification recipients (if any)
            if (recipients != null && recipients.Any() && !dto.IsPublic)
            {
                foreach (var recipient in recipients)
                {
                    // Try to retrieve user by email
                    var user = (await _userService.GetByEmailAsync(recipient)).Data as UserDto;
                    if (user == null) // Not found user match
                    {
                        // Msg: Not found user's email {0} to process send privacy notification
                        var msg = await _msgService.GetMessageAsync(ResultCodeConst.Notification_Warning0003);
                        return new ServiceResult(ResultCodeConst.Notification_Warning0003,
                            StringUtils.Format(msg, recipient));
                    }
                    
                    // Add notification recipient
                    dto.NotificationRecipients.Add(new ()
                    {
                        RecipientId = user.UserId,
                        IsRead = false
                    });
                }
            }
            
            // Map notification from dto to entity
            var notificationEntity = _mapper.Map<Notification>(dto);
            // Process add new notification
            await _unitOfWork.Repository<Notification, int>().AddAsync(notificationEntity);
            // Save DB
            if (await _unitOfWork.SaveChangesAsync() > 0)
            {
                await SendHubNotificationAsync(notificationDto: dto, recipients: recipients, isPublic: dto.IsPublic);

                return new ServiceResult(ResultCodeConst.Notification_Success0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.Notification_Success0001));
            }
            
            // Msg: Failed to send notification
            return new ServiceResult(ResultCodeConst.Notification_Fail0001,
                await _msgService.GetMessageAsync(ResultCodeConst.Notification_Fail0001), false);
        }
        catch(ForbiddenException)
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
            throw new Exception("Error invoke while create notification");
        }
    }

    private async Task SendHubNotificationAsync(
        NotificationDto notificationDto, List<string>? recipients, bool isPublic)
    {
        // Build spec
        var userSpec = new BaseSpecification<User>(u => u.Role.EnglishName == nameof(Role.GeneralMember));
        // Apply include
        userSpec.ApplyInclude(q => q.Include(u => u.Role));
        // Retrieve all users with spec
        var users = (await _userService.GetAllWithSpecAndSelectorAsync(
            specification: userSpec,
            selector: u => new User()
            {
                UserId = u.UserId,
                Email = u.Email
            })).Data as List<User>;
        
        // Exist at least one user
        if (users != null)
        {
            // Convert to list
            var userEmails = users.Select(u => u.Email).ToList();
        
            if (isPublic)
            {
                // Iterate each email to process send notification
                var tasks = userEmails
                    .Select(email => _hubContext.Clients.User(email).SendAsync("ReceiveNotification", new
                    {
                        NotificationId = notificationDto.NotificationId,
                        Title = notificationDto.Title,
                        Message = notificationDto.Message,
                        IsPublic = true,
                        Timestamp = notificationDto.CreateDate,
                        NotificationType = notificationDto.NotificationType
                    }));

                await Task.WhenAll(tasks);
            }
            else if(recipients != null && recipients.Any())
            {
                var tasks = userEmails
                    .Where(email => recipients.Contains(email))
                    .Select(email => _hubContext.Clients.User(email).SendAsync("ReceiveNotification", new
                    {
                        NotificationId = notificationDto.NotificationId,
                        Title = notificationDto.Title,
                        Message = notificationDto.Message,
                        IsPublic = true,
                        Timestamp = notificationDto.CreateDate,
                        NotificationType = notificationDto.NotificationType
                    }));

                await Task.WhenAll(tasks);
            }
        }
    }
}