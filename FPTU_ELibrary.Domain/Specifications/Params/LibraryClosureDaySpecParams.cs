using System.Globalization;

namespace FPTU_ELibrary.Domain.Specifications.Params;

public class LibraryClosureDaySpecParams : BaseSpecParams
{
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