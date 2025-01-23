namespace FPTU_ELibrary.Application.Dtos.AIServices;

public class CheckDuplicateImageDto<T> where T : notnull
{
    public List<ObjectMatchResultDto<T>> ObjectMatchResult { get; set; }
    public List<MatchResultDto> OCRResult { get; set; }
}
public class ObjectMatchResultDto<T>  where T : notnull
{
    public string ImageName { get; set; }
    public List<BaseObjectMatchResultDto<T>> ObjectMatchResults { get; set; }
}

public class BaseObjectMatchResultDto<T> where T : notnull
{
    public T? ObjectType { get; set; }
    public int NumberOfObject { get; set; }
    public bool IsPassed { get; set; }
}