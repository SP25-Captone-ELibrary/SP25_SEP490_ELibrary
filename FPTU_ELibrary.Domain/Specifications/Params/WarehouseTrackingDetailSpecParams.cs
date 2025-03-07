using System.Globalization;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Domain.Specifications.Params;

public class WarehouseTrackingDetailSpecParams : BaseSpecParams
{
    // Filter fields
    public bool? HasGlueBarcode { get; set; }
    
    #region Basic search properties
    public string? ItemName { get; set; }
    public int? ItemTotal { get; set; }
    public string? Isbn { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? SupplierName { get; set; }
    public SupplierType? SupplierType { get; set; }
    public StockTransactionType? StockTransactionType { get; set; }
    #endregion
    
    // Advanced filter fields:
    // ItemTotal - number
    // Isbn - text
    // UnitPrice - number
    // TotalAmount - number
    // Stock transaction type - multiple (pass enum index)
    // Category - multiple (pass ID)
    // LibraryItemCondition - multiple (pass ID)
    // Supplier - multiple (pass ID)
    // CreatedAt - date range
    // UpdatedAt - date range
    public string[]? F { get; set; } 
    public FilterOperator[]? O { get; set; } // Operators
    public string[]? V { get; set; } // Values
    
    // Search type
    public SearchType SearchType { get; set; }
    
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

    public decimal? ParsedSearchDecimal
    {
        get
        {
            if (decimal.TryParse(Search, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedDecimal))
            {
                // Rounds a decimal value to 2 
                decimal roundedValue = Math.Round(parsedDecimal, 2);
            
                // Add decimal(10,2) limitation
                const decimal maxAllowed = 99999999.99m;
                const decimal minAllowed = -99999999.99m;
            
                // Check if any exceed or smaller than limit values
                if (roundedValue > maxAllowed || roundedValue < minAllowed)
                {
                    return 0;
                }
            
                return roundedValue;
            }
            return null;
        }
    }
}