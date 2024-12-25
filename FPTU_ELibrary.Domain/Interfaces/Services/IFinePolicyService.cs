using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IFinePolicyService<TDto> :IGenericService<FinePolicy, TDto, int>
    where TDto : class
{
    Task<IServiceResult> HardDeleteRangeAsync(int[] finePolicyIds);
    
    Task<IServiceResult> ImportFinePolicyAsync(IFormFile finePolicies, DuplicateHandle duplicateHandle);
}