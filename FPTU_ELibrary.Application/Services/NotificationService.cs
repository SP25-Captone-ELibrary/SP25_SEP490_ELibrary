using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Hubs;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using MapsterMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace FPTU_ELibrary.Application.Services.IServices;

public class NotificationService : GenericService<Notification, NotificationDto, Guid>,
    INotificationService<NotificationDto>
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly INotificationRecipientService<NotificationRecipientDto> _notificationRecipient;
    private readonly IUserService<UserDto> _userService;

    public NotificationService(
        IHubContext<NotificationHub> hubContext,
        ILogger logger,
        ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        INotificationRecipientService<NotificationRecipientDto> notificationRecipient,
        IUserService<UserDto> userService) : base(msgService, unitOfWork,
        mapper, logger)
    {
        _hubContext = hubContext;
        _notificationRecipient = notificationRecipient;
        _userService = userService;
    }

    public async Task<IServiceResult> CreateNotification(NotificationDto noti,
        string createBy, List<string>? recipients)
    {
        //create parallel thread to handle create noti 
        //version 1: response when finished creating noti
        // update: like create many accounts func (UserService)
        var serviceResult = new ServiceResult();
        List<string> availableRole = new List<string>()
        {
            nameof(Role.Administration),
            nameof(Role.Librarian),
            nameof(Role.HeadLibrarian),
            nameof(Role.LibraryAssistant),
            nameof(Role.LibraryManager),
        };
        if (!availableRole.Contains(createBy) || createBy.IsNullOrEmpty())
        {
            serviceResult.ResultCode = ResultCodeConst.Auth_Warning0001;
            serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0001);
        }


        try
        {
            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(noti);
            // Check for valid validations
            if (validationResult != null && !validationResult.IsValid)
            {
                // Convert ValidationResult to ValidationProblemsDetails.Errors
                var errors = validationResult.ToProblemDetails().Errors;
                throw new UnprocessableEntityException("Invalid Validations", errors);
            }

            var notiEntity = _mapper.Map<Notification>(noti);
            await _unitOfWork.Repository<Notification, Guid>().AddAsync(notiEntity);
            if (await _unitOfWork.SaveChangesAsync() > 0)
            {
                if (noti.IsPublic == true)
                {
                    serviceResult.ResultCode = ResultCodeConst.SYS_Success0001;
                    serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001);
                    serviceResult.Data = true;
                }
                else
                {
                    foreach (var recipient in recipients)
                    {
                        var userResult = await _userService.GetByEmailAsync(recipient);
                        var user = (UserDto)userResult.Data!;
                        var privateNoti = new NotificationRecipientDto()
                        {
                            IsRead = false,
                            NotificationId = notiEntity.NotificationId,
                            RecipientId = user.UserId
                        };
                        var addPrivateNotiResult = await _notificationRecipient.CreatePrivateNotification(privateNoti);
                        if (addPrivateNotiResult.ResultCode == ResultCodeConst.SYS_Success0001)
                        {
                            await SendPrivateNotification(recipient, noti);
                            serviceResult.ResultCode = ResultCodeConst.SYS_Success0001;
                            serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001);
                            serviceResult.Data = true;
                        }
                        else
                        {
                            serviceResult.ResultCode = ResultCodeConst.SYS_Fail0001;
                            serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001);
                            serviceResult.Data = false;
                        }
                    }
                }
            }
            else
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Fail0001;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001);
                serviceResult.Data = false;
            }
        }
        catch (UnprocessableEntityException e)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke while create notification");
        }

        return serviceResult;
    }

    public async Task<IServiceResult> GetTypes()
    {
        var result = Enum.GetNames(typeof(NotificationType));
        return new ServiceResult(ResultCodeConst.SYS_Success0002,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), new { NotificationType = result });
    }
    
    
    private async Task SendPrivateNotification(string userId, NotificationDto dto)
    {
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", new
            {
                Title = dto.Title,
                Message = dto.Message,
                IsPublic = false,
                Timestamp = dto.CreateDate,
                NotificationType = dto.NotificationType
            });
    }
}