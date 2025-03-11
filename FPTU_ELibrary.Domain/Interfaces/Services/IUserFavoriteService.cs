using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IUserFavoriteService<TDto> : IGenericService<UserFavorite, TDto, int>
    where TDto : class
{
    Task<IServiceResult> AddFavoriteAsync(int libraryItemId, string email);
    Task<IServiceResult> CreateRangeFavAfterRequestFailedWithoutSaveChangesAsync(int[] libraryItemIds, string email, bool isForceToReplaceWhenExist = false);
    Task<IServiceResult> RemoveFavoriteAsync(int libraryItemId, string email);
}