using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class BookEditionInventory
{
    // Key
    public int BookEditionId { get; set; }

    // Inventory amount 
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public int RequestCopies { get; set; }
    public int ReservedCopies { get; set; }

    // Mapping entity
    [JsonIgnore]
    public BookEdition BookEdition { get; set; } = null!;
}
