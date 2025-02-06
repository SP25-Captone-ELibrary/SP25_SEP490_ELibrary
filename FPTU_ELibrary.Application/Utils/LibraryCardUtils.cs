namespace FPTU_ELibrary.Application.Utils;

public static class LibraryCardUtils
{
    public static string GenerateBarcode(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid().ToString("N").Substring(20).ToUpper()}";
    }
}