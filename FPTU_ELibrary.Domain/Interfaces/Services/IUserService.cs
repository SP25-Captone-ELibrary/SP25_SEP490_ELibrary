using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Http;


namespace FPTU_ELibrary.Domain.Interfaces.Services
{
    public interface IUserService<TDto> : IGenericService<User, TDto, Guid>
        where TDto : class
    {
        Task<IServiceResult> GetByEmailAndPasswordAsync(string email, string password);
        Task<IServiceResult> GetByEmailAsync(string email);
        Task<IServiceResult> CreateManyAccountsWithSendEmail(string email, IFormFile? excelFile, DuplicateHandle duplicateHandle, bool isSendEmail = false);
        Task<IServiceResult> CreateAccountByAdmin(TDto user);
        Task<IServiceResult> UpdateProfileAsync(string email, TDto user);
        Task<IServiceResult> UpdateRoleAsync(Guid userId, int roleId);
        Task<IServiceResult> UpdateWithoutValidationAsync(Guid userId, TDto user);
        Task<IServiceResult> UpdateEmailVerificationCodeAsync(Guid userId, string code);
        Task<IServiceResult> ChangeActiveStatusAsync(Guid userId);
        Task<IServiceResult> UpdateMfaSecretAndBackupAsync(string email, string mfaKey, IEnumerable<string> backupCodes);
        Task<IServiceResult> UpdateMfaStatusAsync(Guid userId);
        Task<IServiceResult> SoftDeleteAsync(Guid userId);
        Task<IServiceResult> SoftDeleteRangeAsync(Guid[] userIds);
        Task<IServiceResult> UndoDeleteAsync(Guid userId);
        Task<IServiceResult> UndoDeleteRangeAsync(Guid[] userIds);
        Task<IServiceResult> DeleteRangeAsync(Guid[] userIds);
        Task<IServiceResult> ExportAsync(ISpecification<User> spec);
    }
}
