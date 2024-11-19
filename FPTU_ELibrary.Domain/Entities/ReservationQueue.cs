using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class ReservationQueue
{
    // Key
    public int QueueId { get; set; }
    
    // For which book edition 
    public int BookEditionId { get; set; }

    // Forcasting available datetime
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
    public BookEdition BookEdition { get; set; } = null!;

    [JsonIgnore]
    public User ReservedByNavigation { get; set; } = null!;
}
