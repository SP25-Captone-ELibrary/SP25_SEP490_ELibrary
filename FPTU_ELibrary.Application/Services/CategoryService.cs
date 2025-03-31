using System.Text.RegularExpressions;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
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
    public CategoryService(
         ISystemMessageService msgService, 
         IUnitOfWork unitOfWork, 
         IMapper mapper, 
         ILogger logger)
         : base(msgService, unitOfWork, mapper, logger)
     {
     }

     public override async Task<IServiceResult> CreateAsync(CategoryDto dto)
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
             // Check existing vietnamese name
             var isExistVietnameseName = await _unitOfWork.Repository<Category, int>().AnyAsync(e
                 => e.VietnameseName.ToLower() == dto.VietnameseName.ToLower());
             if (isExistVietnameseName)  
             {
                 customErrors.Add(StringUtils.ToCamelCase(nameof(Category.VietnameseName)), 
                     [isEng ? "Vietnamese name already exist" : "Tên tiếng việt đã tồn tại"]);
             }

             // Check existing english name
             var isExistEnglishName = await _unitOfWork.Repository<Category, int>().AnyAsync(e
                 => e.EnglishName.ToLower() == dto.EnglishName.ToLower());
             if (isExistEnglishName)
             {
                 customErrors.Add(StringUtils.ToCamelCase(nameof(Category.EnglishName)), 
                      [isEng ? "English name already exist" : "Tên tiếng anh đã tồn tại"]);
             }

             // Check exist prefix pattern
             var isExistPrefix = await _unitOfWork.Repository<Category, int>().AnyAsync(c => 
                 c.Prefix.ToLower() == dto.Prefix.ToLower());
             if (isExistPrefix)
             {
                 customErrors.Add(StringUtils.ToCamelCase(nameof(Category.Prefix)), 
                    [isEng ? "Prefix for item classification number already exist" : "Mẫu tiền tố cho số đăng ký cá biệt đã tồn tại"]);
             }
             
             // Check exist any errors
             if (customErrors.Any())
             {
                 throw new UnprocessableEntityException("Invalid data", customErrors);
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
                 throw new UnprocessableEntityException("Invalid validations", errors);
             }

             // Retrieve the entity
             var existingEntity = await _unitOfWork.Repository<Category, int>().GetByIdAsync(id);
             if (existingEntity == null)
             {
                 var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                 return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                     StringUtils.Format(errMsg, isEng ? "category" : "phân loại"));
             }

             // Custom errors
             var customErrors = new Dictionary<string, string[]>();
             // Check existing vietnamese name
             var isExistVietnameseName = await _unitOfWork.Repository<Category, int>().AnyAsync(e
                 => e.VietnameseName.ToLower() == dto.VietnameseName.ToLower() && 
                    e.CategoryId != existingEntity.CategoryId); // Compare with other category
             if (isExistVietnameseName)  
             {
                 customErrors.Add(StringUtils.ToCamelCase(nameof(Category.VietnameseName)), 
                     [isEng ? "Vietnamese name already exist" : "Tên tiếng việt đã tồn tại"]);
             }

             // Check existing english name
             var isExistEnglishName = await _unitOfWork.Repository<Category, int>().AnyAsync(e
                 => e.EnglishName.ToLower() == dto.EnglishName.ToLower() && 
                    e.CategoryId != existingEntity.CategoryId); // Compare with other category
             if (isExistEnglishName)
             {
                 customErrors.Add(StringUtils.ToCamelCase(nameof(Category.EnglishName)), 
                     [isEng ? "English name already exist" : "Tên tiếng anh đã tồn tại"]);
             }

             // Check exist prefix pattern
             var isExistPrefix = await _unitOfWork.Repository<Category, int>().AnyAsync(c => 
                 c.Prefix.ToLower() == dto.Prefix.ToLower() && 
                 c.CategoryId != existingEntity.CategoryId); // Compare with other category
             if (isExistPrefix)
             {
                 customErrors.Add(StringUtils.ToCamelCase(nameof(Category.Prefix)), 
                     [isEng ? "Prefix for item classification number already exist" : "Mẫu tiền tố cho số đăng ký cá biệt đã tồn tại"]);
             }
             
             // Check exist any errors
             if (customErrors.Any())
             {
                 throw new UnprocessableEntityException("Invalid data", customErrors);
             }
             
             // Check constraints with other tables
             // Not allow to update when exist any library items or warehouse tracking details
             if (await _unitOfWork.Repository<Category, int>().AnyAsync(new BaseSpecification<Category>(
                     c => c.CategoryId == id && (c.WarehouseTrackingDetails.Any() || c.LibraryItems.Any()))))
             {
                 return new ServiceResult(ResultCodeConst.SYS_Warning0008,
                     await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0008));
             }

             // Update properties
             existingEntity.Prefix = dto.Prefix;
             existingEntity.EnglishName = dto.EnglishName;
             existingEntity.VietnameseName = dto.VietnameseName;
             existingEntity.Description = dto.Description;
             
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
         catch (UnprocessableEntityException)
         {
             throw;
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
         try
         {
             // Determine current system language
             var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                 LanguageContext.CurrentLanguage);
             var isEng = lang == SystemLanguage.English;
             
             // Retrieve category by id
             var existedCategory = await _unitOfWork.Repository<Category, int>().GetByIdAsync(id);
             if (existedCategory is null) // not found
             {
                 var errorMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                 return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                     StringUtils.Format(errorMsg, isEng ? "category" : "phân loại"));
             }

             // Progress delete
             await _unitOfWork.Repository<Category, int>().DeleteAsync(id);
    
             // Save DB
             if (await _unitOfWork.SaveChangesAsync() > 0)
             {
                 // Success to delete
                 return new ServiceResult(ResultCodeConst.SYS_Success0004,
                     await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004), true);
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
             throw new Exception("Error invoke when delete category");
         }
     }

     public async Task<IServiceResult> DeleteRangeAsync(int[] bookCategoryIds)
     {
         try
         {
             // Get all matching book category 
             // Build spec
             var baseSpec = new BaseSpecification<Category>(e => bookCategoryIds.Contains(e.CategoryId));
             var categoryEntities = await _unitOfWork.Repository<Category, int>()
                 .GetAllWithSpecAsync(baseSpec);
             // Convert to list
             var categoryList = categoryEntities.ToList();
             if (!categoryList.Any())
             {
                 return new ServiceResult(ResultCodeConst.SYS_Warning0005,
                     await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0005));
             }
             
             // Process delete range
             await _unitOfWork.Repository<Category, int>().DeleteRangeAsync(categoryList.Select(x => x.CategoryId).ToArray());
             
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
             return new ServiceResult(ResultCodeConst.File_Warning0001,
                 await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0001));
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
                     Prefix = worksheet.Cells[row, 1].Text,
                     EnglishName = worksheet.Cells[row, 2].Text,
                     VietnameseName = worksheet.Cells[row, 3].Text,
                     Description = worksheet.Cells[row, 4].Text,
                     IsAllowAITraining = bool.TryParse(worksheet.Cells[row, 5].Text, out var validBool) && validBool,
                     TotalBorrowDays = int.TryParse(worksheet.Cells[row, 6].Text, out var validInt) ? validInt : 0,
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
                         ErrMsg = new List<string>
                         {
                             isEng 
                                 ? "English or vietnamese name not valid or already exist" 
                                 : "Tên tiếng anh hoặc tiếng việt không hợp lệ hoặc đã tồn tại"
                         }
                     });
                 }
                 else
                 {
                     // Convert to CategoryDto
                     var newCategory = new CategoryDto
                     {
                         Prefix = categoryRecord.Prefix,
                         EnglishName = categoryRecord.EnglishName,
                         VietnameseName = categoryRecord.VietnameseName,
                         Description = categoryRecord.Description,
                         IsAllowAITraining = categoryRecord.IsAllowAITraining,
                         TotalBorrowDays = categoryRecord.TotalBorrowDays
                     };

                     // Add category
                     categoryToAdd.Add(newCategory);
                 }
             }
             if (failedMsgs.Any())
             {
                 return new ServiceResult(ResultCodeConst.SYS_Fail0008,
                     await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008), failedMsgs);
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

             return new ServiceResult(ResultCodeConst.SYS_Fail0008,
                 await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008), false);
         }
         catch (Exception ex)
         {
             _logger.Error(ex.Message);
             throw new Exception("Error invoke when process import book category");
         }
     }

     private async Task<bool> DetectWrongRecord(CategoryExcelRecord record, SystemLanguage? lang)
     {
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

         return await _unitOfWork.Repository<Category, int>().AnyAsync(e => 
             e.EnglishName.ToLower() == record.EnglishName.ToLower() || 
             e.VietnameseName.ToLower() == record.VietnameseName.ToLower());
     }
}