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

public class UpdateKeyVaultDto
{
    public string FullFormatKey { get; set; }
    public string Value { get; set; }
}