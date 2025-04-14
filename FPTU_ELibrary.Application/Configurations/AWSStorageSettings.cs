namespace FPTU_ELibrary.Application.Configurations;

public class AWSStorageSettings
{
    public string AccessKey { get; set; } = null!;
    public string SecretKey { get; set; } = null!;
    public string BucketName { get; set; } = null!;
    public string Region { get; set; } = null!;
}