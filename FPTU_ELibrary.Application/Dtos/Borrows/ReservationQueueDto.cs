using System.Text.Json.Serialization;
using FPTU_ELibrary.Application.Dtos.LibraryItems;

namespace FPTU_ELibrary.Application.Dtos.Borrows;

public class ReservationQueueDto
{
    // Key
    public int QueueId { get; set; }
    
    // For which book edition 
    public int LibraryItemId { get; set; }

    // Forecasting available datetime
    public DateTime? ExpectedAvailableDate { get; set; }
    
    // Reservation detail
    public Guid ReservedBy { get; set; }
    public DateTime ReservationDate { get; set; }


    // Deposit detail
    public DateTime DepositExpirationDate { get; set; }
    public decimal? DepositFee { get; set; }
    public bool? DepositPaid { get; set; }

    // Queue status
    public string QueueStatus { get; set; } = null!;

    // Mapping entities
    [JsonIgnore]
    public LibraryItemDto LibraryItem { get; set; } = null!;

    [JsonIgnore]
    public UserDto ReservedByNavigation { get; set; } = null!;
}