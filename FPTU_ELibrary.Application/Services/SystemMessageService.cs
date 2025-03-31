using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.Roles;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using Serilog;
using Exception = System.Exception;

namespace FPTU_ELibrary.Application.Services;

public class SystemMessageService : ISystemMessageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger _logger;

    public SystemMessageService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger logger) 
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<string> GetMessageAsync(string msgId)
    {
        try
        {
            // Try to get system message from memory cache, create new if not exist
            var msgEntity = await _unitOfWork.Repository<SystemMessage, string>()
                .GetByIdAsync(msgId);

            // Retrieve global language
            var langStr = LanguageContext.CurrentLanguage;
            var langEnum = EnumExtensions.GetValueFromDescription<SystemLanguage>(langStr);
            // Define message Language
            var message = langEnum switch
            {
                SystemLanguage.Vietnamese => msgEntity?.Vi,
                SystemLanguage.English => msgEntity?.En,
                SystemLanguage.Russian => msgEntity?.Ru,
                SystemLanguage.Japanese => msgEntity?.Ja,
                SystemLanguage.Korean => msgEntity?.Ko,
                _ => msgEntity?.En
            };
            
            return message!;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when progress get system message");
        }
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