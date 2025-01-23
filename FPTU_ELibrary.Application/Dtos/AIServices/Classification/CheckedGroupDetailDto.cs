namespace FPTU_ELibrary.Application.Dtos.AIServices.Classification;

public class CheckedGroupDetailDto<T> where T : notnull
{
    public int ItemId { get; set; }
    public IDictionary<T, bool> PropertiesChecked = new Dictionary<T, bool>();
}