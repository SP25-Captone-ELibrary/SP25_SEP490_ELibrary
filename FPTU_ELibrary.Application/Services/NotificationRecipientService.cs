using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Notifications;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class NotificationRecipientService : GenericService<NotificationRecipient, NotificationRecipientDto, int>,
    INotificationRecipientService<NotificationRecipientDto>
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

    public async Task<IServiceResult> GetNumberOfUnreadNotificationsAsync(string email)
    {
        try
        {
            var userDto = (await _userService.GetByEmailAsync(email)).Data as UserDto;
            if (userDto == null) throw new ForbiddenException("Not allow to access");

            // Build spec
            var baseSpec = new BaseSpecification<NotificationRecipient>(n => 
                n.RecipientId == userDto.UserId && n.IsRead == false);
            // Include notification role
            baseSpec.ApplyInclude(q =>
                q.Include(u => u.Notification));
        
            // Return with count number
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), 
                await _unitOfWork.Repository<NotificationRecipient, int>().CountAsync(baseSpec));
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get number of unread notifications");
        }
    }

    public async Task<IServiceResult> UpdateReadStatusAsync(string email)
    {
        try
        {
            // Check exist user
            var user = (await _userService.GetByEmailAsync(email)).Data as UserDto;
            if (user == null) throw new ForbiddenException("Not allow to access");

            // Build spec
            var baseSpec = new BaseSpecification<NotificationRecipient>(n => n.RecipientId == user.UserId
                                                                             && n.IsRead == false);
            // Include notification role
            baseSpec.ApplyInclude(q =>
                q.Include(u => u.Notification));
            // Retrieve notification with spec
            var notifications =
                await _unitOfWork.Repository<NotificationRecipient, int>().GetAllWithSpecAsync(baseSpec);
            // Convert to list
            var notificationList = notifications.ToList();
            if (!notificationList.Any())
            {
                // Mark as update success
                return new ServiceResult(ResultCodeConst.SYS_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), data: true);
            }

            foreach (var noti in notificationList)
            {
                noti.IsRead = true;
                await _unitOfWork.Repository<NotificationRecipient, int>().UpdateAsync(noti);
            }

            var result = await _unitOfWork.SaveChangesWithTransactionAsync();
            if (result != -1)
            {
                return new ServiceResult(ResultCodeConst.SYS_Success0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), data: true);
            }

            return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update read status");
        }
    }
}