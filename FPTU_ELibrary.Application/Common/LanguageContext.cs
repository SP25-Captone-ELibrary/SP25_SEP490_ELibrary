namespace FPTU_ELibrary.Application.Common;

//  Summary:
//      This class is to manage request-specific data in a thread-safe
//      and context-aware manner
public static class LanguageContext
{
    //  Using AsyncLocal as it allow store data specific to the current 
    //  execution context
    //  Thread safety: requests might be processed on different threads, 
    //  AsyncLocal ensures the language data stays consistent for a request
    private static readonly AsyncLocal<string> _currentLanguage = new();

    public static string CurrentLanguage
    {
        get => _currentLanguage.Value ?? "en";
        set => _currentLanguage.Value = value;
    }
}