namespace FPTU_ELibrary.Application.Configurations;

public class AzureSettings
{
    public string KeyVaultUrl { get; set; } = null!;
    public string KeyVaultClientId { get; set; } = null!;
    public string KeyVaultClientSecret { get; set; } = null!;
    public string KeyVaultDirectoryID { get; set; } = null!;
}