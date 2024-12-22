using System.Text.RegularExpressions;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MimeKit.Encodings;
using Serilog;

namespace FPTU_ELibrary.Application.Services.IServices;

public class FinePolicyService : GenericService<FinePolicy, FinePolicyDto, int>,
    IFinePolicyService<FinePolicyDto>
{
    public FinePolicyService(
        ISystemMessageService msgService
        , IUnitOfWork unitOfWork
        , IMapper mapper
        , ILogger logger
    ) : base(msgService, unitOfWork, mapper, logger)
    {
    }

    public override async Task<IServiceResult> CreateAsync(FinePolicyDto dto)
    {
        var serviceResult = new ServiceResult();
        try
        {
            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(dto);
            // Check for valid validations
            if (validationResult != null && !validationResult.IsValid)
            {
                // Convert ValidationResult to ValidationProblemsDetails.Errors
                var errors = validationResult.ToProblemDetails().Errors;
                throw new UnprocessableEntityException("Invalid Validations", errors);
            }
            var isExistConditionType = await _unitOfWork.Repository<FinePolicy, int>().AnyAsync(e => e.ConditionType == dto.ConditionType);
            if (isExistConditionType) // Already exist employee code
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0003);
                return new ServiceResult(ResultCodeConst.SYS_Warning0003,
                    StringUtils.Format(errMsg, "create", nameof(FinePolicy).ToLower()));
            }
            
            // Process add new entity
            await _unitOfWork.Repository<FinePolicy, int>().AddAsync(_mapper.Map<FinePolicy>(dto));
            // Save to DB
            if (await _unitOfWork.SaveChangesAsync() > 0)
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Success0001;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001);
                serviceResult.Data = true;
            }
            else
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Fail0001;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001);
                serviceResult.Data = false;
            }
            return serviceResult;
        }
        catch (UnprocessableEntityException ex)
        {
            return new ServiceResult(ResultCodeConst.SYS_Warning0001,
                ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process create fine policy");
        }
    }

    public override async Task<IServiceResult> UpdateAsync(int id, FinePolicyDto dto)
    {
        var serviceResult = new ServiceResult();
        try
        {
            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(dto);
            // Check for valid validations
            if (validationResult != null && !validationResult.IsValid)
            {
                // Convert ValidationResult to ValidationProblemsDetails.Errors
                var errors = validationResult.ToProblemDetails().Errors;
                throw new UnprocessableEntityException("Invalid validations", errors);
            }
            // Retrieve the entity
            var existingEntity = await _unitOfWork.Repository<FinePolicy, int>().GetByIdAsync(id);
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, nameof(FinePolicy).ToLower()));
            }

            var isExistConditionType = await _unitOfWork.Repository<FinePolicy, int>().AnyAsync(e => e.ConditionType == dto.ConditionType);
            if (isExistConditionType) // Already exist employee code
            {
                   var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0003);
                    return new ServiceResult(ResultCodeConst.SYS_Warning0003,
                        StringUtils.Format(errMsg, "create", nameof(FinePolicy).ToLower()));
            }
            existingEntity = _mapper.Map(dto, existingEntity);
            
            // Check if there are any differences between the original and the updated entity
            if (!_unitOfWork.Repository<FinePolicy, int>().HasChanges(existingEntity))
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
                serviceResult.Data = true;
                return serviceResult;
            }
            // Progress update when all require passed
            await _unitOfWork.Repository<FinePolicy, int>().UpdateAsync(existingEntity);

            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Fail0003;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003);
                serviceResult.Data = false;
                return serviceResult;
            }

            // Mark as update success
            serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
            serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
            serviceResult.Data = true;
        
        }
        catch (UnprocessableEntityException ex)
        {
            return new ServiceResult(ResultCodeConst.SYS_Warning0001,
                ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update fine policy");
        }
        return serviceResult;
    }

    public override async Task<IServiceResult> DeleteAsync(int id)
    {
        var serviceResult = new ServiceResult();
        try
        {
            var baseSpec = new BaseSpecification<FinePolicy>(fp => fp.FinePolicyId == id && fp.Fines.Any());
            baseSpec.ApplyInclude(q => q.Include(x => x.Fines));
            var existingFinePolicy = await _unitOfWork.Repository<FinePolicy, int>().GetWithSpecAsync(baseSpec);
            if (existingFinePolicy is not null)
            {
                var errorMsg = await _msgService.GetMessageAsync(ResultCodeConst.FinePolicy_Warning0002);
                return new ServiceResult(ResultCodeConst.FinePolicy_Warning0002,
                    StringUtils.Format(errorMsg, nameof(FinePolicy).ToLower()));
            }

            // Process delete entity
            await _unitOfWork.Repository<FinePolicy, int>().DeleteAsync(id);
            // Save to DB
            if (await _unitOfWork.SaveChangesAsync() > 0)
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Success0004;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004);
                serviceResult.Data = true;
            }
            else
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Fail0004;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004);
                serviceResult.Data = false;
            }
            return serviceResult;    
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process delete fine policy");
        }
    }

    public async Task<IServiceResult> HardDeleteRangeAsync(int[] finePolicyIds)
    {
        try
        {
            // Get all matching book category 
            // Build spec
            var baseSpec = new BaseSpecification<FinePolicy>(e => finePolicyIds.Contains(e.FinePolicyId));
            baseSpec.ApplyInclude(q => q.Include(x => x.Fines));
            var categoryEntities = await _unitOfWork.Repository<FinePolicy, int>()
                .GetAllWithSpecAsync(baseSpec);
            var categoryList = categoryEntities.ToList();
            if (categoryList.Count < finePolicyIds.Length)
            {
                return new ServiceResult(ResultCodeConst.FinePolicy_Warning0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.FinePolicy_Warning0002));    
            }

            if (categoryList.Any(x => x.Fines.Any()))
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Process delete range
            await _unitOfWork.Repository<FinePolicy, int>().DeleteRangeAsync(finePolicyIds);
            // Save to DB
            if (await _unitOfWork.SaveChangesAsync() > 0)
            {
                var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0008);
                return new ServiceResult(ResultCodeConst.SYS_Success0008,
                    StringUtils.Format(msg, categoryList.Count.ToString()), true);
            }

            // Fail to delete
            return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is SqlException sqlEx)
            {
                switch (sqlEx.Number)
                {
                    case 547: // Foreign key constraint violation
                        return new ServiceResult(ResultCodeConst.SYS_Fail0007,
                            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0007));
                }
            }

            // Throw if other issues
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process delete range Book Category");
        }
    }

    public async Task<IServiceResult> ImportFinePolicyAsync(IFormFile finePolicies, DuplicateHandle duplicateHandle)
    {
        // Initialize fields
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(LanguageContext.CurrentLanguage);
        var isEng = langEnum == SystemLanguage.English;
        var totalImportData = 0;
        if (finePolicies == null || finePolicies.Length == 0)
        {
            throw new BadRequestException(isEng
                ? "File is not valid"
                : "File không hợp lệ");
        }

        // Validate import file 
        var validationResult = await ValidatorExtensions.ValidateAsync(finePolicies);
        if (validationResult != null && !validationResult.IsValid)
        {
            // Response the uploaded file is not supported
            throw new NotSupportedException(await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0001));
        }

        using var memoryStream = new MemoryStream();
        await finePolicies.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        try
        {
            var failedMsgs = new List<FinePolicyFailedMessage>();
            using var package = new OfficeOpenXml.ExcelPackage(memoryStream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
            {
                throw new BadRequestException(isEng
                    ? "Excel file does not contain any worksheet"
                    : "Không tìm thấy worksheet");
            }

            var processedFinePolicies = new Dictionary<string, int>();
            var rowCount = worksheet.Dimension.Rows;
            var categoryToAdd = new List<FinePolicyDto>();

            for (int row = 2; row <= rowCount; row++)
            {
                var finePolicyRecord = new FinePolicyExcelRecord
                {
                    ConditionType = worksheet.Cells[row, 1].Value?.ToString(),
                    FineAmountPerDay = decimal.Parse(worksheet.Cells[row, 2].Value?.ToString()),
                    FixedFineAmount = decimal.Parse(worksheet.Cells[row, 3].Value?.ToString()),
                    Description = worksheet.Cells[row, 4].Value?.ToString()
                };

                if (processedFinePolicies.ContainsKey(finePolicyRecord.ConditionType))
                {
                    if (duplicateHandle.ToString().ToLower() == "skip")
                    {
                        continue;
                    }
                    else if (duplicateHandle.ToString().ToLower() == "replace")
                    {
                        failedMsgs.RemoveAll(f => f.Row == processedFinePolicies[finePolicyRecord.ConditionType]);
                        processedFinePolicies[finePolicyRecord.ConditionType] = row;
                    }
                }
                else
                {
                    processedFinePolicies[finePolicyRecord.ConditionType] = row;
                }

                var rowErr = await DetectWrongRecord(finePolicyRecord, langEnum);
                if (rowErr)
                {
                    failedMsgs.Add(new FinePolicyFailedMessage()
                    {
                        Row = row,
                        ErrMsg = new List<string> { "Invalid record at " + row }
                    });
                }
                else
                {
                    // Convert to DTO
                    
                    var newFinePolicy = finePolicyRecord.ToFinePolicyDto();

                    // Add Dto
                    categoryToAdd.Add(newFinePolicy);
                }
            }
            if (failedMsgs.Any())
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001), failedMsgs.Select(x=> x.ErrMsg));
            }

            // Process add new entity
            var finePolicyEntities = _mapper.Map<List<FinePolicy>>(categoryToAdd);
            await _unitOfWork.Repository<FinePolicy, int>().AddRangeAsync(finePolicyEntities);
            // Save to DB
            if (await _unitOfWork.SaveChangesAsync() > 0)
            {
                return new ServiceResult(ResultCodeConst.SYS_Success0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001), true);
            }

            return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001), false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process import book category");
        }
    }

    private async Task<bool> DetectWrongRecord(FinePolicyExcelRecord record, SystemLanguage? lang)
    {
        var isEng = lang == SystemLanguage.English;
        if (string.IsNullOrEmpty(record.ConditionType)
            || string.IsNullOrEmpty(record.FineAmountPerDay.ToString())
            || !Regex.IsMatch(record.ConditionType,
                @"^([A-Z][a-z]*)(\s[A-Z][a-z]*)*$")
            || !Regex.IsMatch(record.FineAmountPerDay.ToString(), @"^\d+(\.\d+)?$")
            || !Regex.IsMatch(record.FineAmountPerDay.ToString(), @"^\d+(\.\d+)?$")
            || record.FineAmountPerDay <= 0
            || record.FineAmountPerDay <0
            )
        {
            return true;
        }

        return await _unitOfWork.Repository<FinePolicy, int>().AnyAsync(e
            => e.ConditionType == record.ConditionType);
    }
        
}