using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.Notifications;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Hubs;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using FPTU_ELibrary.Domain.Specifications.Params;
using MapsterMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace FPTU_ELibrary.Application.Services.IServices;

public class NotificationService : GenericService<Notification, NotificationDto, int>,
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
        IUserService<UserDto> userService
        ) : base(msgService, unitOfWork,
        mapper, logger)
    {
        _hubContext = hubContext;
        _notificationRecipient = notificationRecipient;
        _userService = userService;
    }

    public async Task<IServiceResult> CreateNotification(NotificationDto noti,
        List<string>? recipients)
    {
        //create parallel thread to handle create noti 
        //version 1: response when finished creating noti
        // update: like create many accounts func (UserService)
        var serviceResult = new ServiceResult();
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
            await _unitOfWork.Repository<Notification, int>().AddAsync(notiEntity);
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
                            await SendPrivateNotification(recipient, _mapper.Map<NotificationDto>(notiEntity));
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

    public async Task<IServiceResult> GetById(string email, int id)
    {
        var userResult = await _userService.GetByEmailAsync(email);
        var user = (UserDto)userResult.Data!;
        //check if the 
        var recipientBaseSpec = new BaseSpecification<NotificationRecipient>(nr
            => nr.RecipientId == user.UserId
               && nr.NotificationId == id);
        
        //check the existed noti
        var baseSpec = new BaseSpecification<Notification>(q => q.NotificationId == id);
        baseSpec.ApplyInclude(q => q.Include(n => n.NotificationRecipients));
        // find the suitble noti
        var noti = await _unitOfWork.Repository<Notification, int>().GetWithSpecAsync(baseSpec);
        if (noti is null)
        {
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
        }
        if (!noti.IsPublic &&
                 await _unitOfWork.Repository<NotificationRecipient, int>()
                     .GetWithSpecAsync(recipientBaseSpec) is null)
        {
            return new ServiceResult(ResultCodeConst.Notification_Warning0002,
                await _msgService.GetMessageAsync(ResultCodeConst.Notification_Warning0002));
        }

        return new ServiceResult(ResultCodeConst.SYS_Success0002,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), noti);
    }


    private async Task SendPrivateNotification(string userId, NotificationDto dto)
    {
        await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", new
        {
            NotificationId = dto.NotificationId,
            Title = dto.Title,
            Message = dto.Message,
            IsPublic = false,
            Timestamp = dto.CreateDate,
            NotificationType = dto.NotificationType
        });
    }

    public async Task<IServiceResult> GetAllWithSpecAsync(
        NotificationSpecParams specParams,
        string email,
        bool tracked = true
    )
    {
        try
        {
            var userResult = await _userService.GetByEmailAsync(email);
            var user = (UserDto)userResult.Data!;
            var roleId = user.RoleId;

            // Tạo mới NotificationSpecification dựa trên specParams, email, và roleId
            var notificationSpec = new NotificationSpecification(
                specParams,
                specParams.PageIndex ?? 1,
                specParams.PageSize ?? 5,
                email,
                roleId
            );

            // Count total actual items in DB
            var totalActualItem = await _unitOfWork.Repository<Notification, int>().CountAsync();
            var totalNotification = await _unitOfWork.Repository<Notification, int>().CountAsync(notificationSpec);
            var totalPage = (int)Math.Ceiling((double)totalNotification / notificationSpec.PageSize);

            if (notificationSpec.PageIndex > totalPage || notificationSpec.PageIndex < 1)
            {
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                    new PaginatedResultDto<NotificationDto>( new List<NotificationDto>(),
                        notificationSpec.PageIndex, notificationSpec.PageSize, totalPage, totalActualItem)
                );
            }

            notificationSpec.ApplyPaging(
                skip: notificationSpec.PageSize * (notificationSpec.PageIndex - 1),
                take: notificationSpec.PageSize
            );

            var entities = await _unitOfWork.Repository<Notification, int>()
                .GetAllWithSpecAsync(notificationSpec, tracked);

            if (entities.Any())
            {
                var paginationResultDto = new PaginatedResultDto<NotificationDto>(
                    _mapper.Map<IEnumerable<NotificationDto>>(entities),
                    notificationSpec.PageIndex, notificationSpec.PageSize, totalPage, totalActualItem);

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
}