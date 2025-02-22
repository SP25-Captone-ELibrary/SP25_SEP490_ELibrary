using System.Globalization;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Specifications.Params;

public class WarehouseTrackingSpecParams : BaseSpecParams
{
    public int? SupplierId { get; set; }
    public TrackingType? TrackingType { get; set; }
    public WarehouseTrackingStatus? Status { get; set; }

    public int?[]? TotalItemRange { get; set; }
    public decimal?[]? TotalAmountRange { get; set; }
    public DateTime?[]? EntryDateRange { get; set; } 
    public DateTime?[]? ExpectedReturnDateRange { get; set; } 
    public DateTime?[]? ActualReturnDateRange { get; set; } 
    public DateTime?[]? CreatedAtRange { get; set; } 
    public DateTime?[]? UpdatedAtRange { get; set; }
    
    public DateTime? ParsedSearchDate 
    {
        get
        {
            string[] formats = 
            {
                "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy", 
                "yyyy/MM/dd", "dd-MM-yyyy", "MM-dd-yyyy",
                "yyyyMMdd", "ddMMyyyy", "MMddyyyy"
            };

            if (DateTime.TryParseExact(Search, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                return parsedDate;
            }
            return null;
        }
    }
}
