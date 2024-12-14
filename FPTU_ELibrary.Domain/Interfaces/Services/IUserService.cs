using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Http;


namespace FPTU_ELibrary.Domain.Interfaces.Services
{
    public interface IUserService<TDto> : IGenericService<User, TDto, Guid>
        where TDto : class
    {
        Task<IServiceResult> GetByEmailAndPasswordAsync(string email, string password);
        Task<IServiceResult> GetByEmailAsync(string email);
        Task<IServiceResult> CreateAccountByAdmin(TDto user);
        Task<IServiceResult> UpdateRoleAsync(Guid userId, int roleId);
        Task<IServiceResult> UpdateWithoutValidationAsync(Guid userId, TDto dto);
        Task<IServiceResult> UpdateEmailVerificationCodeAsync(Guid userId, string code);
        Task<IServiceResult> UpdateAccount(Guid userId, TDto userUpdateDetail,string roleName);
        Task<IServiceResult> UpdateMfaSecretAndBackupAsync(string email, string mfaKey, IEnumerable<string> backupCodes);
        Task<IServiceResult> UpdateMfaStatusAsync(Guid userId);
        Task<IServiceResult> DeleteAccount(Guid id);
        Task<IServiceResult> ChangeAccountStatus(Guid userId);
        
        #region Background tasks
        Task CreateManyAccountsWithSendEmail(IFormFile excelFile);
        #endregion
    }
}
