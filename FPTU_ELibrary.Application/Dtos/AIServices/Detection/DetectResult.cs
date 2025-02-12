using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace FPTU_ELibrary.Application.Dtos.AIServices.Detection;

public class DetectResponseDto
{
    [JsonProperty("images")]
    public List<DetectedImageDto> Images { get; set; }
}

public class DetectedImageDto
{
    [JsonProperty("results")]
    public List<DetectResultDto> Results { get; set; }
}

public class DetectResultDto
{
    [JsonProperty("box")]
    public BoxDto Box { get; set; } // Set { get; set; } để có thể gán giá trị.

    [JsonProperty("name")]
    public string Name { get; set; }
    [JsonProperty("confidence")] 
    public double Confidence { get; set; } = 0;
}
public class RawDetectResponseDto
{
    [JsonProperty("images")]
    public List<RawDetectionResultResponse> Images { get; set; }
}

public class BoxDto
{
    [JsonProperty("x1")]
    public double X1 { get; set; } // Set { get; set; } để có thể gán giá trị.

    [JsonProperty("x2")]
    public double X2 { get; set; }

    [JsonProperty("y1")]
    public double Y1 { get; set; }

    [JsonProperty("y2")]
    public double Y2 { get; set; }
}
public class RawDetectionResultResponse
{
    public List<ObjectInfoDto> ImportImageDetected { get; set; }
    public List<ObjectInfoDto> CurrentItemDetected { get; set; }
}

public class ObjectInfoDto
{
    public string Name { get; set; }
    public double Percentage { get; set; }
}
