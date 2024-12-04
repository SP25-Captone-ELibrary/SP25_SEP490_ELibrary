using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Microsoft.Extensions.Caching.Distributed;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;

namespace FPTU_ELibrary.Application.Services;

public class SystemMessageService : ISystemMessageService
{
    private readonly IDistributedCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SystemMessageService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IDistributedCache cache) 
    {
        _cache = cache;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IServiceResult> GetByIdAsync(string msgId)
    {
        var entity =
            await _unitOfWork.Repository<SystemMessage, string>().GetByIdAsync(msgId);
        
        if (entity == null)
        {
            return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
                "Fail to get data");
        }

        return new ServiceResult(ResultCodeConst.SYS_Success0002, 
            "Get data successfully", 
            _mapper.Map<SystemMessageDto>(entity));
    }
    
    public async Task<IServiceResult> ImportToExcelAsync(IFormFile file)
    {
        // Validate excel file
        var validationResult = await ValidatorExtensions.ValidateAsync(file);
        if (validationResult != null && validationResult.IsValid)
        {
            throw new UnprocessableEntityException("Invalid inputs",
                validationResult.ToProblemDetails().Errors);
        }
        
        // covert to stream
        var stream = file.OpenReadStream();
        
        // Mark as non-commercial
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        // Excel package 
        using (var xlPackage = new ExcelPackage(stream))
        {
            // Define worksheet
            var worksheet = xlPackage.Workbook.Worksheets.First();
            // Count row 
            var rowCount = worksheet.Dimension.Rows;
            // First row <- skip header
            var firstRow = 2;
            
            // Initialize total added value
            var totalAdded = 0;
            // Iterate each data row and convert to an object
            for (int i = firstRow; i < rowCount; ++i)
            {
                var msgId = worksheet.Cells[i, 1].Value?.ToString();
                var msgContent = worksheet.Cells[i, 2].Value?.ToString();
                var vi = worksheet.Cells[i, 3].Value?.ToString();
                var en = worksheet.Cells[i, 4].Value?.ToString();
                var ru = worksheet.Cells[i, 5].Value?.ToString();
                var ja = worksheet.Cells[i, 6].Value?.ToString();
                var ko = worksheet.Cells[i, 7].Value?.ToString();
                var createDate = double.Parse(worksheet.Cells[i, 8].Value?.ToString() ?? "0");
                var createBy = worksheet.Cells[i, 9].Value?.ToString();
                var modifiedDate = double.Parse(worksheet.Cells[i, 10].Value?.ToString() ?? "0");
                var modifiedBy = worksheet.Cells[i, 11].Value?.ToString();
                
                // Validate msgId
                if (string.IsNullOrEmpty(msgId) && !string.IsNullOrEmpty(msgContent))
                    // TODO: Custom list of errors/warning response while import data 
                    continue;
                // Break when reach last data
                else if(string.IsNullOrEmpty(msgId)) break;

                // Check exist message
                var msg = await _unitOfWork.Repository<SystemMessage, string>()
                    .GetByIdAsync(msgId);
                if (msg != null) continue; // TODO: Custom list of errors/warning response while import data  
                
                // Generate SystemMessage
                var systemMessage = new SystemMessage
                {
                    MsgId = msgId,
                    MsgContent = msgContent ?? null!,
                    Vi = vi,
                    En = en,
                    Ru = ru,
                    Ja = ja,
                    Ko = ko,
                    CreateDate = DateTime.FromOADate(createDate),
                    ModifiedDate = modifiedDate > 0
                        ? DateTime.FromOADate(modifiedDate)
                        : null,
                    CreateBy = createBy ?? null!,
                    ModifiedBy = modifiedBy ?? null,
                };
                
                // Progress create new 
                await _unitOfWork.Repository<SystemMessage, string>().AddAsync(systemMessage);
                var isCreated = await _unitOfWork.SaveChangesWithTransactionAsync() > 0;
                if (!isCreated) // Fail to save
                {
                    // TODO: Custom list of errors/warning response while import data
                    // throw new BadRequestException("Unable to save changes to DB, error invoke at row " + i);
                    continue;
                }
                else
                {
                    // Add items to cache
                    await _cache.SetAsync(systemMessage.MsgId, systemMessage);
                    // Increase total added items
                    totalAdded++;
                }
            }
            
            // Count total added items
            if (totalAdded > 0)
            {
                return new ServiceResult(ResultCodeConst.SYS_Success0005, 
                            $"Import {totalAdded} data successfully", true);
            }

            return new ServiceResult(ResultCodeConst.SYS_Warning0005, 
                "No data effected", false);
        }
    }

    public Task<IServiceResult> ExportToExcelAsync()
    {
        throw new NotImplementedException();
    }
}