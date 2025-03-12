using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.Notifications;
using FPTU_ELibrary.Application.Exceptions;
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

    public override async Task<IServiceResult> GetByIdAsync(int id)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<Notification>(q => q.NotificationId == id);
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(n => n.NotificationRecipients)
                    .ThenInclude(nr => nr.Recipient)
                .Include(n => n.CreatedByNavigation)
            );
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
    
    public override async Task<IServiceResult> GetAllWithSpecAsync(
        ISpecification<Notification> spec, bool tracked = true)
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
            
            // Apply include
            notificationSpec.ApplyInclude(q => q
                .Include(n => n.NotificationRecipients)
                    .Include(n => n.CreatedByNavigation)
            );
            
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

            var entities = await _unitOfWork.Repository<Notification, int>()
                .GetAllWithSpecAsync(notificationSpec, false);
            if (entities.Any())
            {
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
            if (employeeDto == null) throw new ForbiddenException(); // Forbid to access
            
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

    public async Task<IServiceResult> GetAllPrivacyNotificationAsync(
        string email, ISpecification<Notification> spec)
    {
        try
        {
            // Initialize check exist field
            var isEmailExist = false;
            // Check exist user or employee 
            isEmailExist |= (await _userService.AnyAsync(u => Equals(u.Email, email))).Data is true;
            isEmailExist |= (await _employeeService.AnyAsync(e => Equals(e.Email, email))).Data is true;

            // Not found any match
            if (!isEmailExist)
            {
                throw new ForbiddenException();
            }

            // Try to parse specification to NotificationSpecification
            var notificationSpec = spec as NotificationSpecification;
            // Check if specification is null
            if (notificationSpec == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }

            // Apply include
            notificationSpec.ApplyInclude(q => q.Include(n => n.CreatedByNavigation));
            
            // Filtering 
            notificationSpec.AddFilter(x => x.NotificationRecipients.Any(nr => nr.Recipient.Email == email));
            
            // Enable to see privacy email
            notificationSpec.AddFilter(s => s.IsPublic || !s.IsPublic);
            
            // Count total actual items in DB
            var totalNotification = await _unitOfWork.Repository<Notification, int>().CountAsync(notificationSpec);
            var totalPage = (int)Math.Ceiling((double)totalNotification / notificationSpec.PageSize);

            // Set pagination to specification after count total notification 
            if (notificationSpec.PageIndex > totalPage
                || notificationSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
                notificationSpec.PageIndex = 1; // Set default to first page
            }

            // Apply paging
            notificationSpec.ApplyPaging(
                skip: notificationSpec.PageSize * (notificationSpec.PageIndex - 1),
                take: notificationSpec.PageSize
            );

            // Retrieve all data with spec
            var entities = await _unitOfWork.Repository<Notification, int>()
                .GetAllWithSpecAsync(notificationSpec, false);
            if (entities.Any())
            {
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
        catch (ForbiddenException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get all privacy notifications");
        }
    }
    
    public async Task<IServiceResult> GetPrivacyNotificationAsync(int id, string email)
    {
        try
        {
            // Initialize check exist field
            var isEmailExist = false;
            // Check exist user or employee 
            isEmailExist |= (await _userService.AnyAsync(u => Equals(u.Email, email))).Data is true;
            isEmailExist |= (await _employeeService.AnyAsync(e => Equals(e.Email, email))).Data is true;

            // Not found any match
            if (!isEmailExist)
            {
                throw new ForbiddenException();
            }
            
            // Build spec
            var baseSpec = new BaseSpecification<Notification>(q => q.NotificationId == id);
            // Apply include
            baseSpec.ApplyInclude(q => q.Include(n => n.CreatedByNavigation));
            // Retrieve notification with spec
            var existingEntity = await _unitOfWork.Repository<Notification, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                // Data not found or empty
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
            }

            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                _mapper.Map<NotificationDto>(existingEntity));
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when get notification by id");
        }
    }
    
    public async Task<IServiceResult> GetAllCardHolderNotificationByUserIdAsync(Guid userId, int pageIndex, int pageSize)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<NotificationRecipient>(n => n.RecipientId == userId);   
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(db => db.Notification)
            );
            
            // Add default order by
            baseSpec.AddOrderByDescending(n => n.Notification.CreateDate);

            // Count total borrow request
            var totalNotiWithSpec = await _unitOfWork.Repository<NotificationRecipient, int>().CountAsync(baseSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalNotiWithSpec / pageSize);

            // Set pagination to specification after count total notification 
            if (pageIndex > totalPage
                || pageIndex < 1) // Exceed total page or page index smaller than 1
            {
                pageIndex = 1; // Set default to first page
            }
            // Apply pagination
            baseSpec.ApplyPaging(skip: pageSize * (pageIndex - 1), take: pageSize);
            
            // Retrieve data with spec
            var entities = await _unitOfWork.Repository<NotificationRecipient, int>()
                .GetAllWithSpecAsync(baseSpec);
            if (entities.Any())
            {
                var notiDtos = _mapper.Map<List<NotificationRecipientDto>>(entities);
                
                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<LibraryCardHolderNotificationRecipientDto>(
                    notiDtos.Select(n => n.ToCardHolderNotiRecipientDto()),
                    pageIndex, pageSize, totalPage, totalNotiWithSpec);

                // Response with pagination 
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }
            
            // Data not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                new List<LibraryCardHolderNotificationRecipientDto>());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get all notification by user id");
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