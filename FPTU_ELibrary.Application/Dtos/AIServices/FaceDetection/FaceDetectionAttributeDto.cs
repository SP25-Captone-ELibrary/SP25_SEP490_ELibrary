using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Application.Dtos.AIServices.FaceDetection;

// [Face++ - Facial Detection](https://console.faceplusplus.com/documents/5679127)
public class FaceDetectionAttributeDto
{
    [JsonPropertyName("gender")]
    public object? Gender { get; set; } = null!;
    
    [JsonPropertyName("age")]
    public object? Age { get; set; }
    
    [JsonPropertyName("smile")]
    public object? Smile { get; set; }
    
    [JsonPropertyName("glass")]
    public object? Glass { get; set; }
    
    [JsonPropertyName("head_pose")]
    public object? HeadPose { get; set; }
    
    [JsonPropertyName("blur")]
    public object? Blur { get; set; }
    
    [JsonPropertyName("eye_status")]
    public object? EyeStatus { get; set; }
    
    [JsonPropertyName("emotion")]
    public object? Emotion { get; set; }
    
    [JsonPropertyName("face_quality")]
    public object? FaceQuality { get; set; }
    
    [JsonPropertyName("beauty")]
    public object? Beauty { get; set; }
    
    [JsonPropertyName("mouth_status")]
    public object? MouthStatus { get; set; }
    
    [JsonPropertyName("eye_gaze")]
    public object? EyeGaze { get; set; }
    
    [JsonPropertyName("skin_status")]
    public object? SkinStatus { get; set; }
    
}

