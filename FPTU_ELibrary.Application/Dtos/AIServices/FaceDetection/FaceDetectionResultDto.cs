using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Application.Dtos.AIServices.FaceDetection;

public class FaceDetectionResultDto
{
    [JsonPropertyName("request_id")]
    public string RequestId { get; set; } = null!;
    
    [JsonPropertyName("faces")]
    public FaceDetectionDetailDto[] Faces { get; set; } = [];
    
    [JsonPropertyName("image_id")]
    public string ImageId { get; set; } = null!;
    
    [JsonPropertyName("time_used")]
    public int TimeUsed { get; set; }
    
    [JsonPropertyName("face_num")]
    public int FaceNum { get; set; }
    
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}