using Nest;

namespace FPTU_ELibrary.Application.Elastic.Models;

public class ElasticLibraryItemInstance
{
    [Number(NumberType.Integer, Name = "library_item_instance_id")]
    public int LibraryItemInstanceId { get; set; }
    
    [Number(NumberType.Integer, Name = "library_item_id")]
    public int LibraryItemId { get; set; }
    
    [Text(Name = "barcode")]
    public string Barcode { get; set; } = null!;
    
    [Keyword(Name = "status")]
    public string Status { get; set; } = null!;
    
    [Boolean(Name = "is_deleted")] 
    public bool IsDeleted { get; set; }
}