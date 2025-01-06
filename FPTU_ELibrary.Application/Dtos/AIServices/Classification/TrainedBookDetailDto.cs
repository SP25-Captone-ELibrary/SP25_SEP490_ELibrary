using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Application.Dtos.AIServices.Classification;

public class TrainedBookDetailDto
{
    public int BookEditionId { get; set; }
    public List<IFormFile> ImageList { get; set; }  
}