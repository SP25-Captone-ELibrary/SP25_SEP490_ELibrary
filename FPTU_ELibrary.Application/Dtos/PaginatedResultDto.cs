namespace FPTU_ELibrary.Application.Dtos;

public class PaginatedResultDto<T> 
{
    public IEnumerable<T> Sources { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPage { get; set; }
    
    public PaginatedResultDto(IEnumerable<T> sources, int pageIndex, int pageSize, int totalPage)
    {
        Sources = sources;
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalPage = totalPage;
    }
}