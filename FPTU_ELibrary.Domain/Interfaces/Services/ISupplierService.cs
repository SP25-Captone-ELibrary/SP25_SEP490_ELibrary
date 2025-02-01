using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ISupplierService<TDto> : IGenericService<Supplier, TDto, int>
    where TDto : class
{
    Task<IServiceResult> ImportAsync(IFormFile file, string[] scanningFields, DuplicateHandle? duplicateHandle);
    Task<IServiceResult> ExportAsync(ISpecification<Supplier> spec);   
}