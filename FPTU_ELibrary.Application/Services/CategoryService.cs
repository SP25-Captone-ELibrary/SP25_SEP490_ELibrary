using System.Text.RegularExpressions;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Dtos.Fine;
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
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class CategoryService : GenericService<Category, CategoryDto, int>,
    ICategoryService<CategoryDto>
{
    public CategoryService(ISystemMessageService msgService, IUnitOfWork unitOfWork, IMapper mapper, ILogger logger)
        : base(msgService, unitOfWork, mapper, logger)
    {
    }

    public override async Task<IServiceResult> CreateAsync(CategoryDto dto)
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

            var isExistVietnameseName = await _unitOfWork.Repository<Category, int>().AnyAsync(e
                => e.VietnameseName == dto.VietnameseName);
            if (isExistVietnameseName) // Already exist employee code
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0003);
                return new ServiceResult(ResultCodeConst.SYS_Warning0003,
                    StringUtils.Format(errMsg, "create", nameof(Category.VietnameseName).ToLower()));
            }

            var isExistEnglishName = await _unitOfWork.Repository<Category, int>().AnyAsync(e
                => e.EnglishName == dto.EnglishName);
            if (isExistEnglishName)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0003);
                return new ServiceResult(ResultCodeConst.SYS_Warning0003,
                    StringUtils.Format(errMsg, "create", nameof(Category.EnglishName).ToLower()));
            }

            // Process add new entity
            await _unitOfWork.Repository<Category, int>().AddAsync(_mapper.Map<Category>(dto));
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
            throw new Exception("Error invoke when process create book category");
        }
    }

    public override async Task<IServiceResult> UpdateAsync(int id, CategoryDto dto)
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
            var existingEntity = await _unitOfWork.Repository<Category, int>().GetByIdAsync(id);
            if (existingEntity == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, nameof(Category).ToLower()));
            }

            var isExistVietnameseName = await _unitOfWork.Repository<Category, int>().AnyAsync(e
                => e.VietnameseName == dto.VietnameseName);
            if (isExistVietnameseName) // Already exist employee code
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0003);
                return new ServiceResult(ResultCodeConst.SYS_Warning0003,
                    StringUtils.Format(errMsg, "create", nameof(Category.VietnameseName).ToLower()));
            }

            var isExistEnglishName = await _unitOfWork.Repository<Category, int>().AnyAsync(e
                => e.EnglishName == dto.EnglishName);
            if (isExistEnglishName)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0003);
                return new ServiceResult(ResultCodeConst.SYS_Warning0003,
                    StringUtils.Format(errMsg, "create", nameof(Category.EnglishName).ToLower()));
            }

            existingEntity = _mapper.Map(dto, existingEntity);

            // Check if there are any differences between the original and the updated entity
            if (!_unitOfWork.Repository<Category, int>().HasChanges(existingEntity))
            {
                serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
                serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
                serviceResult.Data = true;
                return serviceResult;
            }

            // Progress update when all require passed
            await _unitOfWork.Repository<Category, int>().UpdateAsync(existingEntity);

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
            throw new Exception("Error invoke when process update book category");
        }

        return serviceResult;
    }

    public override async Task<IServiceResult> DeleteAsync(int id)
    {
        var baseSpec = new BaseSpecification<Category>(bc => bc.CategoryId == id);
        baseSpec.ApplyInclude(q => q.Include(x => x.BookCategories));
        var existedCategory = await _unitOfWork.Repository<Category, int>().GetWithSpecAsync(baseSpec);
        if (existedCategory is null)
        {
            var errorMsg = await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0006);
            return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                StringUtils.Format(errorMsg, "book-category"));
        }
        else if (existedCategory.BookCategories.Any())
        {
            return new ServiceResult(ResultCodeConst.Category_Warning0001,
                await _msgService.GetMessageAsync(ResultCodeConst.Category_Warning0001));
        }

        await _unitOfWork.Repository<Category, int>().DeleteAsync(id);
        if (await _unitOfWork.SaveChangesAsync() > 0)
        {
            var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0008);
            return new ServiceResult(ResultCodeConst.SYS_Success0008,
                StringUtils.Format(msg, id.ToString()), true);
        }

        return new ServiceResult(ResultCodeConst.SYS_Fail0004
            , await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004)
            , false);
    }

    public async Task<IServiceResult> HardDeleteRangeAsync(int[] bookCategoryIds)
    {
        try
        {
            // Get all matching book category 
            // Build spec
            var baseSpec = new BaseSpecification<Category>(e => bookCategoryIds.Contains(e.CategoryId));
            baseSpec.ApplyInclude(q => q.Include(x => x.BookCategories));
            var categoryEntities = await _unitOfWork.Repository<Category, int>()
                .GetAllWithSpecAsync(baseSpec);
            var categoryList = categoryEntities.ToList();
            if (categoryList.Count < bookCategoryIds.Length)
            {
                return new ServiceResult(ResultCodeConst.Category_Warning0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.Category_Warning0002));
            }

            if (categoryList.Any(x => x.BookCategories.Any()))
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Process delete range
            await _unitOfWork.Repository<Category, int>().DeleteRangeAsync(bookCategoryIds);
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
    

    // Import book category from excel file with IFormFile and DuplicateHandle duplicateHandle, the logic could be the same as CreateManyAccountsWithSendEmail of UserService but without sending email
    public async Task<IServiceResult> ImportCategoryAsync(IFormFile excelFile, DuplicateHandle duplicateHandle)
    {
        // Initialize fields
        var langEnum =
            (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(LanguageContext.CurrentLanguage);
        var isEng = langEnum == SystemLanguage.English;
        var totalImportData = 0;
        if (excelFile == null || excelFile.Length == 0)
        {
            throw new BadRequestException(isEng
                ? "File is not valid"
                : "File không hợp lệ");
        }

        // Validate import file 
        var validationResult = await ValidatorExtensions.ValidateAsync(excelFile);
        if (validationResult != null && !validationResult.IsValid)
        {
            // Response the uploaded file is not supported
            throw new NotSupportedException(await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0001));
        }

        using var memoryStream = new MemoryStream();
        await excelFile.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        try
        {
            var failedMsgs = new List<UserFailedMessage>();
            using var package = new OfficeOpenXml.ExcelPackage(memoryStream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
            {
                throw new BadRequestException(isEng
                    ? "Excel file does not contain any worksheet"
                    : "Không tìm thấy worksheet");
            }

            var processedCategories = new Dictionary<string, int>();
            var rowCount = worksheet.Dimension.Rows;
            var categoryToAdd = new List<CategoryDto>();

            for (int row = 2; row <= rowCount; row++)
            {
                var categoryRecord = new CategoryExcelRecord()
                {
                    EnglishName = worksheet.Cells[row, 1].Text,
                    VietnameseName = worksheet.Cells[row, 2].Text,
                    Description = worksheet.Cells[row, 3].Text,
                };

                if (processedCategories.ContainsKey(categoryRecord.EnglishName))
                {
                    if (duplicateHandle.ToString().ToLower() == "skip")
                    {
                        continue;
                    }
                    else if (duplicateHandle.ToString().ToLower() == "replace")
                    {
                        failedMsgs.RemoveAll(f => f.Row == processedCategories[categoryRecord.EnglishName]);
                        processedCategories[categoryRecord.EnglishName] = row;
                    }
                }
                else
                {
                    processedCategories[categoryRecord.EnglishName] = row;
                }

                var rowErr = await DetectWrongRecord(categoryRecord, langEnum);
                if (rowErr)
                {
                    failedMsgs.Add(new UserFailedMessage()
                    {
                        Row = row,
                        ErrMsg = new List<string> { "Invalid record at " + row }
                    });
                }
                else
                {
                    // Convert to CategoryDto
                    var newCategory = new CategoryDto
                    {
                        EnglishName = categoryRecord.EnglishName,
                        VietnameseName = categoryRecord.VietnameseName,
                        Description = categoryRecord.Description
                    };

                    // Add category
                    categoryToAdd.Add(newCategory);
                }
            }
            if (failedMsgs.Any())
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001), failedMsgs.Select(x=> x.ErrMsg));
            }

            // Process add new entity
            var bookCategoryEntities = _mapper.Map<List<Category>>(categoryToAdd);
            await _unitOfWork.Repository<Category, int>().AddRangeAsync(bookCategoryEntities);
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
    // write detect wrong record like in UserService

    private async Task<bool> DetectWrongRecord(CategoryExcelRecord record, SystemLanguage? lang)
    {
        var isEng = lang == SystemLanguage.English;
        if (string.IsNullOrEmpty(record.EnglishName)
            || string.IsNullOrEmpty(record.VietnameseName)
            || string.IsNullOrEmpty(record.Description)
            || !Regex.IsMatch(record.EnglishName,
                @"^([A-Z][a-z]*)(\s[A-Z][a-z]*)*$")
            || !Regex.IsMatch(record.VietnameseName,
                @"^([A-ZÀ-Ỵ][a-zà-ỵ]*)(\s[A-ZÀ-Ỵ][a-zà-ỵ]*)*$"
            ))
        {
            return true;
        }

        return await _unitOfWork.Repository<Category, int>().AnyAsync(e
            => e.EnglishName == record.EnglishName);
    }
}