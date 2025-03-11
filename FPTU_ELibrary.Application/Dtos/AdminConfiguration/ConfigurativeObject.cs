namespace FPTU_ELibrary.Application.Dtos.AdminConfiguration;

public class ConfigurativeObject
{
    public string Name { get; set; }
    public List<ObjectKeyValuePair> ObjectKeyValuePairs { get; set; }
}
public class ObjectKeyValuePair
{
    public string Key { get; set; }
    public string Value { get; set; }
    public int Type { get; set; }
}
public class ConfigurativeObjectDetail
{
    public string Name { get; set; }
    public string Key { get; set; }
    public string Value { get; set; }
    public int Type { get; set; }
}

public class UpdateListKeyVaultDto
{
    public List<UpdateKeyVaultDto> UpdateKeyVaultDtos { get; set; }
}
public class UpdateKeyVaultDto
{
    public string FullFormatKey { get; set; }
    public string Value { get; set; }
}
public static class UpdateListKeyVaultDtoExtensions
{
    public static IDictionary<string,string> ToUpdateKeyVaultDtos(this UpdateListKeyVaultDto dto)
    {
        var result = new Dictionary<string, string>();
        foreach (var item in dto.UpdateKeyVaultDtos)
        {
            result.Add(item.FullFormatKey, item.Value);
        }

        return result;
    }
}