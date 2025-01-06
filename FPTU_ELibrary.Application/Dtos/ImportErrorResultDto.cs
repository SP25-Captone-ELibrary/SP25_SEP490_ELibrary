namespace FPTU_ELibrary.Application.Dtos;

public class ImportErrorResultDto
{
    public int RowNumber { get; set; } 
    public List<string> Errors { get; set; } = new();
}
