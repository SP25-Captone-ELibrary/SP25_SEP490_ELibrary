using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Application.Services.IServices;

public interface IAdminConfigurationService
{
    Task<IServiceResult> GetAllKeyVault();
    Task<IServiceResult> GetKeyVault(string key);
    Task<IServiceResult> UpdateKeyVault(IDictionary<string,string> keyValues);
}