namespace FPTU_ELibrary.API.Payloads.Requests.Reservation;

public class ConfirmApplyReservationLabelRequest
{
    public List<int> QueueIds { get; set; } = new();
}