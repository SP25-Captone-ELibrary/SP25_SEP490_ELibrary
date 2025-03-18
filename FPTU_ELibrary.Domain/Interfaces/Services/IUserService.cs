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
        Task<IServiceResult> GetPendingLibraryActivityAsync(Guid libraryCardId);
        Task<IServiceResult> GetPendingLibraryActivitySummaryAsync(Guid libraryCardId);
        Task<IServiceResult> GetByEmailAndPasswordAsync(string email, string password);
        Task<IServiceResult> GetByEmailAsync(string email);
        Task<IServiceResult> CreateManyAccountsWithSendEmail(string email, IFormFile? excelFile, DuplicateHandle duplicateHandle, bool isSendEmail = false);
        Task<IServiceResult> CreateAccountByAdminAsync(TDto dto);
        Task<IServiceResult> UpdateProfileAsync(string email, TDto dto);
        Task<IServiceResult> UpdateRoleAsync(Guid userId, int roleId);
        Task<IServiceResult> UpdateWithoutValidationAsync(Guid userId, TDto dto);
        Task<IServiceResult> UpdateEmailVerificationCodeAsync(Guid userId, string code);
        Task<IServiceResult> ChangeActiveStatusAsync(Guid userId);
        Task<IServiceResult> UpdateMfaSecretAndBackupAsync(string email, string mfaKey, IEnumerable<string> backupCodes);
        Task<IServiceResult> UpdateMfaStatusAsync(Guid userId);
        Task<IServiceResult> UpdatePasswordWithoutSaveChangesAsync(Guid userId, string password);
        Task<IServiceResult> SoftDeleteAsync(Guid userId);
        Task<IServiceResult> SoftDeleteRangeAsync(Guid[] userIds);
        Task<IServiceResult> UndoDeleteAsync(Guid userId);
        Task<IServiceResult> UndoDeleteRangeAsync(Guid[] userIds);
        Task<IServiceResult> DeleteRangeAsync(Guid[] userIds);
        Task<IServiceResult> ExportAsync(ISpecification<User> spec);

        #region Library card holders
        Task<IServiceResult> CreateLibraryCardHolderAsync(
            string createdByEmail, TDto dto, 
            TransactionMethod transactionMethod, int? paymentMethodId, int libraryCardPackageId);
        Task<IServiceResult> UpdateLibraryCardHolderAsync(Guid userId, TDto dto);
        Task<IServiceResult> GetAllLibraryCardHolderAsync(ISpecification<User> spec);
        Task<IServiceResult> GetLibraryCardHolderByIdAsync(Guid userId);
        Task<IServiceResult> GetLibraryCardHolderByBarcodeAsync(string barcode);
        Task<IServiceResult> SoftDeleteLibraryCardHolderAsync(Guid userId);
        Task<IServiceResult> SoftDeleteRangeLibraryCardHolderAsync(Guid[] userIds);
        Task<IServiceResult> UndoDeleteLibraryCardHolderAsync(Guid userId);
        Task<IServiceResult> UndoDeleteRangeLibraryCardHolderAsync(Guid[] userIds);
        Task<IServiceResult> DeleteLibraryCardHolderAsync(Guid userId);
        Task<IServiceResult> DeleteRangeLibraryCardHolderAsync(Guid[] userIds);
        Task<IServiceResult> DeleteLibraryCardWithoutSaveChangesAsync(Guid userId);
        Task<IServiceResult> ImportLibraryCardHolderAsync(IFormFile? file,
            List<IFormFile>? avatarImageFiles,
            string[]? scanningFields, 
            DuplicateHandle? duplicateHandle = null);
        Task<IServiceResult> ExportLibraryCardHolderAsync(ISpecification<User> spec);
        #endregion
    }
}
