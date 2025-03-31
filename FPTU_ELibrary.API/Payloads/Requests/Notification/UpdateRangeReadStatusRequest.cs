namespace FPTU_ELibrary.API.Payloads.Requests.Notification;

public class UpdateRangeReadStatusRequest
{
    public List<int> NotificationIds { get; set; } = new();
}