using System.Security.Claims;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.Notification;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.Notifications;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class NotificationController : ControllerBase
{
    private readonly AppSettings _appSettings;
    private readonly INotificationService<NotificationDto> _notificationService;
    private readonly INotificationRecipientService<NotificationRecipientDto> _notificationRecipientService;

    public NotificationController(
        INotificationService<NotificationDto> notificationService,
        INotificationRecipientService<NotificationRecipientDto> notificationRecipientService,
        IOptionsMonitor<AppSettings> monitor)
    {
        _appSettings = monitor.CurrentValue;
        _notificationService = notificationService;
        _notificationRecipientService = notificationRecipientService;
    }

    #region Management
    [Authorize]
    [HttpPost(APIRoute.Notification.Create, Name=nameof(CreateNotificationAsync))]
    public async Task<IActionResult> CreateNotificationAsync([FromBody] CreateNotificationRequest req)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _notificationService.CreateNotificationAsync(
            createdByEmail: email ?? string.Empty, 
            dto: req.ToNotificationDto(),
            recipients: req.ListRecipient));
    }
    
    [Authorize] 
    [HttpGet(APIRoute.Notification.GetAll, Name = nameof(GetAllNotificationAsync))]
    public async Task<IActionResult> GetAllNotificationAsync([FromQuery] NotificationSpecParams specParams)
    {
        return Ok(await _notificationService.GetAllWithSpecAsync(new NotificationSpecification(
                specParams: specParams, 
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize)));
    }
    
    [Authorize]
    [HttpGet(APIRoute.Notification.GetById, Name = nameof(GetNotificationByIdAsync))]
    public async Task<IActionResult> GetNotificationByIdAsync([FromRoute] int id)
    {
        return Ok(await _notificationService.GetByIdAsync(id: id));
    }
    #endregion

    [Authorize] 
    [HttpGet(APIRoute.Notification.GetAllPrivacy, Name = nameof(GetAllPrivacyNotificationAsync))]
    public async Task<IActionResult> GetAllPrivacyNotificationAsync([FromQuery] NotificationSpecParams specParams)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _notificationService.GetAllPrivacyNotificationAsync(
            email: email ?? string.Empty, 
            spec: new NotificationSpecification(
                specParams: specParams, 
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize)));
    }
    
    [Authorize]
    [HttpGet(APIRoute.Notification.GetNumberOfUnreadNotifications, Name = nameof(GetNumberOfUnreadNotifications))]
    public async Task<IActionResult> GetNumberOfUnreadNotifications()
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _notificationRecipientService.GetNumberOfUnreadNotificationsAsync(email ?? string.Empty));
    }

    [Authorize]
    [HttpPut(APIRoute.Notification.UpdateReadStatus, Name = nameof(UpdateReadStatus))]
    public async Task<IActionResult> UpdateReadStatus()
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _notificationRecipientService.UpdateReadStatusAsync(email ?? string.Empty));
    }
    
    [Authorize]
    [HttpGet(APIRoute.Notification.GetPrivacyById, Name = nameof(GetPrivacyByIdAsync))]
    public async Task<IActionResult> GetPrivacyByIdAsync([FromRoute] int id)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _notificationService.GetPrivacyNotificationAsync(id: id, email: email ?? string.Empty));
    }
}