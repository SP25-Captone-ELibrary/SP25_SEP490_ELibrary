namespace FPTU_ELibrary.Application.Configurations;

public class RedisSettings
{
    public string Host { get; set; } = null!;
    public string Port { get; set; } = null!;
    public string LibraryItemCacheKey { get; set; } = null!;
}