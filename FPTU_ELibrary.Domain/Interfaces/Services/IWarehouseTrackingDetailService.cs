using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IWarehouseTrackingDetailService<TDto> : IGenericService<WarehouseTrackingDetail, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetRangeBarcodeByIdAsync(int trackingDetailId);
    Task<IServiceResult> GetDetailAsync(int id);
    Task<IServiceResult> GetLatestBarcodeByCategoryIdAsync(int categoryId);
    Task<IServiceResult> AddToWarehouseTrackingAsync(int trackingId, TDto dto);
    Task<IServiceResult> DeleteItemAsync(int trackingDetailId, int libraryItemId);
    Task<IServiceResult> UpdateRangeBarcodeRegistrationAsync(int trackingId, List<int> whDetailIds);
    Task<IServiceResult> UpdateBarcodeRegistrationAsync(int trackingDetailId);
    Task<IServiceResult> UpdateItemFromExternalAsync(int trackingDetailId, int libraryItemId);
    Task<IServiceResult> UpdateItemFromInternalAsync(int trackingDetailId, int libraryItemId);
    Task<IServiceResult> GetAllByTrackingIdAsync(int trackingId, ISpecification<WarehouseTrackingDetail> spec);
    Task<IServiceResult> GetAllNotExistItemByTrackingIdAsync(int trackingId, ISpecification<WarehouseTrackingDetail> spec);
    Task<IServiceResult> ImportAsync(int trackingId, 
        IFormFile file, string[]? scanningFields, DuplicateHandle? duplicateHandle);
}