using System.Text.RegularExpressions;

namespace FPTU_ELibrary.Application.Utils;

public class DeweyDecimalUtils
{
    // Validate DCC number
    public static bool IsValidDeweyDecimal(string classificationNumber)
    {
        if (string.IsNullOrWhiteSpace(classificationNumber))
            return false;

        classificationNumber = classificationNumber.Trim();

        var regex = new Regex(@"^\d{1,3}(\.\d{1,10})?$", RegexOptions.Compiled);
        return regex.IsMatch(classificationNumber);
    }

    // Validate Cutter number
    public static bool IsValidCutterNumber(string cutterNumber)
    {
        // Regex for Cutter Numbers
        // Accepts one or two Unicode letters, 1-4 digits,
        // and an optional Unicode letter at the end
        var regex = new Regex(@"^[\p{L}]{1,2}\d{1,4}(\.\d+)?[\p{L}]?$", RegexOptions.IgnoreCase);
        return regex.IsMatch(cutterNumber);
    }
    
    /// <summary>
    /// Given a Dewey Decimal number (0-999), returns the lower and upper boundaries of its range
    /// + 000-099: Computer science, information & general works
    /// + 100-199: Philosophy & Psychology
    /// + 200-299: Religion
    /// + 300-399: Social sciences
    /// + 400-499: Language
    /// + 500-599: Natural sciences and mathematics
    /// + 600-699: Technology
    /// + 700-799: Arts & Recreation
    /// + 800-899: Literature 
    /// + 900-999: History & Geography
    /// </summary>
    private static (int lower, int upper) GetDDCRange(int num)
    {
        if (num < 0 || num > 999) return (0, 0);
        
        int lower = (num / 100) * 100;
        int upper = lower + 99;
        
        return (lower, upper);
    }

    /// <summary>
    /// Converts a DDC string to an integer
    /// </summary>
    private static int? ConvertDDCStringToNumber(string ddcStr)
    {
        if (string.IsNullOrWhiteSpace(ddcStr)) return null;
        
        // Split the string on the decimal point
        string[] parts = ddcStr.Split('.');
        
        // Parse the integer part
        int number = int.Parse(parts[0]);
        return number;
    }
    
    /// <summary>
    /// Compares two DDC strings
    /// </summary>
    public static bool IsDDCWithinRange(string? ddc, string?[] ddcList)
    {
        if(ddc == null || !ddcList.Any()) return false;
        
        // Convert DDC string to integer value
        var baseNumber = ConvertDDCStringToNumber(ddc);
        // Try parse to integer
        if(!int.TryParse(baseNumber.ToString(), out var validBaseNumber)) return false;
        // Extract lower boundary range of base number
        var baseLower = GetDDCRange(validBaseNumber).lower;
        
        // Iterate each ddc range
        foreach (var dStr in ddcList)
        {
            // Skip when not exist ddc
            if(dStr == null) continue;
            
            // Convert DDC strings to integer value
            var currentNumber = ConvertDDCStringToNumber(dStr);
            // Try parse current number
            if(!int.TryParse(currentNumber.ToString(), out var validCurrNum)) continue;
            // Retrieve lower boundary range of current number
            var currentLower = GetDDCRange(validCurrNum).lower;
            
            // Compare lower value
            if(baseLower == currentLower) return true;
        }

        return false;
    }
}