using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ILibraryShelfService<TDto> : IGenericService<LibraryShelf, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetAllBySectionIdAsync(int sectionId);
    Task<IServiceResult> GetDetailAsync(int shelfId, ISpecification<LibraryItem> spec);
    Task<IServiceResult> GetDetailWithFloorZoneSectionByIdAsync(int shelfId);
    Task<IServiceResult> GetItemAppropriateShelfAsync(int libraryItemId,
        bool? isReferenceSection, bool? isChildrenSection, 
        bool? isJournalSection, bool? isMostAppropriate);
}