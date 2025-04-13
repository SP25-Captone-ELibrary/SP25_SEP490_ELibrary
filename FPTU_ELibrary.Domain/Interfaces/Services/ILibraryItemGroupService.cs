using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ILibraryItemGroupService<TDto> : IGenericService<LibraryItemGroup, TDto, int>
    where TDto : class
{
    Task<IServiceResult> CreateForItemAsync(int libraryItemId, string createdByEmail);
    Task<IServiceResult> GetAllItemPotentialGroupAsync(
        ISpecification<LibraryItemGroup> spec, 
        string title, string cutterNumber,
        string classificationNumber, string authorName);
}