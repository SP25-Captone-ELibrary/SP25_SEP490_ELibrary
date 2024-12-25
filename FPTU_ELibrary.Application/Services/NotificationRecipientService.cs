using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Notifications;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class NotificationRecipientService : GenericService<NotificationRecipient, NotificationRecipientDto, int>
    , INotificationRecipientService<NotificationRecipientDto>
{
    private readonly IUserService<UserDto> _userService;

    public NotificationRecipientService(ILogger logger,
        ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IUserService<UserDto> userService) : base(msgService, unitOfWork, mapper, logger)
    {
        _userService = userService;
    }

    public async Task<IServiceResult> CreatePrivateNotification(NotificationRecipientDto notification)
    {
        // Initiate service result
        var serviceResult = new ServiceResult();

        try
        {
            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(notification);
            // Check for valid validations
            if (validationResult != null && !validationResult.IsValid)
            {
                // Convert ValidationResult to ValidationProblemsDetails.Errors
                var errors = validationResult.ToProblemDetails().Errors;
                throw new UnprocessableEntityException("Invalid Validations", errors);
            }

            // Process add new entity
            await _unitOfWork.Repository<NotificationRecipient, int>()
                .AddAsync(_mapper.Map<NotificationRecipient>(notification));
            // Save to DB
            if (await _unitOfWork.SaveChangesAsync() > 0)
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Success0001;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001);
                serviceResult.Data = notification;
            }
            else
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Fail0001;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001);
                serviceResult.Data = false;
            }
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception(ex.Message);
        }

        return serviceResult;
    }

    public async Task<IServiceResult> GetNumberOfUnreadNotifications(string email)
    {
        var userResult = await _userService.GetByEmailAsync(email);
        var user = (UserDto)userResult.Data!;

        var baseSpec = new BaseSpecification<NotificationRecipient>(n => n.RecipientId == user.UserId
                                                                         && n.IsRead == false);
        // Include notification role
        baseSpec.ApplyInclude(q =>
            q.Include(u => u.Notification));
        var noti = await _unitOfWork.Repository<NotificationRecipient, int>().GetAllWithSpecAsync(baseSpec);
        if (!noti.Any())
        {
            var errorMsg = await _msgService.GetMessageAsync(ResultCodeConst.Notification_Warning0001);
            return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                StringUtils.Format(errorMsg, "notification-recipient"));
        }

        return new ServiceResult(ResultCodeConst.SYS_Success0002,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),noti.Count());
    }

    public async Task<IServiceResult> UpdateReadStatus(string email)
    {
        var userResult = await _userService.GetByEmailAsync(email);
        var user = (UserDto)userResult.Data!;

        var baseSpec = new BaseSpecification<NotificationRecipient>(n => n.RecipientId == user.UserId
                                                                         && n.IsRead == false);
        // Include notification role
        baseSpec.ApplyInclude(q =>
            q.Include(u => u.Notification));
        var notifications = await _unitOfWork.Repository<NotificationRecipient, int>().GetAllWithSpecAsync(baseSpec);
        if (!notifications.Any())
        {
            var errorMsg = await _msgService.GetMessageAsync(ResultCodeConst.Notification_Warning0001);
            return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                StringUtils.Format(errorMsg, "notification recipient"));
        }

        foreach (var noti  in notifications)
        {
            noti.IsRead = true;
            await _unitOfWork.Repository<NotificationRecipient, int>().UpdateAsync(noti);
        }

        var result =await _unitOfWork.SaveChangesWithTransactionAsync();
        if (result != -1)
        {
            return new ServiceResult(ResultCodeConst.SYS_Success0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), data: true);
        }

        return new ServiceResult(ResultCodeConst.SYS_Fail0003,
            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
    }
}