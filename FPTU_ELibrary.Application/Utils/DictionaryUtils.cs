namespace FPTU_ELibrary.Application.Utils;

public class DictionaryUtils
{
    public static Dictionary<string, string[]> AddOrUpdate(Dictionary<string, string[]> customErrors, string key, string msg)
    {
        // Add error 
        if (customErrors.ContainsKey(key)) customErrors[key] = customErrors[key].Concat([msg]).ToArray();
        else customErrors[key] = [msg];
        
        return customErrors;
    }
}