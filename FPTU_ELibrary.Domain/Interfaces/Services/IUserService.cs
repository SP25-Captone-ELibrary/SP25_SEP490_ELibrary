using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.AspNetCore.Http;


namespace FPTU_ELibrary.Domain.Interfaces.Services
{
    public interface IUserService<TDto> : IGenericService<User, TDto, Guid>
        where TDto : class
    {
        Task<IServiceResult> GetByEmailAndPasswordAsync(string email, string password);
        Task<IServiceResult> GetByEmailAsync(string email);
        Task<IServiceResult> UpdateRoleAsync(int roleId, Guid userId);
        Task<IServiceResult> UpdateWithoutValidationAsync(Guid userId, TDto dto);
        Task<IServiceResult> UpdateEmailVerificationCodeAsync(Guid userId, string code);
        //Task<IServiceResult> CreateAccountByAdmin(TDto user);
        //Task<IServiceResult> SearchAccount(string searchString);
        //Task<IServiceResult> ChangeAccountStatus(Guid userId);
        //Task<IServiceResult> UpdateAccount(Guid userId, TDto userUpdateDetail,string roleName);
        //Task<IServiceResult> CreateManyAccountsByAdmin(IFormFile excelFile);
        //// This delete feature support not opening database to delete;
        //Task<IServiceResult> DeleteAccount(Guid id);
    }
}
