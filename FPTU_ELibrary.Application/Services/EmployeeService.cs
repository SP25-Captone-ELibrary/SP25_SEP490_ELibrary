using System.Globalization;
using System.Text;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.Roles;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml.Export.ToCollection.Exceptions;
using Serilog;

namespace FPTU_ELibrary.Application.Services
{
	public class EmployeeService : GenericService<Employee, EmployeeDto, Guid>, IEmployeeService<EmployeeDto>
	{
		private readonly ISystemRoleService<SystemRoleDto> _roleService;

		public EmployeeService(
			ISystemRoleService<SystemRoleDto> roleService,
			ISystemMessageService msgService,
			IUnitOfWork unitOfWork, 
			IMapper mapper,
			ILogger logger)
			: base(msgService, unitOfWork, mapper, logger)
		{
			_roleService = roleService;
		}

		public override async Task<IServiceResult> CreateAsync(EmployeeDto dto)
		{
			// Initiate service result
			var serviceResult = new ServiceResult();

			try
			{
				// Check exist email
				var isExistEmail = await _unitOfWork.Repository<Employee, Guid>().AnyAsync(e => e.Email == dto.Email);
				if (isExistEmail)
				{
					return new ServiceResult(ResultCodeConst.Auth_Warning0006,
						await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0006));
				}
				
				// Check exist and valid role
				var getRoleResult = await _roleService.GetByIdAsync(dto.RoleId);
				if (getRoleResult.Data is SystemRoleDto roleDto)
				{
					// Check role type 
					if (roleDto.RoleType != RoleType.Employee.ToString())
					{
						return new ServiceResult(ResultCodeConst.Role_Warning0002,
							await _msgService.GetMessageAsync(ResultCodeConst.Role_Warning0002));
					}
				}
				else if(getRoleResult.Data == null)
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errMsg, "role"));
				}
				
				// Validate inputs using the generic validator
				var validationResult = await ValidatorExtensions.ValidateAsync(dto);
				// Check for valid validations
				if (validationResult != null && !validationResult.IsValid)
				{
					// Convert ValidationResult to ValidationProblemsDetails.Errors
					var errors = validationResult.ToProblemDetails().Errors;
					throw new UnprocessableEntityException("Invalid Validations", errors);
				}
				
				// Process add new entity
				await _unitOfWork.Repository<Employee, Guid>().AddAsync(_mapper.Map<Employee>(dto));
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
			}
			catch(Exception ex)
			{
				_logger.Error(ex.Message);
				throw;
			}
			
			return serviceResult;
		}

		public override async Task<IServiceResult> UpdateAsync(Guid id, EmployeeDto dto)
		{
			// Initiate service result
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
				var existingEntity = await _unitOfWork.Repository<Employee, Guid>().GetByIdAsync(id);
				if (existingEntity == null)
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
						StringUtils.Format(errMsg, nameof(Employee)));
				}

				// Update specific properties
				existingEntity.EmployeeCode = dto.EmployeeCode;
				existingEntity.FirstName = dto.FirstName;
				existingEntity.LastName = dto.LastName;
				existingEntity.Dob = dto.Dob;
				existingEntity.Phone = dto.Phone;
				existingEntity.Address = dto.Address;
				existingEntity.Gender = dto.Gender;
				existingEntity.HireDate = dto.HireDate;
				existingEntity.TerminationDate = dto.TerminationDate;
				
				// Check if there are any differences between the original and the updated entity
				if (!_unitOfWork.Repository<Employee, Guid>().HasChanges(existingEntity))
				{
					serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
					serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
					serviceResult.Data = true;
					return serviceResult;
				}

				// Progress update when all require passed
				await _unitOfWork.Repository<Employee, Guid>().UpdateAsync(existingEntity);

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
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw;
			}

			return serviceResult;
		}

		public override async Task<IServiceResult> DeleteAsync(Guid id)
		{
			// Initiate service result
			var serviceResult = new ServiceResult();

			try
			{
				// Retrieve the entity
				var existingEntity = await _unitOfWork.Repository<Employee, Guid>().GetByIdAsync(id);
				if (existingEntity == null)
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
						StringUtils.Format(errMsg, nameof(Employee)));
				}

				// Check whether employee in the trash bin
				if (!existingEntity.IsDeleted)
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004);
					return new ServiceResult(ResultCodeConst.SYS_Fail0004, 
						StringUtils.Format(errMsg, nameof(Employee)));
				}
				
				// Check employee constraints
				var baseSpec = new BaseSpecification<Employee>(q =>
					( // Has constraint with books
						q.BookCreateByNavigations.Any() || q.BookUpdatedByNavigations.Any() ||
						q.BookEditions.Any() || q.BookResources.Any() ||
						// Has constraint with learning material
						q.LearningMaterialCreateByNavigations.Any() ||
						// Has constraint with borrow & returns
						q.BorrowRecords.Any()
						// With specific employee id 
					) && q.EmployeeId == id);
				var hasConstraints = await _unitOfWork.Repository<Employee, Guid>().AnyAsync(baseSpec);
				if (hasConstraints)
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0007, 
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0007));
				}
				
				// Process add delete entity
				await _unitOfWork.Repository<Employee, Guid>().DeleteAsync(id);
				// Save to DB
				if (await _unitOfWork.SaveChangesAsync() > 0)
				{
					return new ServiceResult(ResultCodeConst.SYS_Success0004, 
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004));
				}
				else
				{
					// Get error msg
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004);

					serviceResult.ResultCode = ResultCodeConst.SYS_Fail0004;
					serviceResult.Message = StringUtils.Format(errMsg, nameof(Employee));
					serviceResult.Data = false;
				}
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw;
			}

			return serviceResult;
		}

		public async Task<IServiceResult> UpdateWithoutValidationAsync(Guid employeeId, EmployeeDto dto)
		{
			// Initiate service result
			var serviceResult = new ServiceResult();

			try
			{
				// Retrieve the entity
				var existingEntity = await _unitOfWork.Repository<Employee, Guid>().GetByIdAsync(employeeId);
				if (existingEntity == null)
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0002, 
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002));
				}

				// Process add update entity
				// Map properties from dto to existingEntity
				_mapper.Map(dto, existingEntity);

				// Check if there are any differences between the original and the updated entity
				if (!_unitOfWork.Repository<Employee, Guid>().HasChanges(existingEntity))
				{
					serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
					serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
					serviceResult.Data = true;
					return serviceResult;
				}

				// Progress update when all require passed
				await _unitOfWork.Repository<Employee, Guid>().UpdateAsync(existingEntity);

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
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke while update employee");
			}

			return serviceResult;
		}

		public async Task<IServiceResult> UpdateRoleAsync(Guid employeeId, int roleId)
		{
			try
			{
				// Get employee by id
				var employee = await _unitOfWork.Repository<Employee, Guid>().GetByIdAsync(employeeId);
				// Get role by id 
				var getRoleResult = await _roleService.GetByIdAsync(roleId);
				if (employee != null 
				    && getRoleResult.Data is SystemRoleDto role)
				{
					// Check is valid role type 
					if (role.RoleType != RoleType.Employee.ToString())
					{
						return new ServiceResult(ResultCodeConst.Role_Warning0002,
							await _msgService.GetMessageAsync(ResultCodeConst.Role_Warning0002), false);
					}
					
					// Progress update user role 
					employee.RoleId = role.RoleId;
					
					// Save to DB
					var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
					if (isSaved) // Save success
					{
						return new ServiceResult(ResultCodeConst.SYS_Success0003,
							await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
					}
					
					// Fail to update
					return new ServiceResult(ResultCodeConst.SYS_Fail0003,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
				}

				var errMSg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002); 
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMSg, "role or user"), false);
			}catch(Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress update user role");	
			}
		}

		public async Task<IServiceResult> UpdateEmailVerificationCodeAsync(Guid employeeId, string code)
		{
			// Initiate service result
			var serviceResult = new ServiceResult();

			try
			{
				// Retrieve the entity
				var existingEntity = await _unitOfWork.Repository<Employee, Guid>().GetByIdAsync(employeeId);
				if (existingEntity == null)
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0002, 
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), false);
				}

				// Update email verification code
				existingEntity.EmailVerificationCode = code;
				
				// Check if there are any differences between the original and the updated entity
				if (!_unitOfWork.Repository<Employee, Guid>().HasChanges(existingEntity))
				{
					serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
					serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
					serviceResult.Data = true;
					return serviceResult;
				}

				// Progress update when all require passed
				await _unitOfWork.Repository<Employee, Guid>().UpdateAsync(existingEntity);

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
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke while confirm email verification code");
			}

			return serviceResult;
		}
		
		public async Task<IServiceResult> ChangeActiveStatusAsync(Guid employeeId)
		{
			try
			{
				// Check exist employee
				var existingEntity = await _unitOfWork.Repository<Employee, Guid>().GetByIdAsync(employeeId);
				if (existingEntity == null)
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errMsg, "employee"));
				}
				
				// Progress change active status
				existingEntity.IsActive = !existingEntity.IsActive;
				
				// Progress update when all require passed
				await _unitOfWork.Repository<Employee, Guid>().UpdateAsync(existingEntity);

				// Save changes to DB
				var rowsAffected = await _unitOfWork.SaveChangesAsync();
				if (rowsAffected == 0)
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0003,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
				}

				// Mark as update success
				return new ServiceResult(ResultCodeConst.SYS_Success0003,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress change employee active status");
			}
		}

		public async Task<IServiceResult> SoftDeleteAsync(Guid employeeId)
		{
			try
			{
				// Check exist employee
				var existingEntity = await _unitOfWork.Repository<Employee, Guid>().GetByIdAsync(employeeId);
				if (existingEntity == null)
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errMsg, "employee"));
				}

				// Check if employee account already mark as deleted
				if (existingEntity.IsDeleted)
				{
					// Get error msg
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);

					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errMsg, nameof(Employee)));
				}
				
				// Update delete status
				existingEntity.IsDeleted = true;
				
				// Save changes to DB
				var rowsAffected = await _unitOfWork.SaveChangesAsync();
				if (rowsAffected == 0)
				{
					// Get error msg
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004);

					return new ServiceResult(ResultCodeConst.SYS_Fail0004,
						StringUtils.Format(errMsg, nameof(Employee)));
				}

				// Mark as update success
				return new ServiceResult(ResultCodeConst.SYS_Success0004,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);	
				throw new Exception("Error invoke when process soft delete employee");	
			}
		}

		public override async Task<IServiceResult> GetAllWithSpecAsync(
			ISpecification<Employee> specification,
			bool tracked = true)
		{
			try
			{
				// Try to parse specification to EmployeeSpecification
				var employeeSpec = specification as EmployeeSpecification;
				// Check if specification is null
				if (employeeSpec == null)
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0002,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
				}				
				
				// Define a local Mapster configuration
				var localConfig = new TypeAdapterConfig();
				localConfig.NewConfig<Employee, EmployeeDto>()
					.Ignore(dest => dest.PasswordHash!)
					.Ignore(dest => dest.RoleId)
					.Ignore(dest => dest.EmailConfirmed)
					.Ignore(dest => dest.TwoFactorEnabled)
					.Ignore(dest => dest.PhoneNumberConfirmed)
					.Ignore(dest => dest.TwoFactorSecretKey!)
					.Ignore(dest => dest.TwoFactorBackupCodes!)
					.Ignore(dest => dest.PhoneVerificationCode!)
					.Ignore(dest => dest.EmailVerificationCode!)
					.Ignore(dest => dest.PhoneVerificationExpiry!)
					.Map(dto => dto.Role, src => src.Role) 
					.AfterMapping((src, dest) => { dest.Role.RoleId = 0; });
				
				// Count total employees
				var totalEmployeeWithSpec = await _unitOfWork.Repository<Employee, Guid>().CountAsync(employeeSpec);
				// Count total page
				var totalPage = (int)Math.Ceiling((double)totalEmployeeWithSpec / employeeSpec.PageSize);
				
				// Set pagination to specification after count total employees 
				if (employeeSpec.PageIndex > totalPage 
					|| employeeSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
				{
					employeeSpec.PageIndex = 1; // Set default to first page
				}
				
				// Apply pagination
				employeeSpec.ApplyPaging(
					skip: employeeSpec.PageSize * (employeeSpec.PageIndex - 1), 
					take: employeeSpec.PageSize);
				
				// Get all with spec
				var entities = await _unitOfWork.Repository<Employee, Guid>()
					.GetAllWithSpecAsync(employeeSpec, tracked);
				
				if (entities.Any()) // Exist data
				{
					// Convert to dto collection 
					var employeeDtos = entities.Adapt<IEnumerable<EmployeeDto>>(localConfig);
					
					// Pagination result 
					var paginationResultDto = new PaginatedResultDto<EmployeeDto>(employeeDtos,
						employeeSpec.PageIndex, employeeSpec.PageSize, totalPage);
					
					// Response with pagination 
					return new ServiceResult(ResultCodeConst.SYS_Success0002, 
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
				}
				
				// Not found any data
				return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
					// Mapping entities to dto and ignore sensitive user data
					entities.Adapt<IEnumerable<EmployeeDto>>(localConfig));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress get all data");
			}
		}
		
		public async Task<IServiceResult> GetByEmailAndPasswordAsync(string email, string password)
		{
			// Query specification
			var baseSpec = new BaseSpecification<Employee>(u => u.Email.Equals(email));
			// Include job role
			baseSpec.ApplyInclude(q => 
				q.Include(e => e.Role));

			// Get user by query specification
			var employee = await _unitOfWork.Repository<Employee, Guid>().GetWithSpecAsync(baseSpec);

			// Verify whether the given password match password hash or not
			if (employee == null || !HashUtils.VerifyPassword(password, employee.PasswordHash!))
				return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));

			return new ServiceResult(ResultCodeConst.SYS_Success0002, 
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
				_mapper.Map<EmployeeDto?>(employee));
		}

		public async Task<IServiceResult> GetByEmailAsync(string email)
		{
			// Query specification
			var baseSpec = new BaseSpecification<Employee>(u => u.Email.Equals(email));
			// Include job role
			baseSpec.ApplyInclude(q => 
				q.Include(e => e.Role));

			// Get user by query specification
			var employee = await _unitOfWork.Repository<Employee, Guid>().GetWithSpecAsync(baseSpec);

			// Not exist employee
			if (employee == null)
				return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));

			return new ServiceResult(ResultCodeConst.SYS_Success0002, 
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
				_mapper.Map<EmployeeDto?>(employee));
		}
		
		public async Task<IServiceResult> UpdateMfaSecretAndBackupAsync(string email, string mfaKey, IEnumerable<string> backupCodes)
        {
            try
            {
                // Get employee by id 
                var employee = await _unitOfWork.Repository<Employee, Guid>().GetWithSpecAsync(
	                new BaseSpecification<Employee>(e => e.Email == email));
                if (employee == null) // Not found
                {
                    var errorMsg = await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0006);
                    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                        StringUtils.Format(errorMsg, "account"));
                }
                
                // Progress update MFA key and backup codes
                employee.TwoFactorSecretKey = mfaKey;
                employee.TwoFactorBackupCodes = string.Join(",", backupCodes);
                
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
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw new Exception("Error invoke when progress update MFA key");
            }    
        }
		
		public async Task<IServiceResult> UpdateMfaStatusAsync(Guid employeeId)
		{
			try
			{
				// Get employee by id 
				var employee = await _unitOfWork.Repository<Employee, Guid>().GetByIdAsync(employeeId);
				if (employee == null) // Not found
				{
					var errorMsg = await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0006);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errorMsg, "employee"));
				}
                
				// Change account 2FA status
				employee.TwoFactorEnabled = true;
                
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
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress update MFA key");
			}    
		}
		
		public async Task<IServiceResult> ImportAsync(IFormFile? file, DuplicateHandle duplicateHandle, string? columnSeparator, string? encodingType, string[]? scanningFields)
		{
			try
			{
				// Check exist file
				if (file == null || file.Length == 0)
				{
					return new ServiceResult(ResultCodeConst.File_Warning0002,
						await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0002));
				}

				// Validate import file 
				var validationResult = await ValidatorExtensions.ValidateAsync(file);
				if (validationResult != null && validationResult.IsValid)
				{
					// Response the uploaded file is not supported
					throw new UnprocessableEntityException(
						await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0001),
						validationResult.ToProblemDetails().Errors);
				}

				// Csv config
				var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
				{
					HasHeaderRecord = true,
					Delimiter = columnSeparator,
					HeaderValidated = null,
					MissingFieldFound = null
				};

				// Process read csv file
				List<EmployeeCsvRecord> records =
					CsvUtils.ReadCsvOrExcel<EmployeeCsvRecord>(file, csvConfig, encodingType);

				// Get all employee role
				var employeeRoles = 
					(await _roleService.GetAllByRoleType(RoleType.Employee)).Data as List<SystemRoleDto>;
				if (employeeRoles == null || !employeeRoles.Any())
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0008, 
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008));
				}
				
				// Determine system lang
				var lang = (SystemLanguage) EnumExtensions.GetValueFromDescription<SystemLanguage>(LanguageContext.CurrentLanguage);
				var isEng = lang == SystemLanguage.English;
				
				// Detect record errors
				var detectResult = await DetectWrongDataAsync(records, scanningFields, employeeRoles, lang);
				if (detectResult.Any())
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0008,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008),
						new ImportErrorResultDto
						{
							Errors = detectResult
						});
				}

				// Additional message
				var additionalMsg = string.Empty;
				// Detect duplicates
				var detectDuplicateResult = DetectDuplicates(records, scanningFields);
				if (detectDuplicateResult.Any())
				{
					// Handling based on DuplicateHandle
					switch (duplicateHandle)
					{
						case DuplicateHandle.Allow:
							// Allow all duplicate items 
							break;
						case DuplicateHandle.Replace:
							foreach (var duplicateIdx in detectDuplicateResult.Keys.OrderByDescending(idx => idx))
							{
								// Remove first duplicate element detected  
								records.RemoveAt(duplicateIdx);
							}

							additionalMsg = isEng 
								? $"{detectDuplicateResult.Keys.Count} data have been replaced" 
								: $"{detectDuplicateResult.Keys.Count} đã bị lượt bỏ";
							break;
						case DuplicateHandle.Skip:
							// Count total skip elements
							var totalSkips = 0;
							// Remove all duplicates 
							foreach (var duplicateIdx in detectDuplicateResult.Keys.OrderByDescending(idx => idx))
							{
								// Remove all duplicates related to current key
								foreach (var otherIdx in detectDuplicateResult[duplicateIdx].OrderByDescending(idx => idx))
								{
									records.RemoveAt(otherIdx);
									
									// Increase total skip
									++totalSkips;
								}

								// Remove first element, after remove all its duplicates
								records.RemoveAt(duplicateIdx);
								
								// Increase total skip
								++totalSkips;
							}
							
							additionalMsg = isEng 
								? $"{totalSkips} data have been replaced" 
								: $"{totalSkips} đã bị lượt bỏ";
							break;
					}
				}

				// Convert to employee dto collection
				var employeeDtos = records.ToEmployeeDtosForImport(employeeRoles);
				
				// Progress import data
				await _unitOfWork.Repository<Employee, Guid>().AddRangeAsync(_mapper.Map<List<Employee>>(employeeDtos));
				// Save to DB
				if (await _unitOfWork.SaveChangesAsync() > 0)
				{
					var respMsg = !string.IsNullOrEmpty(additionalMsg)
						? $"Import {employeeDtos.Count} data successfully, {additionalMsg}"
						: $"Import {employeeDtos.Count} data successfully";
					return new ServiceResult(ResultCodeConst.SYS_Success0005, respMsg, true);
				}
				
				return new ServiceResult(ResultCodeConst.SYS_Warning0005, 
					"No data effected", false);
			}
			catch (UnprocessableEntityException)
			{
				throw;
			}
			catch (TypeConverterException ex)
			{
				var lang = (SystemLanguage) EnumExtensions.GetValueFromDescription<SystemLanguage>(LanguageContext.CurrentLanguage);
				// Extract row information if available
				var rowNumber = ex.Data.Contains("Row") ? ex.Data["Row"] : "unknown";

				// Generate an appropriate error message
				var errMsg = lang == SystemLanguage.English 
					? $"Wrong data type at row {rowNumber}" 
					: $"Sai kiểu dữ liệu ở dòng {rowNumber}";
				
				throw new BadRequestException(errMsg);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke while import employees");
			}
		}

		private async Task<List<string>> DetectWrongDataAsync(
			List<EmployeeCsvRecord> records, 
			string[]? scanningFields,
			List<SystemRoleDto> employeeRoles,
			SystemLanguage lang)
		{
			// Check system lang
			var isEng = lang == SystemLanguage.English;
			
			// Initialize list of errors
			var errorMessages = new List<string>();
			// Default row index set to second row, as first row is header
			var currDataRow = 2; 
			
			foreach (var record in records)
			{
				// Initialize error msg
				var errMsg = string.Empty;
				// Set default as correct
				var isError = false;
				
				// Check role exist
				if (employeeRoles != null && employeeRoles.All(x => 
					    x.EnglishName != record.Role))
				{
					isError = true;
					errMsg = isEng ? "Not exist role" : "Không tìm thấy role";
				}
				
				// Check valid datetime
				if (!string.IsNullOrEmpty(record.Dob) // Invalid date of birth
				    && (!DateTime.TryParse(record.Dob, out var dob) // Cannot parse
				        || dob < new DateTime(1900, 1, 1)     // Too old
				        || dob > DateTime.Now))                            // In the future
				{
					isError = true;
					errMsg = isEng ? "Not valid date of birth" : "Ngày sinh không hợp lệ";
				}else if (!string.IsNullOrEmpty(record.HireDate) // Invalid hire date
				          && !DateTime.TryParse(record.HireDate, out _))
				{
					isError = true;
					errMsg = isEng ? "Not valid hire date" : "Ngày bắt đầu làm việc không hợp lệ";
				}else if (!string.IsNullOrEmpty(record.TerminationDate) // Invalid terminate date
				          && !DateTime.TryParse(record.TerminationDate, out _))
				{
					isError = true;
					errMsg = isEng ? "Not valid hire date" : "Ngày nghỉ việc không hợp lệ";
				}

				if (scanningFields != null)
				{
					// Initialize base spec
					BaseSpecification<Employee>? empBaseSpec = null;
					BaseSpecification<User>? userBaseSpec = null;
					
					// Iterate each fields to add criteria scanning logic
					foreach (var field in scanningFields)
					{
						var normalizedField = field.ToUpperInvariant();
						
						// Building query to check duplicates on Employee entity
						var newEmpSpec = normalizedField switch
						{
							var email when email == nameof(Employee.Email).ToUpperInvariant() =>
								new BaseSpecification<Employee>(e => e.Email.Equals(record.Email)),
							var phone when phone == nameof(Employee.Phone).ToUpperInvariant() =>
								new BaseSpecification<Employee>(e => e.Phone == record.Phone),
							_ => null
						};
						
						// Building query to check duplicates on User entity
						var newUserSpec = normalizedField switch
						{
							var email when email == nameof(Employee.Email).ToUpperInvariant() =>
								new BaseSpecification<User>(e => e.Email.Equals(record.Email)),
							var phone when phone == nameof(Employee.Phone).ToUpperInvariant() =>
								new BaseSpecification<User>(e => e.Phone == record.Phone),
							_ => null
						};

						if (newEmpSpec != null) // Found new employee spec
						{
							// Combine specifications with AND logic
							empBaseSpec = empBaseSpec == null
								? newEmpSpec
								: empBaseSpec.Or(newEmpSpec);
						}
						
						if (newUserSpec != null) // Found new user spec
						{
							// Combine specifications with AND logic
							userBaseSpec = userBaseSpec == null
								? newUserSpec
								: userBaseSpec.Or(newUserSpec);
						}
					}

					// Check exist with spec
					if (
						// Any employee found
						(empBaseSpec != null && await _unitOfWork.Repository<Employee, Guid>().AnyAsync(empBaseSpec)) || 
						// Any user found
						(userBaseSpec != null && await _unitOfWork.Repository<User, Guid>().AnyAsync(userBaseSpec))
					)
					{
						isError = true;
						errMsg = isEng ? "Duplicate email or phone" : "Email hoặc số điện thoại bị trùng";
					}
				}

				if (isError) // Error found
				{
					// Custom message
					errMsg = isEng 
						? $"At row {currDataRow}: {errMsg}"
						: $"Dòng {currDataRow}: {errMsg}";
					// Add error msg
					errorMessages.Add(errMsg);
				}
				// Increase curr row
				currDataRow++;
			}

			return errorMessages;
		}

		private Dictionary<int, List<int>> DetectDuplicates(List<EmployeeCsvRecord> records, string[]? scanningFields)
		{
			if (scanningFields == null || scanningFields.Length == 0)
				return new Dictionary<int, List<int>>();

			var duplicates = new Dictionary<int, List<int>>();
			var keyToIndexMap = new Dictionary<string, int>();
			var seenKeys = new HashSet<string>();

			for (int i = 0; i < records.Count; i++)
			{
				var record = records[i];

				// Generate a unique key based on scanning fields
				var key = string.Join("|", scanningFields.Select(field => 
				{
					var normalizedField = field.ToUpperInvariant();
					return normalizedField switch
					{
						var email when email == nameof(Employee.Email).ToUpperInvariant() => record.Email.Trim().ToUpperInvariant(),
						var phone when phone == nameof(Employee.Phone).ToUpperInvariant() => record.Phone?.Trim().ToUpperInvariant(),
						_ => null
					};
				}).Where(value => !string.IsNullOrEmpty(value)));

				if (string.IsNullOrEmpty(key))
					continue;

				// Check if the key is already seen
				if (seenKeys.Contains(key))
				{
					// Find the first item of the duplicate key
					var firstItemIndex = keyToIndexMap[key];

					// Add the current index to the list of duplicates for this key
					if (!duplicates.ContainsKey(firstItemIndex))
					{
						duplicates[firstItemIndex] = new List<int>();
					}

					duplicates[firstItemIndex].Add(i);
				}
				else
				{
					// Add the key
					seenKeys.Add(key);
					// map it to the current index
					keyToIndexMap[key] = i;
				}
			}

			return duplicates;
		}
	}
}
