using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.Application.Dtos.AIServices.Classification;

public class CheckedGroupDetailDto<T> where T : notnull
{
    public int ItemId { get; set; }
    public bool IsRoot { get; set; } = false;
    public IDictionary<T, int> PropertiesChecked { get; set; } = new Dictionary<T, int>();
}

public class CheckedGroupResponseDto<T> where T : notnull
{
    public List<CheckedGroupDetailDto<T>> ListCheckedGroupDetail { get; set; } = new List<CheckedGroupDetailDto<T>>();
    public int IsAbleToCreateGroup { get; set; }
}