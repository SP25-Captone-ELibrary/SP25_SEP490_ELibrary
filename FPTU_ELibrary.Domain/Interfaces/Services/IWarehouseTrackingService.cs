using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IWarehouseTrackingService<TDto> : IGenericService<WarehouseTracking, TDto, int>
    where TDto : class
{
    Task<IServiceResult> CreateAndImportDetailsAsync(
        TDto dto, IFormFile? trackingDetailsFile, string[]? scanningFields, DuplicateHandle? duplicateHandle);
    Task<IServiceResult> UpdateStatusAsync(int id, WarehouseTrackingStatus status);
}