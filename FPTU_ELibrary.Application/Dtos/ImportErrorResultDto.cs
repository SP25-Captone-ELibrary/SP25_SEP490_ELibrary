namespace FPTU_ELibrary.Application.Dtos;

public class ImportErrorResultDto
{
    public int WorkSheetIndex { get; set; } = 1;
    public int RowNumber { get; set; } 
    public List<string> Errors { get; set; } = new();
}
