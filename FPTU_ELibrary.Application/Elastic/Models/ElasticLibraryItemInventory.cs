using Nest;

namespace FPTU_ELibrary.Application.Elastic.Models;

public class ElasticLibraryItemInventory
{
    [Number(NumberType.Integer, Name = "library_item_id")]
    public int LibraryItemId { get; set; }
    
    [Number(NumberType.Integer, Name = "total_units")]
    public int TotalUnits { get; set; }
    
    [Number(NumberType.Integer, Name = "available_units")]
    public int AvailableUnits { get; set; }
    
    [Number(NumberType.Integer, Name = "request_units")]
    public int RequestUnits { get; set; }
    
    [Number(NumberType.Integer, Name = "borrowed_units")]
    public int BorrowedUnits { get; set; }
    
    [Number(NumberType.Integer, Name = "reserved_units")]
    public int ReservedUnits { get; set; }
}