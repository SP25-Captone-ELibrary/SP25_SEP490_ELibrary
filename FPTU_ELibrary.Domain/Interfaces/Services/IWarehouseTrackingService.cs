using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IWarehouseTrackingService<TDto> : IGenericService<WarehouseTracking, TDto, int>
    where TDto : class
{
    Task<IServiceResult> AddFinalizedStockInFileAsync(int trackingId, string url);
    Task<IServiceResult> GetAllStockTransactionTypeByTrackingTypeAsync(TrackingType trackingType);
    Task<IServiceResult> GetByIdAndIncludeInventoryAsync(int trackingId);
    Task<IServiceResult> CreateAndImportDetailsAsync(
        TDto dto, IFormFile? trackingDetailsFile, List<IFormFile> coverImageFiles,
        string[]? scanningFields, DuplicateHandle? duplicateHandle);
    Task<IServiceResult> CreateSupplementRequestAsync(TDto dto);
    Task<IServiceResult> CreateStockInWithDetailsAsync(TDto dto);
    Task<IServiceResult> UpdateStatusAsync(int id, WarehouseTrackingStatus status);
    Task<IServiceResult> UpdateInventoryWithoutSaveChanges(int id, TDto dto);
    Task<IServiceResult> RecalculateTotalAndAmountWithoutSaveChangesAsync(int id, int totalItem, decimal totalAmount);
}