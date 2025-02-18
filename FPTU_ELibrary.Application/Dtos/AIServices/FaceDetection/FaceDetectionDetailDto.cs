using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Application.Dtos.AIServices.FaceDetection;

public class FaceDetectionDetailDto
{
    [JsonPropertyName("face_token")]
    public string FaceToken { get; set; } = null!;
    
    [JsonPropertyName("face_rectangle")]
    public object FaceRectangle { get; set; } = null!;
    
    [JsonPropertyName("landmark")]
    public object? Landmark { get; set; } = null!;
    
    [JsonPropertyName("attributes")]
    public FaceDetectionAttributeDto? Attributes { get; set; } = null!;
}