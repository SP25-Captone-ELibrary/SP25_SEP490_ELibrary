using System.Security.Claims;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.Notification;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Notifications;
using FPTU_ELibrary.Application.Services;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

public class NotificationController: ControllerBase
{
    private readonly INotificationService<NotificationDto> _notificationService;
    private readonly INotificationRecipientService<NotificationRecipientDto> _notificationRecipientService;

    public NotificationController(INotificationService<NotificationDto> notificationService,
        INotificationRecipientService<NotificationRecipientDto> notificationRecipientService)
    {
        _notificationService = notificationService;
        _notificationRecipientService = notificationRecipientService;
    }

    [Authorize]
    [HttpPost(APIRoute.Notification.Create,Name=nameof(CreateNotification))]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationRequest req)
    {
        // Retrieve user email from token
        var roleName = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        var dto = req.ToNotificationDto();
        return Ok(await _notificationService.CreateNotification(dto,roleName, req.ListRecipient));
    }

    [AllowAnonymous]
    [HttpGet(APIRoute.Notification.GetTypes,Name= nameof(GetTypes))]
    public async Task<IActionResult> GetTypes()
    {
        return Ok(await _notificationService.GetTypes());
    }

    [Authorize]
    [HttpGet(APIRoute.Notification.GetNumberOfUnreadNotifications, Name = nameof(GetNumberOfUnreadNotifications))]
    public async Task<IActionResult> GetNumberOfUnreadNotifications()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? ""; 
        return Ok(await _notificationRecipientService.GetNumberOfUnreadNotifications(email));
    }

    [Authorize]
    [HttpPut(APIRoute.Notification.UpdateReadStatus, Name = nameof(UpdateReadStatus))]
    public async Task<IActionResult> UpdateReadStatus()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? ""; 
        return Ok(await _notificationRecipientService.UpdateReadStatus(email));
    }

    [HttpGet(APIRoute.Notification.GetNotificationByAdmin, Name = nameof(GetAllNotification))]
    [Authorize]
    public async Task<IActionResult> GetAllNotification([FromQuery] NotificationSpecParams specParams)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
        return Ok(await _notificationService.GetAllWithSpecAsync(specParams, email));
    }
}