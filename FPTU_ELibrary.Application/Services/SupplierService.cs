using System.Globalization;
using CsvHelper.Configuration;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Suppliers;
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

namespace FPTU_ELibrary.Application.Services;

public class SupplierService : GenericService<Supplier, SupplierDto, int>,
    ISupplierService<SupplierDto>
{
    public SupplierService(
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
    }

    public override async Task<IServiceResult> UpdateAsync(int id, SupplierDto dto)
    {
        try
		{
			// Determine current system lang
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

			// Build specification
			var baseSpec = new BaseSpecification<Supplier>(s => s.SupplierId == id);
			// Apply include
			baseSpec.ApplyInclude(q => q
				.Include(s => s.WarehouseTrackings)
			);
			// Retrieve the entity
			var existingEntity = await _unitOfWork.Repository<Supplier, int>().GetWithSpecAsync(baseSpec);
			if (existingEntity == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
					StringUtils.Format(errMsg, isEng ? "supplier" : "bên cung cấp"));
			}
			
			// Check constraints whether existing tracking mark as [Completed] or [Cancelled]
			if (existingEntity.WarehouseTrackings
			    .Any(wt => 
				    wt.Status == WarehouseTrackingStatus.Completed ||
				    wt.Status == WarehouseTrackingStatus.Cancelled)
			    )
			{
				// Do not allow to update
				return new ServiceResult(ResultCodeConst.Supplier_Warning0001,
					await _msgService.GetMessageAsync(ResultCodeConst.Supplier_Warning0001));
			}
			
			// Process add update entity
			existingEntity.SupplierName = dto.SupplierName;
			existingEntity.SupplierType = dto.SupplierType;
			existingEntity.ContactPerson = dto.ContactPerson;
			existingEntity.ContactEmail = dto.ContactEmail;
			existingEntity.ContactPhone = dto.ContactPhone;
			existingEntity.Address = dto.Address;
			existingEntity.Country = dto.Country;
			existingEntity.City = dto.City;
			existingEntity.UpdatedAt = dto.UpdatedAt;

			// Check if there are any differences between the original and the updated entity
			if (!_unitOfWork.Repository<Supplier, int>().HasChanges(existingEntity))
			{
				return new ServiceResult(ResultCodeConst.SYS_Success0003, 
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
			}

			// Progress update when all require passed
			await _unitOfWork.Repository<Supplier, int>().UpdateAsync(existingEntity);

			// Save changes to DB
			var rowsAffected = await _unitOfWork.SaveChangesAsync();
			if (rowsAffected == 0)
			{
				return new ServiceResult(ResultCodeConst.SYS_Fail0003, 
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
			}

			// Mark as update success
			return new ServiceResult(ResultCodeConst.SYS_Success0003, 
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
		}
		catch (UnprocessableEntityException)
		{
			throw;
		}
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process update supplier");
        }
    }
    
    public override async Task<IServiceResult> DeleteAsync(int id)
	{
		// Initiate service result
		var serviceResult = new ServiceResult();

		try
		{
			// Determine current system lang
			var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
				LanguageContext.CurrentLanguage);
			var isEng = lang == SystemLanguage.English;
			
			// Retrieve the entity
			var existingEntity = await _unitOfWork.Repository<Supplier, int>().GetByIdAsync(id);
			if (existingEntity == null)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMsg, isEng ? "supplier" : "bên cung cấp"));
			}

			// Process add delete entity
			await _unitOfWork.Repository<Supplier, int>().DeleteAsync(id);
			// Save to DB
			if (await _unitOfWork.SaveChangesAsync() > 0)
			{
				return new ServiceResult(ResultCodeConst.SYS_Success0004,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004));
			}
			else
			{
				serviceResult.ResultCode = ResultCodeConst.SYS_Fail0004;
				serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004);
				serviceResult.Data = false;
			}
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
			throw new Exception("Error invoke when process delete supplier");
		}

		return serviceResult;
	}

	public async Task<IServiceResult> ImportAsync(
		IFormFile file, string[] scanningFields, DuplicateHandle? duplicateHandle)
	{
		try
		{
			// Determine system lang
			var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(LanguageContext
				.CurrentLanguage);

			// Check exist file
			if (file.Length == 0)
			{
				return new ServiceResult(ResultCodeConst.File_Warning0002,
					await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0002));
			}

			// Validate import file 
			var validationResult = await ValidatorExtensions.ValidateAsync(file);
			if (validationResult != null && !validationResult.IsValid)
			{
				// Response the uploaded file is not supported
				return new ServiceResult(ResultCodeConst.File_Warning0001,
					await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0001));
			}

			// Csv config
			var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				HeaderValidated = null,
				MissingFieldFound = null
			};

			// Process read csv file
			var readResp =
				CsvUtils.ReadCsvOrExcelByHeaderIndexWithErrors<SupplierCsvRecord>(
					file: file,
					config: csvConfig,
					props: new ExcelProps()
					{
						// Header start from row 1-1
						FromRow = 1,
						ToRow = 1,
						// Start from col
						FromCol = 1,
						// Start read data index
						StartRowIndex = 2
					},
					encodingType: null,
					systemLang: lang);
			if(readResp.Errors.Any())
			{
				var errorResps = readResp.Errors.Select(x => new ImportErrorResultDto()
				{	
					RowNumber = x.Key,
					Errors = x.Value.ToList()
				});
			    
				return new ServiceResult(ResultCodeConst.SYS_Fail0008,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008), errorResps);
			}
			
			// Additional message
			var additionalMsg = string.Empty;
			// Detect duplicates
			var detectDuplicateResult = DetectDuplicatesInFile(readResp.Records, scanningFields ?? [], lang);
			if (detectDuplicateResult.Errors.Count != 0 && duplicateHandle == null) // Has not selected any handle options yet
			{
				var errorResp = detectDuplicateResult.Errors.Select(x => new ImportErrorResultDto()
				{	
					RowNumber = x.Key,
					Errors = x.Value
				});
                
				// Response error messages for data confirmation and select handle options 
				return new ServiceResult(ResultCodeConst.SYS_Fail0008,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008), errorResp);
			}
			if (detectDuplicateResult.Errors.Count != 0 && duplicateHandle != null) // Selected any handle options
			{
				// Handle duplicates
				var handleResult = CsvUtils.HandleDuplicates(
					readResp.Records, detectDuplicateResult.Duplicates, (DuplicateHandle) duplicateHandle, lang);
				// Update records
				readResp.Records = handleResult.handledRecords;
				// Update msg 
				additionalMsg = handleResult.msg;
			}
			
			// Progress import supplier
			var totalImported = 0;
			// Initialize list items
			var itemList = new List<SupplierDto>();
			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
			foreach (var record in readResp.Records)
			{
				itemList.Add(new SupplierDto()
				{
					SupplierName = record.SupplierName,
					SupplierType = Enum.TryParse(record.SupplierType, true, out SupplierType supplierType) 
						? supplierType : SupplierType.Publisher,
					ContactPerson = record.ContactPerson,
					ContactEmail = record.ContactEmail,
					ContactPhone = record.ContactPhone,
					Address = record.Address,
					Country = record.Country,
					City = record.City,
					IsActive = true,
					IsDeleted = false,
					CreatedAt = currentLocalDateTime
				});
			}
			
			if (itemList.Any())
			{
				// Add new book
				await _unitOfWork.Repository<Supplier, int>().AddRangeAsync(_mapper.Map<List<Supplier>>(itemList));
					
				// Save change to DB
				if(await _unitOfWork.SaveChangesAsync() > 0) totalImported = itemList.Count;
			}

			if (totalImported == 0)
			{
				return new ServiceResult(ResultCodeConst.SYS_Warning0005,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0005));
			}


			var msg = StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0005), 
				$"{totalImported}");
			var customMsg = !string.IsNullOrEmpty(additionalMsg) ? $"{msg}, {additionalMsg}" : msg;
			return new ServiceResult(ResultCodeConst.SYS_Success0005, customMsg);
		}
		catch (UnprocessableEntityException)
        {
            throw;
        }
		catch (Exception ex)
		{
			_logger.Error(ex.Message);
			throw new Exception("Error invoke while process import supplier");
		}
	}

	public async Task<IServiceResult> ExportAsync(ISpecification<Supplier> spec)
	{
		try
		{
			 // Try to parse specification to SupplierSpecification
            var baseSpec = spec as SupplierSpecification;
            // Check if specification is null
            if (baseSpec == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
            	    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }	
            
            // Get all with spec
            var entities = await _unitOfWork.Repository<Supplier, int>()
	            .GetAllWithSpecAsync(baseSpec, tracked: false);
            if (entities.Any()) // Exist data
            {
	            // Map entities to dtos 
	            var dtos = _mapper.Map<List<SupplierDto>>(entities);
	            // Process export data to file
	            var fileBytes = CsvUtils.ExportToExcelWithNameAttribute(
		            dtos.Select(x => x.ToSupplierCsvRecord()).ToList());

	            return new ServiceResult(ResultCodeConst.SYS_Success0002,
		            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
		            fileBytes);
            }
			
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
	            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
		}
		catch (UnprocessableEntityException)
		{
			throw;
		}
		catch (Exception ex)
		{
			_logger.Error(ex.Message);
			throw new Exception("Error invoke while process export supplier");
		}
	}

	private (Dictionary<int, List<string>> Errors, Dictionary<int, List<int>> Duplicates) DetectDuplicatesInFile(
		List<SupplierCsvRecord> records,
		string[] scanningFields,
		SystemLanguage? lang
	)
	{
		// Check whether exist any scanning fields
		if (scanningFields.Length == 0)
			return (new(), new());
		
		// Determine current system language
		var isEng = lang == SystemLanguage.English;
        
		// Initialize error messages (for display purpose)
		var errorMessages = new Dictionary<int, List<string>>();
		
		// Initialize key pair dictionary (for handle purpose)
		// Key: root element
		// Value: duplicate elements with root
		var duplicates = new Dictionary<int, List<int>>();
        
		// Initialize a map to track seen keys for each field
		var fieldToSeenKeys = new Dictionary<string, Dictionary<string, int>>();
		foreach (var field in scanningFields.Select(f => f.ToUpperInvariant()))
		{
			fieldToSeenKeys[field] = new Dictionary<string, int>();
		}
		
		// Default row index set to second row, as first row is header
		var currDataRow = 2;
		for (int i = 0; i < records.Count; i++)
		{
			var record = records[i];
            
			// Initialize row errors
			var rowErrors = new List<string>();
			
			// Check duplicates for each scanning field
			foreach (var field in scanningFields.Select(f => f.ToUpperInvariant()))
			{
				string? fieldValue = field switch
				{
					var name when name == nameof(Supplier.SupplierName).ToUpperInvariant() => record.SupplierName?.Trim()
						.ToUpperInvariant(),
					_ => null
				};

				// Skip if the field value is null or empty
				if (string.IsNullOrEmpty(fieldValue))
					continue;

				// Check if the key has already seen
				var seenKeys = fieldToSeenKeys[field];
				if (seenKeys.ContainsKey(fieldValue))
				{
					// Retrieve the first index where the duplicate was seen
					var firstItemIndex = seenKeys[fieldValue];

					// Add the current index to the duplicates list
					if (!duplicates.ContainsKey(firstItemIndex))
					{
						duplicates[firstItemIndex] = new List<int>();
					}

					duplicates[firstItemIndex].Add(i);

					// Add duplicate error message
					rowErrors.Add(isEng
						? $"Duplicate data for field '{field}': '{fieldValue}'"
						: $"Dữ liệu bị trùng cho trường '{field}': '{fieldValue}'");
				}
				else
				{
					// Mark this field value as seen at the current index
					seenKeys[fieldValue] = i;
				}
			}
			
			// If errors exist for specific row, add to the dictionary
			if (rowErrors.Any())
			{
				errorMessages.Add(currDataRow, rowErrors);
			}
            
			// Increment the row counter
			currDataRow++;
		}

		return (errorMessages, duplicates);
	}
}