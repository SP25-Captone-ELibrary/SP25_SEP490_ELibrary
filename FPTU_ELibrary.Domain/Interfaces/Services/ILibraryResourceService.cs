using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ILibraryResourceService<TDto> : IGenericService<LibraryResource, TDto, int> 
    where TDto : class
{
    Task<IServiceResult> GetAllWithSpecFromDashboardAsync(ISpecification<LibraryResource> spec);
    Task<IServiceResult> AddResourceToLibraryItemAsync(int libraryItemId, TDto dto);
    Task<IServiceResult> AddResourceToLibraryItemAsync(int libraryItemId, TDto dto,
        Dictionary<int, string> chunkDetails);
    Task<IServiceResult> SoftDeleteAsync(int id);
    Task<IServiceResult> SoftDeleteRangeAsync(int[] ids);
    Task<IServiceResult> UndoDeleteAsync(int id);
    Task<IServiceResult> UndoDeleteRangeAsync(int[] ids);
    Task<IServiceResult> DeleteRangeAsync(int[] ids);

    Task<IServiceResult<(Stream,string)>> GetFullPdfFileWithWatermark(string email, int resourceId);
    Task<IServiceResult>GetFullAudioFileWithWatermark(string email, int resourceId);
    Task<IServiceResult<MemoryStream>> GetAudioPreview(int resourceId);
    Task<IServiceResult<Stream>> GetPdfPreview(int resourceId);
    Task<IServiceResult> GetNumberOfUploadAudioFile(string email, int resourceId);
    Task<IServiceResult> WatermarkAudioAsyncFromAWS(string? s3OriginalAudioName, string email);
    Task<IServiceResult<Stream>> GetAudioPreviewFromAws(int resourceId);

    Task<IServiceResult> GetFullOriginalAudio(int resourceId);
    // Task<IServiceResult<Stream>> AddAudioWatermark(int resourceId, string resourceLang, string email, int chunkSize);
}