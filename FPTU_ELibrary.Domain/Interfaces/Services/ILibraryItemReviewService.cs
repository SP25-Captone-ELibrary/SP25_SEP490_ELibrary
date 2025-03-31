using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ILibraryItemReviewService<TDto> : IGenericService<LibraryItemReview, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetItemReviewByEmailAsync(string email, int libraryItemId);
    Task<IServiceResult> ReviewItemAsync(string email, TDto dto);
    Task<IServiceResult> DeleteAsync(string email, int libraryItemId);
}