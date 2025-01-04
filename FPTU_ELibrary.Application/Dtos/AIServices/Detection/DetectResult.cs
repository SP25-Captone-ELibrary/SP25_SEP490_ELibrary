namespace FPTU_ELibrary.Application.Dtos.AIServices.Detection;

public class DetectResultDto
{
    public BoxDto Box {get;}
    public string Name { get;}    
}

public class BoxDto
{
    public double X1 { get; }

    public double X2 { get; }

    public double Y1 { get; }

    public double Y2 { get; }
}
public class DetectedImageDto
{
    public List<DetectResultDto> Results { get; set; }
}

public class DetectResponseDto
{
    public List<DetectedImageDto> Images { get; set; }
}