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
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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
            // Determine current system language
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;

            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(dto);
            // Check for valid validations
            if (validationResult != null && !validationResult.IsValid)
            {
                // Convert ValidationResult to ValidationProblemsDetails.Errors
                var errors = validationResult.ToProblemDetails().Errors;
                throw new UnprocessableEntityException("Invalid Validations", errors);
            }

            // Custom errors
            var customErrors = new Dictionary<string, string[]>();
            
            // Check exist policy title
            var isExistPolicyTitle = await _unitOfWork.Repository<FinePolicy, int>()
                .AnyAsync(e => e.FinePolicyTitle.ToLower() == dto.FinePolicyTitle.ToLower());
            if (isExistPolicyTitle)
            {
                // Add error
                customErrors.Add(StringUtils.ToCamelCase(nameof(FinePolicyDto.FinePolicyTitle)), 
                    [isEng ? "Policy title already exist" : "Tên chính sách đã tồn tại"]);
            }
            
            // Invoke any errors
            if (customErrors.Any())
            {
                throw new UnprocessableEntityException("Invalid Data", customErrors);
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
        catch (UnprocessableEntityException)
        {
            throw;
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
            // Determine current system lang
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Custom errors
            var customErrors = new Dictionary<string, string[]>();
            
            // Check exist policy title
            var isExistPolicyTitle = await _unitOfWork.Repository<FinePolicy, int>()
                .AnyAsync(e => e.FinePolicyTitle.ToLower() == dto.FinePolicyTitle.ToLower() &&
                    e.FinePolicyId != id); // Compare to other policy
            if (isExistPolicyTitle)
            {
                // Add error
                customErrors.Add(StringUtils.ToCamelCase(nameof(FinePolicyDto.FinePolicyTitle)), 
                    [isEng ? "Policy title already exist" : "Tên chính sách đã tồn tại"]);
            }
            
            // Invoke any errors
            if (customErrors.Any())
            {
                throw new UnprocessableEntityException("Invalid Data", customErrors);
            }
            
            // Retrieve the entity
            var existingEntity = await _unitOfWork.Repository<FinePolicy, int>().GetByIdAsync(id);
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, nameof(FinePolicy).ToLower()));
            }
            
            // Update properties
            existingEntity.FinePolicyTitle = dto.FinePolicyTitle;
            existingEntity.FineAmountPerDay = dto.MinDamagePct;
            existingEntity.FixedFineAmount = dto.MaxDamagePct;
            existingEntity.ProcessingFee = dto.ProcessingFee;
            existingEntity.DailyRate = dto.DailyRate;
            existingEntity.ChargePct = dto.ChargePct;
            existingEntity.Description = dto.Description;
            existingEntity.ConditionType = dto.ConditionType;
            
            // Validate inputs using the generic validator
            var validationResult = await ValidatorExtensions.ValidateAsync(_mapper.Map(existingEntity,dto));
            // Check for valid validations
            if (validationResult != null && !validationResult.IsValid)
            {
                // Convert ValidationResult to ValidationProblemsDetails.Errors
                var errors = validationResult.ToProblemDetails().Errors;
                throw new UnprocessableEntityException("Invalid validations", errors);
            }
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
        catch (UnprocessableEntityException)
        {
            throw;
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
            // Determine current system language
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Check exist fine policy
            var existingFinePolicy = await _unitOfWork.Repository<FinePolicy, int>().GetByIdAsync(id);
            if (existingFinePolicy == null)
            {
                var errorMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errorMsg, isEng ? "fine pocily" : "chính sách"));
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
            throw new Exception("Error invoke when process delete fine policy");
        }
    }

    public async Task<IServiceResult> DeleteRangeAsync(int[] finePolicyIds)
    {
        try
        {
            // Get all matching book category 
            // Build spec
            var baseSpec = new BaseSpecification<FinePolicy>(e => finePolicyIds.Contains(e.FinePolicyId));
            var finePolicyEntities = await _unitOfWork.Repository<FinePolicy, int>()
                .GetAllWithSpecAsync(baseSpec);
            // Convert to list 
            var finePolicyList = finePolicyEntities.ToList();
            if (!finePolicyList.Any())
            {
                return new ServiceResult(ResultCodeConst.SYS_Warning0005,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0005));
            }
            
            // Process delete range
            await _unitOfWork.Repository<FinePolicy, int>().DeleteRangeAsync(finePolicyList.Select(x => x.FinePolicyId).ToArray());
            // Save to DB
            if (await _unitOfWork.SaveChangesAsync() > 0)
            {
                var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0008);
                return new ServiceResult(ResultCodeConst.SYS_Success0008,
                    StringUtils.Format(msg, finePolicyList.Count.ToString()), true);
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
        if (finePolicies.Length == 0)
        {
            // Msg: Fail to import data
            return new ServiceResult(ResultCodeConst.SYS_Fail0008,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008));
        }

        // Validate import file 
        var validationResult = await ValidatorExtensions.ValidateAsync(finePolicies);
        if (validationResult != null && !validationResult.IsValid)
        {
            // Response the uploaded file is not supported
            return new ServiceResult(ResultCodeConst.File_Warning0001,
                await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0001));
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
                // Msg: Fail to import data
                return new ServiceResult(ResultCodeConst.SYS_Fail0008,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008));
            }

            var processedFinePolicies = new Dictionary<string, int>();
            var rowCount = worksheet.Dimension.Rows;
            var categoryToAdd = new List<FinePolicyDto>();

            for (int row = 2; row <= rowCount; row++)
            {
                var finePolicyRecord = new FinePolicyExcelRecord
                {
                    FinePolicyTitle = worksheet.Cells[row, 1].Value?.ToString() ?? null!,
                    ConditionType = worksheet.Cells[row, 2].Value?.ToString(),
                    MinDamagePct = decimal.TryParse(worksheet.Cells[row, 3].Value?.ToString(), out var validMinDamagePct) ? validMinDamagePct : 0m,
                    MaxDamagePct = decimal.TryParse(worksheet.Cells[row, 4].Value?.ToString(), out var validMaxDamagePct) ? validMaxDamagePct : 0m,
                    ProcessingFee = decimal.TryParse(worksheet.Cells[row, 5].Value?.ToString(), out var validProcessingFee) ? validProcessingFee : 0m,
                    DailyRate = decimal.TryParse(worksheet.Cells[row, 6].Value?.ToString(), out var validDailyRate) ? validDailyRate : 0m,
                    ChargePct = decimal.TryParse(worksheet.Cells[row, 7].Value?.ToString(), out var valiChargePct) ? valiChargePct : 0m,
                    Description = worksheet.Cells[row, 5].Value?.ToString()
                };

                // Validate condition type
                if (!Enum.TryParse(typeof(FinePolicyConditionType), finePolicyRecord.ConditionType, true, out _))
                {
                    failedMsgs.Add(new FinePolicyFailedMessage()
                    {
                        Row = row,
                        ErrMsg = new List<string> { isEng ? "Condition type is not exist" : "Loại chính sách không tồn tại" }
                    });
                }
                
                if (processedFinePolicies.ContainsKey(finePolicyRecord.FinePolicyTitle))
                {
                    if (duplicateHandle.ToString().ToLower() == "skip")
                    {
                        continue;
                    }
                    else if (duplicateHandle.ToString().ToLower() == "replace")
                    {
                        failedMsgs.RemoveAll(f => f.Row == processedFinePolicies[finePolicyRecord.FinePolicyTitle]);
                        processedFinePolicies[finePolicyRecord.FinePolicyTitle] = row;
                    }
                }
                else
                {
                    processedFinePolicies[finePolicyRecord.FinePolicyTitle] = row;
                }

                var rowErr = await DetectWrongRecord(finePolicyRecord, langEnum);
                if (rowErr)
                {
                    failedMsgs.Add(new FinePolicyFailedMessage()
                    {
                        Row = row,
                        ErrMsg = new List<string> { isEng ? "Invoke error(s), please re-check" : "Có lỗi xảy ra, vui lòng kiểm tra lại dữ liệu" }
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
                return new ServiceResult(ResultCodeConst.SYS_Fail0008,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008), failedMsgs);
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

            return new ServiceResult(ResultCodeConst.SYS_Fail0008,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008), false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process import fine policy");
        }
    }

    private async Task<bool> DetectWrongRecord(FinePolicyExcelRecord record, SystemLanguage? lang)
    {
        // DAMAGE rules
        if (record.ConditionType == nameof(FinePolicyConditionType.Damage))
        {
            if (!record.MinDamagePct.HasValue)
                return true;
            if (record.MinDamagePct < 0m || record.MinDamagePct > 1m)
                return true;

            if (!record.MaxDamagePct.HasValue)
                return true;
            if (record.MaxDamagePct < 0m || record.MaxDamagePct > 1m)
                return true;

            if (!record.ChargePct.HasValue)
                return true;
            if (record.ChargePct < 0m || record.ChargePct > 1m)
                return true;

            if (!record.ProcessingFee.HasValue)
                return true;
            if (record.ProcessingFee < 0m)
                return true;

            if (record.MinDamagePct > record.MaxDamagePct)
                return true;
        }
        // OVERDUE rules
        else if (record.ConditionType == nameof(FinePolicyConditionType.OverDue))
        {
            if (!record.DailyRate.HasValue)
                return true;
            if (record.DailyRate < 0m)
                return true;
        }
        // LOST rules
        else if (record.ConditionType == nameof(FinePolicyConditionType.Lost))
        {
            if (!record.ChargePct.HasValue)
                return true;
            if (record.ChargePct < 0m || record.ChargePct > 1m)
                return true;

            if (!record.ProcessingFee.HasValue)
                return true;
            if (record.ProcessingFee < 0m)
                return true;
        }

        // finally: check for duplicate title
        bool exists = await _unitOfWork
            .Repository<FinePolicy, int>()
            .AnyAsync(e => e.FinePolicyTitle.ToLower() == record.FinePolicyTitle.ToLower());

        if (exists)
            return true;
        
        // Passed all validations
        return false;
    }

    public override async Task<IServiceResult> GetAllWithSpecAsync(ISpecification<FinePolicy> spec, bool tracked = true)
    {
        try
        {
            // Try to parse specification to FinePolicySpecification
            var fineSpec = spec as FinePolicySpecification;
            // Check if specification is null
            if (fineSpec == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }
            // count total records
            var totalRecords = await _unitOfWork.Repository<FinePolicy, int>().CountAsync(fineSpec);
            // count total pages
            var totalPage = (int)Math.Ceiling((double)totalRecords / fineSpec.PageSize);
            
            if (fineSpec.PageIndex > totalPage 
                || fineSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
            {
                fineSpec.PageIndex = 1; // Set default to first page
            }
            fineSpec.ApplyPaging(
                skip: fineSpec.PageSize * (fineSpec.PageIndex - 1), 
                take: fineSpec.PageSize);
            
            var entities = await _unitOfWork.Repository<FinePolicy, int>().GetAllWithSpecAsync(spec, tracked);
            if (entities.Any())
            {
					
                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<FinePolicyDto>(_mapper.Map<IEnumerable<FinePolicyDto>>(entities),
                    fineSpec.PageIndex, fineSpec.PageSize, totalPage, totalRecords);
					
                // Response with pagination 
                return new ServiceResult(ResultCodeConst.SYS_Success0002, 
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }
            // Not found any data
            return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                // Mapping entities to dto and ignore sensitive user data
                _mapper.Map<IEnumerable<FinePolicyDto>>(entities));
            
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get all fine policy");
        }
    }
}