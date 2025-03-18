using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper.Configuration;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Cloudinary;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Dtos.Payments.PayOS;
using FPTU_ELibrary.Application.Dtos.Roles;
using FPTU_ELibrary.Application.Dtos.Users;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Hubs;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Serilog;
using RoleEnum = FPTU_ELibrary.Domain.Common.Enums.Role;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FPTU_ELibrary.Application.Services
{
	public class UserService : GenericService<User, UserDto, Guid>, IUserService<UserDto>
	{
		// Lazy services
		private readonly Lazy<ILibraryCardService<LibraryCardDto>> _libraryCardService;
		private readonly Lazy<ILibraryCardPackageService<LibraryCardPackageDto>> _cardPackageService;
		private readonly Lazy<IBorrowRequestService<BorrowRequestDto>> _borrowReqService;
		private readonly Lazy<IBorrowRecordService<BorrowRecordDto>> _borrowRecService;
		private readonly Lazy<IReservationQueueService<ReservationQueueDto>> _reservationService;
		
		private readonly IEmailService _emailService;
		private readonly ISystemRoleService<SystemRoleDto> _roleService;
		private readonly ICloudinaryService _cloudService;
		private readonly IServiceProvider _serviceProvider;
		private readonly IEmployeeService<EmployeeDto> _employeeService;
		private readonly IPaymentMethodService<PaymentMethodDto> _paymentMethodService;
		
		private readonly AppSettings _appSettings;
		private readonly PayOSSettings _payOsSettings;
		private readonly BorrowSettings _borrowSettings;
		private readonly PaymentSettings _paymentSettings;

		public UserService(
			// Lazy services
			Lazy<IBorrowRequestService<BorrowRequestDto>> borrowReqService,
			Lazy<IBorrowRecordService<BorrowRecordDto>> borrowRecService,
			Lazy<IReservationQueueService<ReservationQueueDto>> reservationService,
			Lazy<ILibraryCardService<LibraryCardDto>> libraryCardService,
			Lazy<ILibraryCardPackageService<LibraryCardPackageDto>> cardPackageService,
			
			// Normal services
			ILogger logger,
			ICloudinaryService cloudService,
			ISystemMessageService msgService,
			IEmailService emailService,
			IEmployeeService<EmployeeDto> employeeService,
			IOptionsMonitor<AppSettings> monitor,
			IOptionsMonitor<BorrowSettings> monitor1,
			IOptionsMonitor<PaymentSettings> monitor2,
			IOptionsMonitor<PayOSSettings> monitor3,
			ISystemRoleService<SystemRoleDto> roleService,
			IPaymentMethodService<PaymentMethodDto> paymentMethodService,
			TokenValidationParameters tokenValidationParams,
			IUnitOfWork unitOfWork,
			IMapper mapper, IServiceProvider serviceProvider) 
			: base(msgService, unitOfWork, mapper, logger)
		{
			_borrowReqService = borrowReqService;
			_borrowRecService = borrowRecService;
			_reservationService = reservationService;
			_roleService = roleService;
			_cloudService = cloudService;
			_emailService = emailService;
			_employeeService = employeeService;
			_serviceProvider = serviceProvider;
			_libraryCardService = libraryCardService;
			_cardPackageService = cardPackageService;
			_paymentMethodService = paymentMethodService;
			_appSettings = monitor.CurrentValue;
			_borrowSettings = monitor1.CurrentValue;
			_payOsSettings = monitor3.CurrentValue;
			_paymentSettings = monitor2.CurrentValue;
		}

		public override async Task<IServiceResult> GetByIdAsync(Guid id)
		{
			// Build spec
			var baseSpec = new BaseSpecification<User>(u => u.UserId.Equals(id));
			// Apply include
			baseSpec.ApplyInclude(u => u.Include(u => u.Role));
			// Get user by query specification
			var existedUser = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(baseSpec);
			if (existedUser is null)
			{
				// Data not found or empty
				return new ServiceResult(ResultCodeConst.SYS_Warning0004,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
			}

			// Define a local Mapster configuration
			var localConfig = new TypeAdapterConfig();
			localConfig.NewConfig<User, UserDto>()
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
				.Ignore(dest => dest.Transactions)
				.Map(dto => dto.Role, src => src.Role)
				.AfterMapping((src, dest) => { dest.Role.RoleId = 0; });

			return new ServiceResult(ResultCodeConst.SYS_Success0002,
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
				existedUser.Adapt<UserDto>(localConfig));
		}

		public override async Task<IServiceResult> GetAllWithSpecAsync(ISpecification<User> spec, bool tracked = true)
		{
			try
			{
				// Try to parse specification to UserSpecification
				var userSpec = spec as UserSpecification;
				// Check if specification is null
				if (userSpec == null)
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0002,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
				}

				// Define a local Mapster configuration
				var localConfig = new TypeAdapterConfig();
				localConfig.NewConfig<User, UserDto>()
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

				// Count total users
				var totalUserWithSpec = await _unitOfWork.Repository<User, Guid>().CountAsync(userSpec);
				// Count total page
				var totalPage = (int)Math.Ceiling((double)totalUserWithSpec / userSpec.PageSize);

				// Set pagination to specification after count total users 
				if (userSpec.PageIndex > totalPage
				    || userSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
				{
					userSpec.PageIndex = 1; // Set default to first page
				}

				// Apply pagination
				userSpec.ApplyPaging(
					skip: userSpec.PageSize * (userSpec.PageIndex - 1),
					take: userSpec.PageSize);

				var entities = await _unitOfWork.Repository<User, Guid>().GetAllWithSpecAsync(spec, tracked);
				if (entities.Any())
				{
					// Convert to dto collection 
					var userDtos = entities.Adapt<IEnumerable<UserDto>>(localConfig);

					// Pagination result 
					var paginationResultDto = new PaginatedResultDto<UserDto>(userDtos,
						userSpec.PageIndex, userSpec.PageSize, totalPage, totalUserWithSpec);

					// Response with pagination 
					return new ServiceResult(ResultCodeConst.SYS_Success0002,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
				}

				// Not found any data
				return new ServiceResult(ResultCodeConst.SYS_Warning0004,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
					// Mapping entities to dto and ignore sensitive user data
					entities.Adapt<IEnumerable<UserDto>>(localConfig));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress get all data");
			}
		}

		public override async Task<IServiceResult> CreateAsync(UserDto dto)
		{
			// Initiate service result
			var serviceResult = new ServiceResult();

			try
			{
				// Process add new entity
				await _unitOfWork.Repository<User, Guid>().AddAsync(_mapper.Map<User>(dto));
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
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke while create user");
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
				var existingEntity = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(id);
				if (existingEntity == null)
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errMsg, nameof(User)).ToLower());
				}

				// Check whether user in the trash bin
				if (!existingEntity.IsDeleted)
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0004,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
				}

				// Process add delete entity
				await _unitOfWork.Repository<User, Guid>().DeleteAsync(id);
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
				throw new Exception("Error invoke when process delete user");
			}

			return serviceResult;
		}

		public override async Task<IServiceResult> UpdateAsync(Guid id, UserDto dto)
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
				var existingEntity = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(id);
				if (existingEntity == null)
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errMsg, typeof(User).ToString().ToLower()));
				}

				// Current local datetime
				var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
					// Vietnam timezone
					TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

				// Update properties
				existingEntity.FirstName = dto.FirstName ?? string.Empty;
				existingEntity.LastName = dto.LastName ?? string.Empty;
				existingEntity.Dob = dto.Dob;
				existingEntity.Phone = dto.Phone;
				existingEntity.Address = dto.Address;
				existingEntity.Gender = dto.Gender;
				existingEntity.ModifiedDate = currentLocalDateTime;

				// Check if there are any differences between the original and the updated entity
				if (!_unitOfWork.Repository<User, Guid>().HasChanges(existingEntity))
				{
					serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
					serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
					serviceResult.Data = true;
					return serviceResult;
				}

				// Progress update when all require passed
				await _unitOfWork.Repository<User, Guid>().UpdateAsync(existingEntity);

				// Save changes to DB
				var rowsAffected = await _unitOfWork.SaveChangesAsync();
				if (rowsAffected == 0)
				{
					serviceResult.ResultCode = ResultCodeConst.SYS_Fail0003;
					serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003);
					serviceResult.Data = false;
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
				throw new Exception("Error invoke when process update user");
			}

			return serviceResult;
		}

		public async Task<IServiceResult> UpdateProfileAsync(string email, UserDto dto)
		{
			// Initiate service result
			var serviceResult = new ServiceResult();

			try
			{
				// Determine current lang 
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
				var existingEntity = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(
					new BaseSpecification<User>(u => u.Email == email));
				if (existingEntity == null)
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errMsg, isEng ? isEng ? "user" : "bạn đọc" : "bạn đọc"));
				}

				// Update specific properties
				existingEntity.FirstName = dto.FirstName ?? string.Empty;
				existingEntity.LastName = dto.LastName ?? string.Empty;
				existingEntity.Dob = dto.Dob;
				existingEntity.Phone = dto.Phone;
				existingEntity.Address = dto.Address;
				existingEntity.Gender = dto.Gender;
				existingEntity.Avatar = dto.Avatar;

				// Check if there are any differences between the original and the updated entity
				if (!_unitOfWork.Repository<User, Guid>().HasChanges(existingEntity))
				{
					serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
					serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
					serviceResult.Data = true;
					return serviceResult;
				}

				// Progress update when all require passed
				await _unitOfWork.Repository<User, Guid>().UpdateAsync(existingEntity);

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
				throw;
			}

			return serviceResult;
		}

		public async Task<IServiceResult> DeleteRangeAsync(Guid[] userIds)
		{
			try
			{
				// Get all matching user 
				// Build spec
				var baseSpec = new BaseSpecification<User>(e => userIds.Contains(e.UserId));
				var userEntities = await _unitOfWork.Repository<User, Guid>()
					.GetAllWithSpecAsync(baseSpec);
				// Check if any data already soft delete
				var userList = userEntities.ToList();
				if (userList.Any(x => !x.IsDeleted))
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0004,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
				}

				// Process delete range
				await _unitOfWork.Repository<User, Guid>().DeleteRangeAsync(userIds);
				// Save to DB
				if (await _unitOfWork.SaveChangesAsync() > 0)
				{
					var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0008);
					return new ServiceResult(ResultCodeConst.SYS_Success0008,
						StringUtils.Format(msg, userList.Count.ToString()), true);
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
				throw new Exception("Error invoke when process delete range user");
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process delete range user");
			}
		}

		public async Task<IServiceResult> UpdateWithoutValidationAsync(Guid userId, UserDto dto)
		{
			// Initiate service result
			var serviceResult = new ServiceResult();

			try
			{
				// Retrieve the entity
				var existingEntity = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(userId);
				if (existingEntity == null)
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0002,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002));
				}

				// Process add update entity
				// Map properties from dto to existingEntity
				_mapper.Map(dto, existingEntity);

				// Check if there are any differences between the original and the updated entity
				if (!_unitOfWork.Repository<User, Guid>().HasChanges(existingEntity))
				{
					serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
					serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
					serviceResult.Data = true;
					return serviceResult;
				}

				// Progress update when all require passed
				await _unitOfWork.Repository<User, Guid>().UpdateAsync(existingEntity);

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
				throw new Exception("Error invoke while update user");
			}

			return serviceResult;
		}

		public async Task<IServiceResult> UpdateEmailVerificationCodeAsync(Guid userId, string code)
		{
			// Initiate service result
			var serviceResult = new ServiceResult();

			try
			{
				// Retrieve the entity
				var existingEntity = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(userId);
				if (existingEntity == null)
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0002,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), false);
				}

				// Update email verification code
				existingEntity.EmailVerificationCode = code;

				// Check if there are any differences between the original and the updated entity
				if (!_unitOfWork.Repository<User, Guid>().HasChanges(existingEntity))
				{
					serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
					serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
					serviceResult.Data = true;
					return serviceResult;
				}

				// Progress update when all require passed
				await _unitOfWork.Repository<User, Guid>().UpdateAsync(existingEntity);

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

		public async Task<IServiceResult> UpdateRoleAsync(Guid userId, int roleId)
		{
			try
			{
				// Get user by id
				var user = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(userId);
				// Get role by id 
				var getRoleResult = await _roleService.GetByIdAsync(roleId);
				if (user != null
				    && getRoleResult.Data is SystemRoleDto role)
				{
					// Check is valid role type 
					if (role.RoleType != RoleType.User.ToString())
					{
						return new ServiceResult(ResultCodeConst.Role_Warning0002,
							await _msgService.GetMessageAsync(ResultCodeConst.Role_Warning0002));
					}

					// Progress update user role 
					user.RoleId = role.RoleId;

					// Save to DB
					var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
					if (isSaved) // Save success
					{
						return new ServiceResult(ResultCodeConst.SYS_Success0003,
							await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
					}

					// Fail to update
					return new ServiceResult(ResultCodeConst.SYS_Fail0003,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
				}

				var errMSg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMSg, "role or user"));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress update user role");
			}
		}

		public async Task<IServiceResult> GetByEmailAndPasswordAsync(string email, string password)
		{
			try
			{
				// Query specification
				var baseSpec = new BaseSpecification<User>(u => u.Email.Equals(email));
				// Include job role
				baseSpec.ApplyInclude(q =>
					q.Include(u => u.Role));

				// Get user by query specification
				var user = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(baseSpec);

				// Verify whether the given password match password hash or not
				if (user == null || !HashUtils.VerifyPassword(password, user.PasswordHash!))
					return new ServiceResult(ResultCodeConst.SYS_Warning0004,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));

				return new ServiceResult(ResultCodeConst.SYS_Success0002,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
					_mapper.Map<UserDto?>(user));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke while get user by email and password");
			}
		}

		public async Task<IServiceResult> GetByEmailAsync(string email)
		{
			try
			{
				// Query specification
				var baseSpec = new BaseSpecification<User>(u => u.Email.Equals(email));
				// Include job role
				baseSpec.ApplyInclude(q => q
					.Include(u => u.LibraryCard)	
					.Include(u => u.Role)
				);

				// Get user by query specification
				var user = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(baseSpec);

				// Not exist user
				if (user == null)
					return new ServiceResult(ResultCodeConst.SYS_Warning0004,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
				// Response read success
				return new ServiceResult(ResultCodeConst.SYS_Success0002,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
					_mapper.Map<UserDto?>(user));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke while get user by email");
			}
		}

		public async Task<IServiceResult> GetPendingLibraryActivityAsync(Guid libraryCardId)
		{
			try
			{
				// Build spec
				var userSpec = new BaseSpecification<User>(u => u.LibraryCardId == libraryCardId);
				// Retrieve user information by lib card id 
				var user = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(userSpec);
				if (user == null)
				{
					// Msg: Data not found or empty
					return new ServiceResult(ResultCodeConst.SYS_Warning0004,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
						// Default user pending activity
						new UserPendingActivityDto());
				}
				
				// Retrieve all requesting items
				var pendingBorrowRequests = (await _borrowReqService.Value.GetAllPendingRequestByLibCardIdAsync(
					libraryCardId: libraryCardId)).Data as List<GetBorrowRequestDto>;
				// Retrieve all borrowing items
				var activeBorrowRecords = (await _borrowRecService.Value.GetAllActiveRecordByLibCardIdAsync(
					libraryCardId: libraryCardId)).Data as List<GetBorrowRecordDto>;
				// Retrieve all reserving items
				var currentReservationQueues = (await _reservationService.Value.GetAllPendingAndAssignedReservationByLibCardIdAsync(
						libraryCardId: libraryCardId)).Data as List<GetReservationQueueDto>;
				
				// Count total requesting items
				var totalRequesting = pendingBorrowRequests != null && pendingBorrowRequests.Any()
					? pendingBorrowRequests.Select(br => br.LibraryItems.Count).Sum()
					: 0;
				// Count total borrowing items
				var totalBorrowing = activeBorrowRecords != null && activeBorrowRecords.Any()
					? activeBorrowRecords.Select(brd => brd.BorrowRecordDetails.Count).Sum()
					: 0;
				// Count total pending reserving items
				var totalPendingReserving = currentReservationQueues != null && currentReservationQueues.Any()
					? currentReservationQueues.Count(r => r.QueueStatus == ReservationQueueStatus.Pending)
					: 0;
				// Count total assigned reserving items
				var totalAssignedReserving = currentReservationQueues != null && currentReservationQueues.Any()
					? currentReservationQueues.Count(r => r.QueueStatus == ReservationQueueStatus.Assigned)
					: 0;
				// Count remain total
				// Not count pending reservation to total borrow amount as ensuring that user only assigned when borrowing amount smaller than threshold
				var remainTotal = _borrowSettings.BorrowAmountOnceTime - (totalRequesting + totalBorrowing + totalAssignedReserving);
				
				// Initialize summary
				var summaryActivity = new UserPendingActivitySummaryDto()
				{
					TotalRequesting = totalRequesting,
					TotalBorrowing = totalBorrowing,
					TotalPendingReserving = totalPendingReserving,
					TotalAssignedReserving = totalAssignedReserving,
					TotalBorrowOnce = _borrowSettings.BorrowAmountOnceTime,
					RemainTotal = Math.Max(0, remainTotal),
					IsAtLimit = remainTotal <= 0
				};
				
				// Initialize user pending activity
				var userPendingActivity = new UserPendingActivityDto()
				{
					PendingBorrowRequests = pendingBorrowRequests ?? new(),
					ActiveBorrowRecords = activeBorrowRecords ?? new(),
					PendingReservationQueues = currentReservationQueues != null && currentReservationQueues.Any()
						? currentReservationQueues.Where(r => 
							r.QueueStatus == ReservationQueueStatus.Pending).ToList()
						: new(),
					AssignedReservationQueues = currentReservationQueues != null && currentReservationQueues.Any()
						? currentReservationQueues.Where(r => 
							r.QueueStatus == ReservationQueueStatus.Assigned).ToList()
						: new(),
					SummaryActivity = summaryActivity
				};
				
				// Msg: Get data successfully
				return new ServiceResult(ResultCodeConst.SYS_Success0002,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), userPendingActivity);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke whenp rocess get user's pending activity");
			}
		}

		public async Task<IServiceResult> GetPendingLibraryActivitySummaryAsync(Guid libraryCardId)
		{
			try
			{
				// Build spec
                var userSpec = new BaseSpecification<User>(u => u.LibraryCardId == libraryCardId);
                // Apply include
                userSpec.ApplyInclude(q => q.Include(u => u.LibraryCard!));
                // Retrieve user information by lib card id 
                var user = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(userSpec);
                if (user == null)
                {
                	// Msg: Data not found or empty
                	return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                		await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                		// Default user pending activity
                		new UserPendingActivitySummaryDto());
                }
                
                // Count total requesting items
                var reqCountRes = (await _borrowReqService.Value.CountAllPendingRequestByLibCardIdAsync(
	                libraryCardId: libraryCardId)).Data;
				// Try parse to integer
				int.TryParse(reqCountRes?.ToString() ?? "0", out var totalRequesting);
				
				// Count total active records
				var activeRecCountRes = (await _borrowRecService.Value.CountAllActiveRecordByLibCardIdAsync(
					libraryCardId: libraryCardId)).Data;
				// Try parse to integer
				int.TryParse(activeRecCountRes?.ToString() ?? "0", out var totalBorrowing);
				
				// Count total pending reservations
				var pendingReservingCountRes = (await _reservationService.Value.CountAllReservationByLibCardIdAndStatusAsync(
					libraryCardId: libraryCardId,
					status: ReservationQueueStatus.Pending)).Data;
				// Try parse to integer
				int.TryParse(pendingReservingCountRes?.ToString() ?? "0", out var totalPendingReserving);
				
				// Count total pending reservations
				var assignedReservingCountRes = (await _reservationService.Value.CountAllReservationByLibCardIdAndStatusAsync(
					libraryCardId: libraryCardId,
					status: ReservationQueueStatus.Assigned)).Data;
				// Try parse to integer
				int.TryParse(assignedReservingCountRes?.ToString() ?? "0", out var totalAssignedReserving);
				
				// Max amount to borrow 
				var maxAmountToBorrow = user.LibraryCard != null && user.LibraryCard.IsAllowBorrowMore // Is allow to borrow more than default
					? user.LibraryCard.MaxItemOnceTime // Use updated max amount
					: _borrowSettings.BorrowAmountOnceTime; // Use default
				// Count remain total
				var remainTotal = maxAmountToBorrow - (totalRequesting + totalBorrowing + totalPendingReserving + totalAssignedReserving);
				
				// Initialize summary
				var summaryActivity = new UserPendingActivitySummaryDto()
				{
					TotalRequesting = totalRequesting,
					TotalBorrowing = totalBorrowing,
					TotalPendingReserving = totalPendingReserving,
					TotalAssignedReserving = totalAssignedReserving,
					TotalBorrowOnce = maxAmountToBorrow,
					RemainTotal = Math.Max(0, remainTotal),
					IsAtLimit = remainTotal <= 0
				};
				
				// Msg: Get data successfully
				return new ServiceResult(ResultCodeConst.SYS_Success0002,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), summaryActivity);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process get pending library activity summary by lib card id");
			}
		}
		
		public async Task<IServiceResult> CreateAccountByAdminAsync(UserDto dto)
		{
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

				// Custom validation result
				var customErrors = new Dictionary<string, string[]>();
				// Check exist email & employee code
				var isExistUserEmail =
					await _unitOfWork.Repository<User, Guid>().AnyAsync(u => u.Email == dto.Email);
				var isExistEmployeeEmail =
					await _unitOfWork.Repository<Employee, Guid>().AnyAsync(e => e.Email == dto.Email);
				if (isExistEmployeeEmail || isExistUserEmail) // Already exist email
				{
					customErrors.Add(
						StringUtils.ToCamelCase(nameof(User.Email)),
						[await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0006)]);
				}

				// Check whether invoke errors
				if (customErrors.Any()) throw new UnprocessableEntityException("Invalid Data", customErrors);

				// Try to retrieve general member role
				var result = await _roleService.GetByNameAsync(RoleEnum.GeneralMember);
				if (result.ResultCode == ResultCodeConst.SYS_Success0002)
				{
					// Assign role
					dto.RoleId = (result.Data as SystemRoleDto)!.RoleId;
				}
				else
				{
					var errorMsg = await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0006);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errorMsg, "role"));
				}

				// Process add new entity
				await _unitOfWork.Repository<User, Guid>().AddAsync(_mapper.Map<User>(dto));
				// Save to DB
				if (await _unitOfWork.SaveChangesAsync() > 0)
				{
					return new ServiceResult(ResultCodeConst.SYS_Success0001,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001), true);
				}

				// Fail to create
				return new ServiceResult(ResultCodeConst.SYS_Fail0001,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001), false);
			}
			catch (UnprocessableEntityException)
			{
				throw;
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress create account by admin");
			}
		}

		public async Task<IServiceResult> ChangeActiveStatusAsync(Guid userId)
		{
			try
			{
				// Determine current system lang
				var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
					LanguageContext.CurrentLanguage);
				var isEng = lang == SystemLanguage.English;
				
				// Check exist user
				var existingEntity = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(userId);
				if (existingEntity == null)
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errMsg, isEng ? "user" : "bạn đọc"));
				}

				// Progress change active status
				existingEntity.IsActive = !existingEntity.IsActive;

				// Progress update when all require passed
				await _unitOfWork.Repository<User, Guid>().UpdateAsync(existingEntity);

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
				throw new Exception("Error invoke when progress change user active status");
			}
		}

		public async Task<IServiceResult> UpdateMfaSecretAndBackupAsync(string email, string mfaKey,
			IEnumerable<string> backupCodes)
		{
			try
			{
				// Get user by id 
				var user = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(
					new BaseSpecification<User>(u => u.Email == email));
				if (user == null) // Not found
				{
					var errorMsg = await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0006);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errorMsg, "account"));
				}

				// Progress update MFA key and backup codes
				user.TwoFactorSecretKey = mfaKey;
				user.TwoFactorBackupCodes = string.Join(",", backupCodes);

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

		public async Task<IServiceResult> UpdateMfaStatusAsync(Guid userId)
		{
			try
			{
				// Get user by id 
				var user = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(userId);
				if (user == null) // Not found
				{
					var errorMsg = await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0006);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errorMsg, "account"));
				}

				// Change account 2FA status
				user.TwoFactorEnabled = true;

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

		public async Task<IServiceResult> UpdatePasswordWithoutSaveChangesAsync(Guid userId, string password)
		{
			try
			{
				// Retrieve user by id
				var existingEntity = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(userId);
				if (existingEntity == null) // Not found 
				{
					// Mark as failed to update
					return new ServiceResult(ResultCodeConst.SYS_Fail0003,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
				}
				
				// Assign and hash the password
				existingEntity.PasswordHash = HashUtils.HashPassword(password);
				
				// Process update without save
				await _unitOfWork.Repository<User, Guid>().UpdateAsync(existingEntity);
				
				// Mark as update success
				return new ServiceResult(ResultCodeConst.SYS_Success0003,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process update password without save changes");
			}
		}
		
		public async Task<IServiceResult> SoftDeleteAsync(Guid userId)
		{
			try
			{
				// Determine current system lang
				var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
					LanguageContext.CurrentLanguage);
				var isEng = lang == SystemLanguage.English;
				
				// Check exist user
				var existingEntity = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(userId);
				// Check if user account already mark as deleted
				if (existingEntity == null || existingEntity.IsDeleted)
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errMsg, isEng ? "user" : "bạn đọc"));
				}

				// Update delete status
				existingEntity.IsDeleted = true;

				// Save changes to DB
				var rowsAffected = await _unitOfWork.SaveChangesAsync();
				if (rowsAffected == 0)
				{
					// Get error msg
					return new ServiceResult(ResultCodeConst.SYS_Fail0004,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
				}

				// Mark as update success
				return new ServiceResult(ResultCodeConst.SYS_Success0007,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0007));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process soft delete user");
			}
		}

		public async Task<IServiceResult> SoftDeleteRangeAsync(Guid[] userIds)
		{
			try
			{
				// Get all matching user 
				// Build spec
				var baseSpec = new BaseSpecification<User>(e => userIds.Contains(e.UserId));
				var userEntities = await _unitOfWork.Repository<User, Guid>()
					.GetAllWithSpecAsync(baseSpec);
				// Check if any data already soft delete
				var userList = userEntities.ToList();
				if (userList.Any(x => x.IsDeleted))
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0004,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
				}

				// Progress update deleted status to true
				userList.ForEach(x => x.IsDeleted = true);

				// Save changes to DB
				var rowsAffected = await _unitOfWork.SaveChangesAsync();
				if (rowsAffected == 0)
				{
					// Get error msg
					return new ServiceResult(ResultCodeConst.SYS_Fail0004,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
				}

				// Mark as update success
				return new ServiceResult(ResultCodeConst.SYS_Success0007,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0007));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when remove range user");
			}
		}

		public async Task<IServiceResult> UndoDeleteAsync(Guid userId)
		{
			try
			{
				// Determine current system lang
				var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
					LanguageContext.CurrentLanguage);
				var isEng = lang == SystemLanguage.English;
				
				// Check exist user
				var existingEntity = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(userId);
				// Check if user account already mark as deleted
				if (existingEntity == null || !existingEntity.IsDeleted)
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errMsg, isEng ? "user" : "bạn đọc"));
				}

				// Update delete status
				existingEntity.IsDeleted = false;

				// Save changes to DB
				var rowsAffected = await _unitOfWork.SaveChangesAsync();
				if (rowsAffected == 0)
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0004,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
				}

				// Mark as update success
				return new ServiceResult(ResultCodeConst.SYS_Success0009,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0009));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process undo delete user");
			}
		}

		public async Task<IServiceResult> UndoDeleteRangeAsync(Guid[] userIds)
		{
			try
			{
				// Get all matching user 
				// Build spec
				var baseSpec = new BaseSpecification<User>(e => userIds.Contains(e.UserId));
				var userEntities = await _unitOfWork.Repository<User, Guid>()
					.GetAllWithSpecAsync(baseSpec);
				// Check if any data already soft delete
				var userList = userEntities.ToList();
				if (userList.Any(x => !x.IsDeleted))
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0004,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
				}

				// Progress undo deleted status to false
				userList.ForEach(x => x.IsDeleted = false);

				// Save changes to DB
				var rowsAffected = await _unitOfWork.SaveChangesAsync();
				if (rowsAffected == 0)
				{
					// Get error msg
					return new ServiceResult(ResultCodeConst.SYS_Fail0004,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
				}

				// Mark as update success
				return new ServiceResult(ResultCodeConst.SYS_Success0009,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0009));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process undo delete range");
			}
		}

		public async Task<IServiceResult> ExportAsync(ISpecification<User> spec)
		{
			try
			{
				// Try to parse specification to UserSpecification
				var userSpec = spec as UserSpecification;
				// Check if specification is null
				if (userSpec == null)
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0002,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
				}

				// Define a local Mapster configuration
				var localConfig = new TypeAdapterConfig();
				localConfig.NewConfig<User, UserDto>()
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

				// Get all with spec
				var entities = await _unitOfWork.Repository<User, Guid>()
					.GetAllWithSpecAsync(userSpec, tracked: false);

				if (entities.Any()) // Exist data
				{
					// Map entities to dtos 
					var userDtos = _mapper.Map<List<UserDto>>(entities);
					// Process export data to file
					var fileBytes = CsvUtils.ExportToExcel(
						userDtos.ToUserExcelRecords());

					return new ServiceResult(ResultCodeConst.SYS_Success0002,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
						fileBytes);
				}

				return new ServiceResult(ResultCodeConst.SYS_Warning0004,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process export employee to excel");
			}
		}

		public async Task<IServiceResult> CreateManyAccountsWithSendEmail(string email, IFormFile? excelFile,
			DuplicateHandle duplicateHandle, bool isSendEmail)
		{
			// Initialize fields
			var langEnum =
				(SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(LanguageContext
					.CurrentLanguage);
			var isEng = langEnum == SystemLanguage.English;
			var totalImportData = 0;

			async Task ProcessAccountsAsync()
			{
				using var scope = _serviceProvider.CreateScope();
				var roleService = scope.ServiceProvider.GetRequiredService<ISystemRoleService<SystemRoleDto>>();
				var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
				var logger = scope.ServiceProvider.GetRequiredService<ILogger>();
				var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
				var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
				var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<AccountHub>>();

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
					throw new NotSupportedException(
						await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0001));
				}

				using var memoryStream = new MemoryStream();
				await excelFile.CopyToAsync(memoryStream);
				memoryStream.Position = 0;

				try
				{
					var result = await roleService.GetByNameAsync(RoleEnum.GeneralMember);
					if (result.ResultCode != ResultCodeConst.SYS_Success0002)
					{
						logger.Error("Not found any role with nameof General user");
						throw new NotFoundException("Role", "General user");
					}

					var failedMsgs = new List<UserFailedMessage>();

					using var package = new OfficeOpenXml.ExcelPackage(memoryStream);
					var worksheet = package.Workbook.Worksheets.FirstOrDefault();
					if (worksheet == null)
					{
						throw new BadRequestException(isEng
							? "Excel file does not contain any worksheet"
							: "Không tìm thấy worksheet");
					}

					int rowCount = worksheet.Dimension.Rows;
					var processedEmails = new Dictionary<string, int>();
					var emailPasswords = new Dictionary<string, string>();
					var userToAdd = new List<UserDto>();

					for (int row = 2; row <= rowCount; row++)
					{
						var userRecord = new UserExcelRecord()
						{
							Email = worksheet.Cells[row, 1].Text,
							FirstName = worksheet.Cells[row, 2].Text,
							LastName = worksheet.Cells[row, 3].Text,
							Dob = worksheet.Cells[row, 4].Text,
							Gender = worksheet.Cells[row, 5].Text,
							Phone = worksheet.Cells[row, 6].Text,
							Address = worksheet.Cells[row, 7].Text,
						};

						if (processedEmails.ContainsKey(userRecord.Email))
						{
							if (duplicateHandle.ToString().ToLower() == "skip")
							{
								continue;
							}
							else if (duplicateHandle.ToString().ToLower() == "replace")
							{
								failedMsgs.RemoveAll(f => f.Row == processedEmails[userRecord.Email]);
								processedEmails[userRecord.Email] = row;
							}
						}
						else
						{
							processedEmails[userRecord.Email] = row;
						}

						var rowErr = await DetectWrongRecord(userRecord, unitOfWork, langEnum);
						if (rowErr.Count != 0)
						{
							failedMsgs.Add(new UserFailedMessage()
							{
								Row = row,
								ErrMsg = rowErr
							});
						}
						else
						{
							// Convert to UserDto
							var newUser = userRecord.ToUserDto();
							// Retrieve default role
							var role = (SystemRoleDto)result.Data!;
							// Assign role
							newUser.RoleId = role.RoleId;

							if (isSendEmail) // Not process add random password for user when mark as send email
							{
								var password = HashUtils.GenerateRandomPassword();
								newUser.PasswordHash = HashUtils.HashPassword(password);

								// Add key-pair (email, password) 
								emailPasswords.Add(newUser.Email, password);
							}

							// Add user
							userToAdd.Add(newUser);
						}
					}

					if (failedMsgs.Count > 0 && isSendEmail)
					{
						await hubContext.Clients.User(email).SendAsync("ReceiveFailErrorMessage", failedMsgs);
					}

					// Update total import data
					totalImportData = userToAdd.Count;

					// Process add range
					await unitOfWork.Repository<User, Guid>().AddRangeAsync(mapper.Map<List<User>>(userToAdd));
					await unitOfWork.SaveChangesAsync();


					if (isSendEmail) // Only process send message to hub when mark as send email 
					{
						foreach (var userDto in userToAdd)
						{
							var emailMessageDto = new EmailMessageDto(
								new List<string> { userDto.Email },
								"ELibrary - Change password notification",
								$@"
			                    <h3>Hi {userDto.FirstName} {userDto.LastName},</h3>
			                    <p>Your account has been created. Your password is:</p>
			                    <h1>{emailPasswords[userDto.Email]}</h1>");
							await emailService.SendEmailAsync(emailMessageDto, true);
						}

						await hubContext.Clients.User(email).SendAsync("ReceiveCompleteNotification",
							"All emails sent successfully.");
					}
				}
				catch (Exception ex)
				{
					logger.Error(ex, "An error occurred while processing accounts");
				}
			}

			if (isSendEmail)
			{
				_ = Task.Run(ProcessAccountsAsync);

				return new ServiceResult(ResultCodeConst.SYS_Success0001,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001));
			}
			else
			{
				await ProcessAccountsAsync();

				var respMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0005);
				return new ServiceResult(ResultCodeConst.SYS_Success0005,
					StringUtils.Format(respMsg, totalImportData.ToString()), true);
			}
		}

		#region Library card holders
		public async Task<IServiceResult> CreateLibraryCardHolderAsync(
			string createdByEmail,
			UserDto dto,
			TransactionMethod transactionMethod,
			int? paymentMethodId,
			int libraryCardPackageId)
		{
			try
			{
				// Determine current system language
				var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
					LanguageContext.CurrentLanguage);
				var isEng = lang == SystemLanguage.English;
				
				// Check exist process by information
                var isCreateByEmailExist = (await _employeeService.AnyAsync(e => Equals(e.Email, createdByEmail))).Data is true;
                if (!isCreateByEmailExist) // not found
                {
                    throw new ForbiddenException("Not allow to access"); 
                }
				
				// Validate user
				var validationResult = await ValidatorExtensions.ValidateAsync(dto);
				// Check for valid validations
				if (validationResult != null && !validationResult.IsValid)
				{
					// Convert ValidationResult to ValidationProblemsDetails.Errors
					var errors = validationResult.ToProblemDetails().Errors;
					throw new UnprocessableEntityException("Invalid Validations", errors);
				}

				// Check exist library card go along with
				if (dto.LibraryCard == null)
				{
					// Not found {0}
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errMsg, isEng 
							? "library card go along with user information to process create patron" 
							: "thông tin thẻ thư viện đi kèm với thông tin bạn đọc để tạo mới"));
				}
				
				// Current local datetime
	            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
		            // Vietnam timezone
		            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
	            
				// Custom errors
				var customErrs = new Dictionary<string, string[]>();
				// Check email exist in user
				var isEmailExistInUser = await _unitOfWork.Repository<User, Guid>()
					.AnyAsync(u => Equals(u.Email, dto.Email));
				if (isEmailExistInUser)
				{
					// Add error
					customErrs = DictionaryUtils.AddOrUpdate(customErrs,
						key: StringUtils.ToCamelCase(nameof(User.Email)),
						msg: isEng ? "Email has already existed" : "Email đã tồn tại");
				}
				// Check email exist in employee
				var isEmailExistInEmployee = (await _employeeService.AnyAsync(e => 
					Equals(e.Email, dto.Email))).Data is true;
				if (isEmailExistInEmployee)
                {
                	// Add error
                	customErrs = DictionaryUtils.AddOrUpdate(customErrs,
                		key: StringUtils.ToCamelCase(nameof(User.Email)),
                		msg: isEng ? "Email has already existed" : "Email đã tồn tại");
                }
				
				// Check phone exist
				var isPhoneExist = await _unitOfWork.Repository<User, Guid>()
					.AnyAsync(u => !string.IsNullOrEmpty(dto.Phone) && Equals(u.Phone, dto.Phone));
				if (isPhoneExist)
				{
					// Add error
					customErrs = DictionaryUtils.AddOrUpdate(customErrs,
						key: StringUtils.ToCamelCase(nameof(User.Phone)),
						msg: isEng ? "Phone has already existed" : "SĐT đã tồn tại");
				}
				// Check exist avatar in cloud
				if(!string.IsNullOrEmpty(dto.Avatar))
				{
					// Extract public id 
					var updatePublicId = StringUtils.GetPublicIdFromUrl(dto.Avatar);
					if (updatePublicId != null)
					{
						var isImageOnCloud =
							(await _cloudService.IsExistAsync(updatePublicId, FileType.Image)).Data is true;
						if (!isImageOnCloud)
						{
							// Mark as fail to create
							return new ServiceResult(ResultCodeConst.SYS_Fail0001,
								await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
						}
					}
					else
					{
						// Mark as fail to create
						return new ServiceResult(ResultCodeConst.SYS_Fail0001,
							await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
					}
				}
				
				// Check whether invoke any validation error
				if (customErrs.Any()) throw new UnprocessableEntityException("Invalid data", customErrs);
				
				// Try to retrieve library card package 
				var libCardPackageDto = (await _cardPackageService.Value.GetByIdAsync(id: libraryCardPackageId)
					).Data as LibraryCardPackageDto;
				if (libCardPackageDto == null)
				{
					// Msg: Payment object does not exist. Please try again
					return new ServiceResult(ResultCodeConst.Transaction_Fail0002,
						await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0002));
				}
				
				// Initialize expired offset unix seconds
				var expiredAtOffsetUnixSeconds = 0;
				// Initialize random password field (use when method as Cash)
				var rndPass = string.Empty;
				// Initialize payOS response
                PayOSPaymentResponseDto? payOsResp = null; 
                // Initialize transaction 
                TransactionDto? transactionDto = null;
                // Generate transaction code
                var transactionCode = PaymentUtils.GenerateRandomOrderCodeDigits(_paymentSettings.TransactionCodeLength);
                // Determine transaction method
                switch (transactionMethod)
                {
                	// Cash
                	case TransactionMethod.Cash:
                		// Create transaction with PAID status
                		transactionDto = new TransactionDto
                		{
                			TransactionCode = transactionCode.ToString(),
                			Amount = libCardPackageDto.Price,
                			TransactionMethod = TransactionMethod.Cash,
                			TransactionStatus = TransactionStatus.Paid,
                			TransactionType = TransactionType.LibraryCardRegister,
                			CreatedAt = currentLocalDateTime,
                			TransactionDate = currentLocalDateTime,
                			LibraryCardPackageId = libCardPackageDto.LibraryCardPackageId,
                			CreatedBy = createdByEmail
                		};
                		
                		// Update card status
                        dto.LibraryCard.Status = LibraryCardStatus.Active;
                        // Set expiry date
		                dto.LibraryCard.ExpiryDate = currentLocalDateTime.AddMonths(
                            // Months defined in specific library card package 
                            libCardPackageDto.DurationInMonths);
		                
		                // Generate random password
                        rndPass = HashUtils.GenerateRandomPassword();
                        // Hash password
                        dto.PasswordHash = HashUtils.HashPassword(rndPass);
                		break;
                	// Digital payment
                	case TransactionMethod.DigitalPayment:
                		// Check exist payment method id 
                        var isExistPaymentMethod = (await _paymentMethodService.AnyAsync(p => 
                            Equals(p.PaymentMethodId, paymentMethodId))).Data is true;
                		if (!isExistPaymentMethod)
                		{
                			// Not found {0}
                			var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                			return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                				StringUtils.Format(errMsg, isEng ? "payment method" : "phương thức thanh toán"));
                		}
                		
                        // Check whether existing any transaction has pending status
                        var isExistPendingStatus = await _unitOfWork.Repository<Transaction, int>()
                            .AnyAsync(t => t.TransactionType == TransactionType.LibraryCardRegister &&
                                           t.TransactionStatus == TransactionStatus.Pending);
                        if (isExistPendingStatus)
                        {
                            // Msg: Failed to create payment transaction as existing transaction with pending status
                            return new ServiceResult(ResultCodeConst.Transaction_Warning0003,
                                await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Warning0003));
                        }
                        
                		// Create transaction with PENDING status (digital payment)
                        transactionDto = new TransactionDto
                        {
                            TransactionCode = transactionCode.ToString(),
                            Amount = libCardPackageDto.Price,
                            TransactionMethod = TransactionMethod.DigitalPayment,
                            TransactionStatus = TransactionStatus.Pending,
                            TransactionType = TransactionType.LibraryCardRegister,
                            CreatedAt = currentLocalDateTime,
                            ExpiredAt = currentLocalDateTime.AddMinutes(_paymentSettings.TransactionExpiredInMinutes),
                            LibraryCardPackageId = libCardPackageDto.LibraryCardPackageId,
                            PaymentMethodId = paymentMethodId,
                            CreatedBy = createdByEmail
                        };
                        
		                // Assign expired at 
		                expiredAtOffsetUnixSeconds = (int)((DateTimeOffset)transactionDto.ExpiredAt).ToUnixTimeSeconds();
                        // Generate payment link
                        var payOsPaymentRequest = new PayOSPaymentRequestDto()
                        {
                            OrderCode = transactionCode,
                            Amount = (int) transactionDto.Amount,
                            Description = isEng ? "Library card register"  : "Dang ky the thu vien",
                            BuyerName = $"{dto.FirstName} {dto.LastName}".ToUpper(),
                            BuyerEmail = dto.Email,
                            BuyerPhone = dto.Phone ?? string.Empty,
                            BuyerAddress = dto.Address ?? string.Empty,
                            Items = [
                                new
                                {
                                    Name = isEng ? transactionDto.TransactionType.ToString() : transactionDto.TransactionType.GetDescription(),
                                    Quantity = 1,
                                    Price = transactionDto.Amount
                                }
                            ],
                            CancelUrl = _payOsSettings.CancelUrl,
                            ReturnUrl = _payOsSettings.ReturnUrl,
                            ExpiredAt = (int)((DateTimeOffset) transactionDto.ExpiredAt).ToUnixTimeSeconds()
                        };
                        
                        // Generate signature
                        await payOsPaymentRequest.GenerateSignatureAsync(transactionCode, _payOsSettings);
                        var payOsPaymentResp = await payOsPaymentRequest.GetUrlAsync(_payOsSettings);
                        
                        // Create Payment status
                        bool isCreatePaymentSuccess = payOsPaymentResp.Item1; // Is created success
                        if (isCreatePaymentSuccess && payOsPaymentResp.Item3 != null)
                        {
                            // Assign payOs response
                            payOsResp = payOsPaymentResp.Item3;
                            
                            // Set library card default status
                            dto.LibraryCard.Status = LibraryCardStatus.UnPaid;
                            // Assign transaction code
                            dto.LibraryCard.TransactionCode = transactionCode.ToString();
                            // Assign payment URL
                            transactionDto.QrCode = payOsResp.Data.QrCode;
                        }
                        else
                        {
                            // Msg: Failed to create payment transaction. Please try again
                            return new ServiceResult(ResultCodeConst.Transaction_Fail0001,
                                await _msgService.GetMessageAsync(ResultCodeConst.Transaction_Fail0001));
                        }
                        break;
                }
				
				// Assign default role 
				var roleDto = (await _roleService.GetByNameAsync(Role.GeneralMember)).Data as SystemRoleDto;
				if (roleDto == null)
				{
					_logger.Error("Fail to create library card due to user role not found");
					// Unknown error
					return new ServiceResult(ResultCodeConst.SYS_Warning0006,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0006));
				}

				// Add user necessary properties
				dto.CreateDate = currentLocalDateTime;
				dto.RoleId = roleDto.RoleId;
				dto.IsEmployeeCreated = true;

				// Add library card necessary props
				dto.LibraryCard.Barcode = LibraryCardUtils.GenerateBarcode(_appSettings.LibraryCardBarcodePrefix);
				dto.LibraryCard.IssuanceMethod = LibraryCardIssuanceMethod.InPerson;
				dto.LibraryCard.IssueDate = currentLocalDateTime;
				dto.LibraryCard.IsReminderSent = false;
				dto.LibraryCard.IsExtended = false;
				dto.LibraryCard.ExtensionCount = 0;

				// Extend borrow default
				dto.LibraryCard.IsAllowBorrowMore = false;
				dto.LibraryCard.MaxItemOnceTime = 0;

				// Total missed default 
				dto.LibraryCard.TotalMissedPickUp = 0;
				
				// Add default account security
				dto.EmailConfirmed = true;
				dto.IsActive = true;
				dto.IsDeleted = false;
				
				// Add transaction to user
				if (transactionDto != null) dto.Transactions.Add(transactionDto);
				else
				{
					// Fail to create
                    return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                    	await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001), false);
				}
				
				// Progress add new user with library card
				await _unitOfWork.Repository<User, Guid>().AddAsync(_mapper.Map<User>(dto));
				// Save change
				var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
				if (isSaved)
				{
					// Assign package to transaction
					transactionDto.LibraryCardPackage = libCardPackageDto;
					
					// Determine transaction method
					switch (transactionMethod)
					{
						// CASH
						case TransactionMethod.Cash:
							// Send card has been activated email
							var isSent = await SendActivatedEmailAsync(
								email: dto.Email,
								password: rndPass,
								cardDto: dto.LibraryCard,
								transactionDto: transactionDto,
								libName: _appSettings.LibraryName,
								libContact: _appSettings.LibraryContact,
								isEmployeeCreated: true);
							if (isSent)
							{
								var successMsg = isEng ? "Announcement email has sent to patron" : "Email thông báo đã gửi đến bạn đọc"; 
								// Msg: Register library card success
								return new ServiceResult(ResultCodeConst.SYS_Success0001,
									await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001) + $". {successMsg}");
							}
                        
							var failMsg = isEng ? ", but fail to send email" : ", nhưng gửi email thông báo thất bại";
							// Msg: Register library card success
							return new ServiceResult(ResultCodeConst.SYS_Success0001,
								await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001) + failMsg);
                    
						// DIGITAL PAYMENT
						case TransactionMethod.DigitalPayment:
							// Msg: Create payment link successfully
							return new ServiceResult(ResultCodeConst.SYS_Success0001,
								await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001), 
								new PayOSPaymentLinkResponseDto()
								{
									PayOsResponse = payOsResp!,
									ExpiredAtOffsetUnixSeconds = expiredAtOffsetUnixSeconds
								});
					}
					
					// Msg: Create successfully
					return new ServiceResult(ResultCodeConst.SYS_Success0001,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001), true);
				}

				// Fail to create
				return new ServiceResult(ResultCodeConst.SYS_Fail0001,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001), false);
			}
			catch(ForbiddenException)
			{
				throw;
			}
			catch (UnprocessableEntityException)
			{
				throw;
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process create library card holder");
			}
		}
		
		public async Task<IServiceResult> GetAllLibraryCardHolderAsync(ISpecification<User> spec)
		{
			try
			{
				// Try to parse specification to LibraryCardHolderSpecification
				var cardHolderSpecification = spec as LibraryCardHolderSpecification;
				// Check if specification is null
				if (cardHolderSpecification == null)
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0002,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
				}
				
				// Count total library items
				var totalLibItemWithSpec = await _unitOfWork.Repository<User, Guid>().CountAsync(cardHolderSpecification);
				// Count total page
				var totalPage = (int)Math.Ceiling((double)totalLibItemWithSpec / cardHolderSpecification.PageSize);

				// Set pagination to specification after count total library item
				if (cardHolderSpecification.PageIndex > totalPage
				    || cardHolderSpecification.PageIndex < 1) // Exceed total page or page index smaller than 1
				{
					cardHolderSpecification.PageIndex = 1; // Set default to first page
				}

				// Apply pagination
				cardHolderSpecification.ApplyPaging(
					skip: cardHolderSpecification.PageSize * (cardHolderSpecification.PageIndex - 1),
					take: cardHolderSpecification.PageSize);
				
				// Retrieve all with spec
				var entities = await _unitOfWork.Repository<User, Guid>().GetAllWithSpecAsync(cardHolderSpecification);
				if (entities.Any())
				{
					// Convert to dto collection
					var userDtos = _mapper.Map<List<UserDto>>(entities);

					// Pagination result 
					var paginationResultDto = new PaginatedResultDto<LibraryCardHolderDto>(userDtos.Select(u => u.ToLibraryCardHolderDto()),
						cardHolderSpecification.PageIndex, cardHolderSpecification.PageSize, totalPage, totalLibItemWithSpec);

					// Response with pagination 
					return new ServiceResult(ResultCodeConst.SYS_Success0002,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
				}

				// Data not found or empty
				return new ServiceResult(ResultCodeConst.SYS_Warning0004,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
					_mapper.Map<List<UserDto>>(entities).Select(x => x.ToLibraryCardHolderDto()));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process get all library card holders");
			}
		}

		public async Task<IServiceResult> GetLibraryCardHolderByIdAsync(Guid userId)
		{
			try
			{
				// Determine current system lang
				var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
					LanguageContext.CurrentLanguage);
				var isEng = lang == SystemLanguage.English;

				// Check exist user 
				// Build spec
				var baseSpec = new BaseSpecification<User>(u => Equals(userId, u.UserId));
				// Apply include
				baseSpec.ApplyInclude(q => q
					.Include(u => u.LibraryCard!)
				);
				// Retrieve user with spec
				var existingEntity = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(baseSpec);
				if (existingEntity == null)
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errMsg, isEng ? "patron" : "bạn đọc"));
				}

				// Get data successfully
				return new ServiceResult(ResultCodeConst.SYS_Success0002,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
					_mapper.Map<UserDto>(existingEntity).ToLibraryCardHolderDto());
			}
			catch (ForbiddenException)
			{
				throw;
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process get library card holder by id");
			}
		}

		public async Task<IServiceResult> GetLibraryCardHolderByBarcodeAsync(string barcode)
        {
            try
            {
                // Determine current system lang
                var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                    LanguageContext.CurrentLanguage);
                var isEng = lang == SystemLanguage.English;
                
                // Build spec
                var baseSpec = new BaseSpecification<User>(u => 
	                u.LibraryCard != null &&
	                Equals(u.LibraryCard.Barcode, barcode));
                // Apply include
                baseSpec.ApplyInclude(q => q.Include(u => u.LibraryCard!));
                // Retrieve with spec
                var existingEntity = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(baseSpec);
                if (existingEntity == null || existingEntity.LibraryCardId == Guid.Empty)
                {
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                        StringUtils.Format(errMsg, isEng ? "library card" : "thẻ thư viện"));
                }
                
                // Validate library card 
                var validateCardRes = await _libraryCardService.Value.CheckCardValidityAsync(
	                Guid.Parse(existingEntity.LibraryCardId.ToString() ?? string.Empty));
                // Return invalid card
                if (validateCardRes.ResultCode != ResultCodeConst.LibraryCard_Success0001) return validateCardRes;
    
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                    _mapper.Map<UserDto>(existingEntity).ToLibraryCardHolderDto()    
                );
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw new Exception("Error invoke when process get library card by barcode");
            }
        }

		public async Task<IServiceResult> UpdateLibraryCardHolderAsync(Guid userId, UserDto userDto)
		{
			try
			{
				// Validate inputs using the generic validator
				var validationResult = await ValidatorExtensions.ValidateAsync(userDto);
				// Check for valid validations
				if (validationResult != null && !validationResult.IsValid)
				{
					// Convert ValidationResult to ValidationProblemsDetails.Errors
					var errors = validationResult.ToProblemDetails().Errors;
					throw new UnprocessableEntityException("Invalid validations", errors);
				}
				
				// Determine current system lang
				var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
					LanguageContext.CurrentLanguage);
				var isEng = lang == SystemLanguage.English;

				// Try to retrieve user by id 
				var existingEntity = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(userId);
				if (existingEntity == null)
				{
					// Not found {0}
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errMsg, isEng ? "patron" : "bạn đọc"));
				}

				// Update properties
				existingEntity.FirstName = userDto.FirstName ?? string.Empty;
				existingEntity.LastName = userDto.LastName ?? string.Empty;
				existingEntity.Address = userDto.Address;
				existingEntity.Gender = Enum.TryParse(typeof(Gender), userDto.Gender, out _) ? userDto.Gender : null;
				existingEntity.Phone = userDto.Phone;
				existingEntity.Dob = userDto.Dob;

				// Process update entity 
				await _unitOfWork.Repository<User, Guid>().UpdateAsync(existingEntity);
				// Save DB
				var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
				if (isSaved)
				{
					// Update successfully
					return new ServiceResult(ResultCodeConst.SYS_Success0003,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
				}

				// Fail to update
				return new ServiceResult(ResultCodeConst.SYS_Fail0003,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), true);
			}
			catch (UnprocessableEntityException)
			{
				throw;
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process update library card holder");
			}
		}
		
		public async Task<IServiceResult> SoftDeleteLibraryCardHolderAsync(Guid userId)
		{
			try
			{
				// Determine current lang context
				var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
					LanguageContext.CurrentLanguage);
				var isEng = lang == SystemLanguage.English;
				
				// Build spec
                var baseSpec = new BaseSpecification<User>(u => u.UserId == userId);
                // Apply include
                baseSpec.ApplyInclude(q => q.Include(u => u.LibraryCard!));
                // Retrieve with spec
                var existingEntity = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(baseSpec);
                if (existingEntity == null || existingEntity.IsDeleted)
                {
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                        StringUtils.Format(errMsg, isEng ? "patron" : "bạn đọc"));
                }
                
                // Check constraints
                var hasConstraints = await _unitOfWork.Repository<User, Guid>()
	                .AnyAsync(new BaseSpecification<User>(u =>
		                u.UserId == userId && // with specific user
		                (
			                u.IsEmployeeCreated == true && // Only process soft delete when created by employee
			                ((u.LibraryCardId != Guid.Empty && u.LibraryCard != null &&
			                 u.LibraryCard.Status != LibraryCardStatus.Pending && 
			                 u.LibraryCard.Status != LibraryCardStatus.Rejected) || // Already registered library card
			                u.Transactions.Any() || // exist any transactions
			                u.DigitalBorrows.Any() || // exist any transactions
			                u.RefreshTokens.Any() || // has already signed in
			                u.LibraryItemReviews.Any() || // exist any reviews
			                u.UserFavorites.Any())))); // exist any user favourites
                if (hasConstraints)
                {
	                // Msg: Cannot delete because it is bound to other data
	                return new ServiceResult(ResultCodeConst.SYS_Fail0007,
		                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0007), false);
                }
                
				// Update soft delete prop
				existingEntity.IsDeleted = true;
				
				// Process update
				await _unitOfWork.Repository<User, Guid>().UpdateAsync(existingEntity);
				// Save DB
				var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
				if (isSaved)
				{
					// Deleted data to trash
					return new ServiceResult(ResultCodeConst.SYS_Success0007,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0007), true);
				}
				
				// Fail to delete data
				return new ServiceResult(ResultCodeConst.SYS_Fail0004,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process soft delete library card holder");
			}
		}

		public async Task<IServiceResult> SoftDeleteRangeLibraryCardHolderAsync(Guid[] userIds)
		{
			try
			{
				// Build spec
				var baseSpec = new BaseSpecification<User>(u => userIds.Contains(u.UserId));
				// Apply include
				baseSpec.ApplyInclude(q => q.Include(u => u.LibraryCard!));
				// Retrieve with spec
				var entities = await _unitOfWork.Repository<User, Guid>().GetAllWithSpecAsync(baseSpec);
				// Convert to list 
				var cardHolderList = entities.ToList();
				if (cardHolderList.Any(u => u.IsDeleted))
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0004,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);
				}

				// Initialize err dic
				var customErrs = new Dictionary<string, string[]>();
				for (int i = 0; i < cardHolderList.Count; ++i)
				{
					var user = cardHolderList[i];
					
					// Check constraints
					var hasConstraints = await _unitOfWork.Repository<User, Guid>()
						.AnyAsync(new BaseSpecification<User>(u =>
							u.UserId == user.UserId && // with specific user
							(
								u.IsEmployeeCreated == true && // Only process soft delete when created by employee
								((u.LibraryCardId != Guid.Empty && u.LibraryCard != null &&
								 u.LibraryCard.Status != LibraryCardStatus.Rejected &&
								 u.LibraryCard.Status != LibraryCardStatus.Pending) || // Already registered library card
								u.Transactions.Any() || // exist any transactions
								u.DigitalBorrows.Any() || // exist any transactions
								u.RefreshTokens.Any() || // has already signed in
								u.LibraryItemReviews.Any() || // exist any reviews
								u.UserFavorites.Any())))); // exist any user favourites
					if (hasConstraints)
					{
						// Msg: Cannot delete because it is bound to other data
                        return new ServiceResult(ResultCodeConst.SYS_Fail0007,
                            await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0007), false);
						// Add error 
						// customErrs = DictionaryUtils.AddOrUpdate(customErrs,
						// 	key: $"ids[{i}]",
						// 	msg: await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0007));
					}
					else
					{
						// Update soft delete status
                        user.IsDeleted = true;
                        // Process soft delete
                        await _unitOfWork.Repository<User, Guid>().UpdateAsync(user);
					}
				}
				
				if(customErrs.Any()) throw new UnprocessableEntityException("Remove constraints invoked", customErrs);
				
				// Save to DB
				if (await _unitOfWork.SaveChangesAsync() > 0)
				{
					var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0008);
					return new ServiceResult(ResultCodeConst.SYS_Success0008,
						StringUtils.Format(msg, cardHolderList.Count.ToString()), true);
				}
    
				// Fail to delete
				return new ServiceResult(ResultCodeConst.SYS_Fail0004,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);
			}
			catch (UnprocessableEntityException)
			{
				throw;
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process soft delete range library card holder");
			}
		}

		public async Task<IServiceResult> UndoDeleteLibraryCardHolderAsync(Guid userId)
		{
			try
			{
				// Determine current lang context
				var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
					LanguageContext.CurrentLanguage);
				var isEng = lang == SystemLanguage.English;
				
				// Build spec
				var baseSpec = new BaseSpecification<User>(u => u.UserId == userId);
				// Apply include
				baseSpec.ApplyInclude(q => q.Include(u => u.LibraryCard!));
				// Retrieve with spec
				var existingEntity = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(baseSpec);
				if (existingEntity == null || !existingEntity.IsDeleted)
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errMsg, isEng ? "patron" : "bạn đọc"));
				}
				
				// Update soft delete status
				existingEntity.IsDeleted = false;
				
				// Process update
				await _unitOfWork.Repository<User, Guid>().UpdateAsync(existingEntity);
				// Save DB
				var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
				if (isSaved)
				{
					// Recovery data successfully
					return new ServiceResult(ResultCodeConst.SYS_Success0009,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0009), true);
				}
				
				// Fail to delete data
				return new ServiceResult(ResultCodeConst.SYS_Fail0004,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process undo delete library card holder");
			}
		}

		public async Task<IServiceResult> UndoDeleteRangeLibraryCardHolderAsync(Guid[] userIds)
		{
			try
			{
				// Build spec
				var baseSpec = new BaseSpecification<User>(u => userIds.Contains(u.UserId));
				// Apply include
				baseSpec.ApplyInclude(q => q.Include(u => u.LibraryCard!));
				// Retrieve with spec
				var entities = await _unitOfWork.Repository<User, Guid>().GetAllWithSpecAsync(baseSpec);
				// Convert to list 
				var cardHolderList = entities.ToList();
				if (cardHolderList.Any(u => !u.IsDeleted))
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0004,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);
				}
				
				// Update soft delete range
				foreach (var cardHolder in cardHolderList)
				{
					cardHolder.IsDeleted = false;
					
					// Process update entity
					await _unitOfWork.Repository<User, Guid>().UpdateAsync(cardHolder);
				}
				
				// Save to DB
				if (await _unitOfWork.SaveChangesAsync() > 0)
				{
					// Recovery data successfully
					return new ServiceResult(ResultCodeConst.SYS_Success0009,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0009), true);
				}
    
				// Fail to update
				return new ServiceResult(ResultCodeConst.SYS_Fail0003,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003), false);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process undo delete range library card holder");
			}
		}

		public async Task<IServiceResult> DeleteLibraryCardHolderAsync(Guid userId)
		{
			try
			{
				// Determine current lang context
				var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
					LanguageContext.CurrentLanguage);
				var isEng = lang == SystemLanguage.English;
				
				// Retrieve the entity
				var existingEntity = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(userId);
				if (existingEntity == null)
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errMsg, isEng ? "patron" : "bạn đọc"));
				}

				// Check whether cardholder in the trash bin
				if (!existingEntity.IsDeleted)
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0004,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);
				}
				
				// Try to remove library card (if any). Only allow when card is in pending status and user is created by employee
				if (existingEntity.LibraryCardId != Guid.Empty && existingEntity.IsEmployeeCreated)
				{
					// Process delete card
					var deleteConstraintRes = 
						await _libraryCardService.Value.DeleteCardWithoutSaveChangesAsync(
							Guid.Parse(existingEntity.LibraryCardId.ToString() ?? string.Empty));

					if (deleteConstraintRes.ResultCode != ResultCodeConst.SYS_Success0004)
					{
						// Msg: Cannot delete because it is bound to other data
						return new ServiceResult(ResultCodeConst.SYS_Fail0007,
							await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0007), false);
					}
				}
				
				// Process delete entity
				await _unitOfWork.Repository<User, Guid>().DeleteAsync(userId);
				// Save to DB
				if (await _unitOfWork.SaveChangesWithTransactionAsync() > 0)
				{
					// Delete successfully
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
				throw new Exception("Error invoke when process delete library card holder");
			}
		}

		public async Task<IServiceResult> DeleteRangeLibraryCardHolderAsync(Guid[] userIds)
		{
			try
			{
				// Get all matching user
				// Build spec
				var baseSpec = new BaseSpecification<User>(e => userIds.Contains(e.UserId));
				var cardHolderEntities = await _unitOfWork.Repository<User, Guid>()
					.GetAllWithSpecAsync(baseSpec);
				// Check if any data already soft delete
				var cardHolderList = cardHolderEntities.ToList();
				if (cardHolderList.Any(x => !x.IsDeleted))
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0004,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);
				}
			
				// Select range of library card id to delete
				var rangeCardIds = cardHolderList
					.Where(x => x.LibraryCardId != Guid.Empty)
					.Select(x => Guid.Parse(x.LibraryCardId.ToString() ?? string.Empty))
					.ToArray();
				// Process delete library cards
				var deleteRangeCardRes = await _libraryCardService.Value.DeleteRangeCardWithoutSaveChangesAsync(rangeCardIds);
				if (deleteRangeCardRes.ResultCode != ResultCodeConst.SYS_Success0004)
				{
					// Msg: Cannot delete because it is bound to other data
					return new ServiceResult(ResultCodeConst.SYS_Fail0007,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0007), false);
				}	
				
				// Process delete range entity
				await _unitOfWork.Repository<User, Guid>().DeleteRangeAsync(
					ids: cardHolderList.Select(c => c.UserId).ToArray());
				// Save to DB
				if (await _unitOfWork.SaveChangesWithTransactionAsync() > 0)
				{
					// Delete successfully
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
				throw new Exception("Error invoke when process delete range library card holder");
			}
		}
		
		public async Task<IServiceResult> DeleteLibraryCardWithoutSaveChangesAsync(Guid userId)
		{
			try
			{
				// Build spec
				var baseSpec = new BaseSpecification<User>(u => u.UserId == userId);
				// Retrieve with spec
				var existingEntity = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(baseSpec);
				if (existingEntity == null || existingEntity.LibraryCardId == null)
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0004,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);	
				}
				
				// Set null for library card
				existingEntity.LibraryCardId = null;
				
				// Process update
				await _unitOfWork.Repository<User, Guid>().UpdateAsync(existingEntity);
				
				// Mark as update success
				return new ServiceResult(ResultCodeConst.SYS_Success0003,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003), true);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when delete library card without save");
			}
		}

		public async Task<IServiceResult> ImportLibraryCardHolderAsync(IFormFile? file,
			List<IFormFile>? avatarImageFiles,
			string[]? scanningFields, 
			DuplicateHandle? duplicateHandle)
		{
			try
			{
				// Determine system lang
				var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(LanguageContext
					.CurrentLanguage);
				var isEng = lang == SystemLanguage.English;

				// Check exist file
				if (file == null || file.Length == 0)
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
				
				// Extract all cover image file name
				var imageFileNames = avatarImageFiles.Select(f => f.FileName).ToList();
				// Find duplicate image file names
				var duplicateFileNames = imageFileNames
					.GroupBy(name => name)
					.Where(group => group.Count() > 1) // Filter groups with more than one occurrence
					.Select(group => group.Key)       // Select the duplicate file names
					.ToList();
				if (duplicateFileNames.Any())
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.File_Warning0004);
			    
					// Add single quotes to each file name
					var formattedFileNames = duplicateFileNames
						.Select(fileName => $"'{fileName}'"); 

					return new ServiceResult(
						ResultCodeConst.File_Warning0004,
						StringUtils.Format(errMsg, String.Join(", ", formattedFileNames))
					);
				}
				
				// Process read csv file
				var readResp =
					CsvUtils.ReadCsvOrExcelByHeaderIndexWithErrors<LibraryCardHolderCsvRecordDto>(
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
				
				// Validate collection of records
				readResp.Errors = await ValidateLibraryCardHolderCsvRecordsAsync(
					records: readResp.Records,
					startRowIndex: 2,
					imageFileNames: imageFileNames,
					lang: lang);
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

				// Retrieve default role 
                var roleDto = (await _roleService.GetByNameAsync(Role.GeneralMember)).Data as SystemRoleDto;
                if (roleDto == null)
                {
                	// Not found {0}
                	var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                	return new ServiceResult(ResultCodeConst.SYS_Fail0002, 
                		StringUtils.Format(errMsg, isEng ? "default role to process import" : "role mặc định để thực hiện import"));
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
				
				// Handle upload images (Image name | URL)
				var uploadFailList = new List<string>();
				var imageUrlDic = new Dictionary<string, string>();
				foreach (var avatarImage in avatarImageFiles)
				{
					// Try to validate file
					var validateResult = await 
						new ImageTypeValidator(lang.ToString() ?? SystemLanguage.English.ToString()).ValidateAsync(avatarImage);
					if (!validateResult.IsValid)
					{
						return new ServiceResult(ResultCodeConst.SYS_Warning0001, isEng 
							? $"File '{avatarImage.FileName}' is not a image file " +
							  $"Valid format such as (.jpeg, .png, .gif, etc.)" 
							: $"File '{avatarImage.FileName}' không phải là file hình ảnh. " +
							  $"Các loại hình ảnh được phép là: (.jpeg, .png, .gif, v.v.)");
					}
			    
					// Upload image to cloudinary
					var uploadResult = (await _cloudService.UploadAsync(avatarImage, FileType.Image, ResourceType.BookImage))
						.Data as CloudinaryResultDto;
					if (uploadResult == null)
					{
						// Add image that fail to upload
						uploadFailList.Add(avatarImage.FileName);
					}
					else
					{
						// Add to dic
						imageUrlDic.Add(avatarImage.FileName, uploadResult.SecureUrl);
					}
				}
				
				var totalImported = 0;
				var totalFailed = 0;
				// Process import book editions
				var successRecords = readResp.Records
					.Where(r => r.LibraryCardAvatar != null &&
					            !uploadFailList.Contains(r.LibraryCardAvatar))
					.ToList();
				var failRecords = new List<LibraryCardHolderCsvRecordDto>();
				if (successRecords.Any())
				{
					// Initialize list library cardholders
					var cardHolderList = successRecords.Select(r =>
					{
						var imageUrl = string.Empty;
						if (r.LibraryCardAvatar != null)
						{
							imageUrl = imageUrlDic.TryGetValue(r.LibraryCardAvatar, out var avatarImageUrl)
								? avatarImageUrl
								: null;
						}
						return r.ToUserDto(roleId: roleDto.RoleId, borrowSettings: _borrowSettings, libraryCardAvatar: imageUrl);
					}).ToList();

					if (cardHolderList.Any())
					{
						// Add new cardholder
						await _unitOfWork.Repository<User, Guid>().AddRangeAsync(_mapper.Map<List<User>>(cardHolderList));
					
						// Save change to DB
						if(await _unitOfWork.SaveChangesAsync() > 0) totalImported = cardHolderList.Count;
						else failRecords.AddRange(successRecords);
					}
				}
				
				// Aggregate all book editions fail to upload & fail to save DB (if any)
				failRecords.AddRange(readResp.Records
					.Where(r => r.LibraryCardAvatar != null && 
					            uploadFailList.Contains(r.LibraryCardAvatar))
					.ToList());
				if (failRecords.Any()) totalFailed = failRecords.Count;
				
				string message;
			    byte[]? fileBytes;
				// Generate a message based on the import and failure counts
			    if (totalImported > 0 && totalFailed == 0)
			    {
				    // All records imported successfully
				    message = StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0005), totalImported.ToString());
	                // Additional message (if any)
	                message = !string.IsNullOrEmpty(additionalMsg) ? $"{message}, {additionalMsg}" : message;
	                // Generate excel file for imported data
	                fileBytes = CsvUtils.ExportToExcel(successRecords, sheetName: "ImportedReaders");
				    return new ServiceResult(ResultCodeConst.SYS_Success0005, message, Convert.ToBase64String(fileBytes));
			    }

			    if (totalImported > 0 && totalFailed > 0)
			    {
				    // Partial success with some failures
				    fileBytes = CsvUtils.ExportToExcel(failRecords, sheetName: "FailImageUploadReaders");

				    var baseMessage = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0005);
	                var failMessage = lang == SystemLanguage.English
					    ? $", {totalFailed} failed to import"
					    : $", {totalFailed} thêm mới thất bại";
	                
				    message = StringUtils.Format(baseMessage, totalImported.ToString());
	                // Additional message (if any)
	                message = !string.IsNullOrEmpty(additionalMsg) ? $"{message}, {additionalMsg} {failMessage}" : message + failMessage;
				    return new ServiceResult(ResultCodeConst.SYS_Success0005, message, Convert.ToBase64String(fileBytes));
			    }

			    if (totalImported == 0 && totalFailed > 0)
			    {
				    // Complete failure
				    fileBytes = CsvUtils.ExportToExcel(failRecords, sheetName: "FailImageUploadReaders");
				    message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008);
				    return new ServiceResult(ResultCodeConst.SYS_Fail0008, message, Convert.ToBase64String(fileBytes));
			    }

				// Default case: No records imported or failed
			    message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0008);
			    return new ServiceResult(ResultCodeConst.SYS_Fail0008, message);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process import library card holder");
			}
		}
		
		public async Task<IServiceResult> ExportLibraryCardHolderAsync(ISpecification<User> spec)
		{
			try
			{
				// Try to parse specification to LibraryCardHolderSpecification
				var baseSpec = spec as LibraryCardHolderSpecification;
				// Check if specification is null
				if (baseSpec == null)
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0002,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
				}				
			
				// Apply include
				baseSpec.ApplyInclude(q => q
					.Include(be => be.LibraryCard!)
				);
				// Get all with spec
				var entities = await _unitOfWork.Repository<User, Guid>()
					.GetAllWithSpecAsync(baseSpec, tracked: false);
				if (entities.Any()) // Exist data
				{
					// Map entities to dtos 
					var userDtos = _mapper.Map<List<UserDto>>(entities);
					// Process export data to file
					var fileBytes = CsvUtils.ExportToExcelWithNameAttribute(
						userDtos.Select(u => u.ToCardHolderCsvRecordDto()).ToList());

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
				throw new Exception("Error invoke when process export library card holders");
			}
		}
		
		private async Task<Dictionary<int, string[]>> ValidateLibraryCardHolderCsvRecordsAsync(
	        List<LibraryCardHolderCsvRecordDto> records, 
	        int startRowIndex,
	        List<string>? imageFileNames,
	        SystemLanguage? lang = SystemLanguage.English)
	    {
	        // Check current system lang
	        var isEng = lang == SystemLanguage.English;
	        
	        // Initialize error dic
	        var errDic = new Dictionary<int, string[]>();
	        // Assign current row index
	        var currRow = startRowIndex;
	        
	        // Iterate each record to validate data
	        for (int i = 0; i < records.Count; i++)
	        {
	            // Initialize list errors
	            var errors = new List<string>();

	            // Email
	            var email = records[i].Email;
	            if (string.IsNullOrEmpty(email) ||
	                !Regex.Match(email, @"^((?!\.)[\w\-_.]*[^.])(@\w+)(\.\w+(\.\w+)?[^.\W])$").Success)
	            {
	                errors.Add(isEng ? "Not valid email address" : "Email không hợp lệ");
	            }
	            else
	            {
	                // Check whether email already exist
	                var isEmailExist = await _unitOfWork.Repository<User, Guid>().AnyAsync(u => u.Email == email);
	                if (isEmailExist)
	                {
	                    errors.Add(isEng ? "Email has already existed" : "Email đã tồn tại");
	                }
	            }
	            
	            // Firstname
	            var firstName = records[i].FirstName;
	            if (string.IsNullOrEmpty(firstName))
	            {
	                errors.Add(isEng ? "First name cannot be empty" : "Vui lòng nhập đầy đủ họ và tên");
	            }else if (!Regex.Match(firstName, @"^([A-ZÀ-Ỵ][a-zà-ỵ]*)(\s[A-ZÀ-Ỵ][a-zà-ỵ]*)*$").Success)
	            {
	                errors.Add(isEng 
	                    ? "First name should start with an uppercase letter for each word" 
	                    : "Họ phải bắt đầu bằng chữ cái viết hoa cho mỗi từ");
	            }else if (firstName.Length > 100 || firstName.Length < 1)
	            {
	                errors.Add(isEng 
	                    ? "First name must be between 1 and 100 characters long" 
	                    : "Họ phải có độ dài từ 1 đến 100 ký tự");
	            }
	            
	            // Lastname
	            var lastName = records[i].LastName;
	            if (string.IsNullOrEmpty(lastName))
	            {
	                errors.Add(isEng ? "Last name cannot be empty" : "Vui lòng nhập đầy đủ họ và tên");
	            }else if (!Regex.Match(lastName, @"^([A-ZÀ-Ỵ][a-zà-ỵ]*)(\s[A-ZÀ-Ỵ][a-zà-ỵ]*)*$").Success)
	            {
	                errors.Add(isEng 
	                    ? "Last name should start with an uppercase letter for each word" 
	                    : "Tên phải bắt đầu bằng chữ cái viết hoa cho mỗi từ");
	            }else if (lastName.Length > 100 || lastName.Length < 1)
	            {
	                errors.Add(isEng 
	                    ? "Last name must be between 1 and 100 characters long" 
	                    : "Tên phải có độ dài từ 1 đến 100 ký tự");
	            }
	            
	            // Dob 
	            var dob = records[i].Dob;
	            if (dob != null) // Only process validate dob when exist
	            {
	                if (dob != DateTime.MinValue && 
	                    dob.Value.Date >= DateTime.UtcNow.Date)
	                {
	                    errors.Add(isEng 
	                        ? "Invalid date of birth"
	                        : "Ngày sinh không hợp lệ");
	                }
	            }
	            
	            // Phone
	            var phoneNumber = records[i].Phone;
	            if (phoneNumber != null)
	            {
	                if (phoneNumber.Length < 10)
	                {
	                    errors.Add(isEng 
	                        ? "Phone must not be less than 10 characters" 
	                        : "SĐT không được ít hơn 10 ký tự");
	                }else if (phoneNumber.Length > 12)
	                {
	                    errors.Add(isEng 
	                        ? "Phone must not exceed 12 characters" 
	                        : "SĐT không được vượt quá 12 ký tự");
	                }else if (!Regex.Match(phoneNumber, @"^0\d{9,10}$").Success)
	                {
	                    errors.Add(isEng 
	                        ? "Phone not valid" 
	                        : "SĐT không hợp lệ");
	                }
	                else
	                {
	                    // Check whether phone already exist
	                    var isPhoneExist = await _unitOfWork.Repository<User, Guid>().AnyAsync(u => u.Phone == phoneNumber);
	                    if (isPhoneExist)
	                    {
	                        errors.Add(isEng ? "Phone number has already existed" : "SĐT đã tồn tại");
	                    }
	                }
	            }
	            
	            // Address 
	            var address = records[i].Address;
	            if (!string.IsNullOrEmpty(address) && address.Length > 255)
	            {
	                errors.Add(isEng ? "Address cannot exceed 255 characters" : "Địa chỉ không vượt quá 255 ký tự");
	            }
	            
	            // Gender
	            var gender = records[i].Gender;
	            if (!string.IsNullOrEmpty(gender) && !Enum.TryParse(gender, true, out Gender genderEnum))
	            {
	                if(gender.ToLower() == Gender.Male.GetDescription().ToLower())
	                {
	                    records[i].Gender = Gender.Male.ToString();
	                }else if (gender.ToLower() == Gender.Female.GetDescription().ToLower())
	                {
	                    records[i].Gender = Gender.Female.ToString();
	                }else if (gender.ToLower() == Gender.Other.GetDescription().ToLower())
	                {
	                    records[i].Gender = Gender.Other.ToString();
	                }
	                else
	                {
	                    errors.Add(isEng ? "Gender is invalid" : "Giới tính không hợp lệ");
	                }
	            }
	            
	            // Validate library card information (if any)
	            if (records[i].IsCreateLibraryCard)
	            {
	                // Library card full name
	                var libCardFullName = records[i].LibraryCardFullName;
	                if (string.IsNullOrEmpty(libCardFullName))
	                {
	                    errors.Add(isEng 
	                        ? "Library card full name cannot be empty when create library card" 
	                        : "Tên thẻ thư viện không được rỗng khi đánh dấu tạo thẻ đi kèm");
	                }else if (!Regex.Match(libCardFullName, @"^([A-ZÀ-Ỵ][a-zà-ỵ]*)(\s[A-ZÀ-Ỵ][a-zà-ỵ]*)*$").Success)
	                {
	                    errors.Add(isEng 
	                        ? "Library card full name should start with an uppercase letter for each word" 
	                        : "Tên thẻ thư viện phải bắt đầu bằng chữ cái viết hoa cho mỗi từ");
	                }else if (lastName.Length > 200 || lastName.Length < 1)
	                {
	                    errors.Add(isEng 
	                        ? "Library card full name must be between 1 and 200 characters long" 
	                        : "Tên thẻ thư viện phải có độ dài từ 1 đến 200 ký tự");
	                }
	                
	                // Library card avatar
	                var libCardAvatar = records[i].LibraryCardAvatar;
	                if (!string.IsNullOrEmpty(libCardAvatar) &&
	                    imageFileNames != null && !imageFileNames.Contains(libCardAvatar))
	                {
	                    errors.Add(isEng 
	                        ? "Library card avatar is not exist in collection of input images" 
	                        : "Không tìm thấy ảnh thẻ trong danh sách ảnh đầu vào");
	                }
	                
	                // Library card barcode
	                var barcode = records[i].Barcode;
	                if (string.IsNullOrEmpty(barcode))
	                {
	                    errors.Add(isEng ? "Barcode is required when create library card" : "Yêu cầu nhập mã thẻ khi đánh dấu tạo thẻ đi kèm");
	                }else if (!barcode.StartsWith(_appSettings.LibraryCardBarcodePrefix))
	                {
	                    errors.Add(isEng 
	                        ? $"Barcode is invalid. Must start with '{_appSettings.LibraryCardBarcodePrefix}'" 
	                        : $"Mã thẻ phải bắt đầu bằng '{_appSettings.LibraryCardBarcodePrefix}'");
	                }
	                else
	                {
	                    // Check barcode exist
	                    var isBarcodeExist = await _unitOfWork.Repository<User, Guid>().AnyAsync(u => 
	                        u.LibraryCard != null && u.LibraryCard.Barcode == barcode);
	                    if (isBarcodeExist)
	                    {
	                        errors.Add(isEng ? "Barcode has already existed" : "Mã thẻ đã tồn tại");
	                    }
	                }
	                
	                // Issuance method
	                var issuanceMethod = records[i].IssuanceMethod;
	                if (string.IsNullOrEmpty(issuanceMethod))
	                {
	                    errors.Add(isEng ? "Issuance method is required" : "Hình thức đăng ký thẻ không được rỗng");
	                }else if (!Enum.TryParse(issuanceMethod, true, out LibraryCardIssuanceMethod issuanceMethodEnum))
	                {
	                    if(issuanceMethod.ToLower() == LibraryCardIssuanceMethod.Online.GetDescription().ToLower())
	                    {
	                        records[i].IssuanceMethod = LibraryCardIssuanceMethod.Online.ToString();
	                    }else if (issuanceMethod.ToLower() == LibraryCardIssuanceMethod.InPerson.GetDescription().ToLower())
	                    {
	                        records[i].IssuanceMethod = LibraryCardIssuanceMethod.InPerson.ToString();
	                    }
	                    else
	                    {
	                        errors.Add(isEng ? "Issuance method is invalid" : "Hình thức đăng ký thẻ không hợp lệ");
	                    }
	                }
	                
	                // Library card status
	                var cardStatus = records[i].LibraryCardStatus;
	                if (string.IsNullOrEmpty(cardStatus))
	                {
	                    errors.Add(isEng ? "Library card status is required" : "Trạng thái thẻ không được rỗng");
	                }else if (!Enum.TryParse(cardStatus, true, out LibraryCardStatus validCardStatus))
	                {
	                    if(cardStatus.ToLower() == LibraryCardStatus.Active.GetDescription().ToLower())
	                    {
	                        records[i].LibraryCardStatus = LibraryCardStatus.Active.ToString();
	                    }
	                    else if (cardStatus.ToLower() == LibraryCardStatus.UnPaid.GetDescription().ToLower())
	                    {
		                    records[i].LibraryCardStatus = LibraryCardStatus.UnPaid.ToString();
	                    }
	                    else if (cardStatus.ToLower() == LibraryCardStatus.Expired.GetDescription().ToLower())
	                    {
	                        records[i].LibraryCardStatus = LibraryCardStatus.Expired.ToString();
	                    }
	                    else if (cardStatus.ToLower() == LibraryCardStatus.Pending.GetDescription().ToLower())
	                    {
	                        records[i].LibraryCardStatus = LibraryCardStatus.Pending.ToString();
	                    }
	                    else if (cardStatus.ToLower() == LibraryCardStatus.Rejected.GetDescription().ToLower())
	                    {
		                    records[i].LibraryCardStatus = LibraryCardStatus.Rejected.ToString();
	                    }
	                    else
	                    {
	                        errors.Add(isEng ? "Library card status is invalid" : "Trạng thái thẻ không hợp lệ");
	                    }
	                    
	                }else
	                {
	                    switch (validCardStatus)
	                    {
		                    case LibraryCardStatus.UnPaid:
			                    // Required issue date
			                    if (!records[i].IssueDate.HasValue)
			                    {
				                    errors.Add(isEng 
					                    ? "Issue date must be assigned when card status is Unpaid" 
					                    : "Ngày tạo thẻ không được rỗng khi thẻ đang ở trạng thái 'Chưa thanh toán'");
			                    }
			                    
			                    // Check for exist expiry date when card mark as pending
			                    if (records[i].ExpiryDate.HasValue)
			                    {
				                    errors.Add(isEng 
					                    ? "Expiry date must not assigned when card status is Unpaid" 
					                    : "Không điền ngày hết hạn khi thẻ đang ở trạng thái 'Chưa thanh toán'");
			                    }
			                    break;
	                        case LibraryCardStatus.Pending:
	                            // Required issue date
	                            if (!records[i].IssueDate.HasValue)
	                            {
	                                errors.Add(isEng 
	                                    ? "Issue date must be assigned when card status is Pending" 
	                                    : "Ngày tạo thẻ không được rỗng khi thẻ đang ở trạng thái 'Đang chờ duyệt'");
	                            }
	                            // Check for exist expiry date when card mark as pending
	                            if (records[i].ExpiryDate.HasValue)
	                            {
	                                errors.Add(isEng 
	                                    ? "Expiry date must not assigned when card status is Pending" 
	                                    : "Không điền ngày hết hạn khi thẻ đang ở trạng thái 'Đang chờ duyệt'");
	                            }
	                            break;
	                        case LibraryCardStatus.Rejected:
		                        // Required issue date
		                        if (!records[i].IssueDate.HasValue)
		                        {
			                        errors.Add(isEng 
				                        ? "Issue date must be assigned when card status is Rejected" 
				                        : "Ngày tạo thẻ không được rỗng khi thẻ đang ở trạng thái 'Bị từ chối'");
		                        }
		                        // Check for exist expiry date when card mark as pending
		                        if (records[i].ExpiryDate.HasValue)
		                        {
			                        errors.Add(isEng 
				                        ? "Expiry date must not assigned when card status is Rejected" 
				                        : "Không điền ngày hết hạn khi thẻ đang ở trạng thái 'Bị từ chối'");
		                        }
		                        break;
	                        case LibraryCardStatus.Active:
	                            // Check for exist issue and expiry date when card mark as active
	                            if (!records[i].IssueDate.HasValue || !records[i].ExpiryDate.HasValue)
	                            {
	                                errors.Add(isEng 
	                                    ? "Issue & expiry date must assigned when card status is Active" 
	                                    : "Ngày tạo thẻ và ngày hết hạn không được rỗng khi thẻ đang ở trạng thái 'Đang hoạt động'");
	                            }
	                            if (records[i].IssueDate != null && 
	                                     records[i].ExpiryDate != null && 
	                                     records[i].IssueDate > records[i].ExpiryDate)
	                            {
	                                errors.Add(isEng 
	                                    ? "Issue date must smaller than expiry date when card status is Active" 
	                                    : "Ngày tạo thẻ phải nhỏ hơn ngày hết hạn khi thẻ đang ở trạng thái 'Đang hoạt động'");
	                            }
	                            break;
	                        case LibraryCardStatus.Expired:
	                            // Check for exist issue and expiry date when card mark as active
	                            if (!records[i].IssueDate.HasValue || !records[i].ExpiryDate.HasValue)
	                            {
	                                errors.Add(isEng 
	                                    ? "Issue & expiry date must assigned when card status is Expired" 
	                                    : "Ngày tạo thẻ và ngày hết hạn không được rỗng khi thẻ đang ở trạng thái 'Hết hạn'");
	                            }
	                            if (records[i].IssueDate != null && 
	                                      records[i].ExpiryDate != null && 
	                                      records[i].IssueDate < records[i].ExpiryDate)
	                            {
	                                errors.Add(isEng 
	                                    ? "Issue date must exceed than expiry date when card status is Expired" 
	                                    : "Ngày tạo thẻ phải lớn hơn ngày hết hạn khi thẻ đang ở trạng thái 'Hết hạn'");
	                            }
	                            break;
	                    }
	                }
	            }
	                
	            if (errors.Any()) // Invoke any error
	            {
	                // Add collection of error messages with specific row index
	                errDic.Add(currRow, errors.ToArray());
	            }
	            // Increase curr row index
	            currRow++;
	        }

	        return errDic;
	    }
		
		private (Dictionary<int, List<string>> Errors, Dictionary<int, List<int>> Duplicates) DetectDuplicatesInFile(
	        List<LibraryCardHolderCsvRecordDto> records, 
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
	                    var title when title == nameof(LibraryCard.FullName).ToUpperInvariant() => record.LibraryCardFullName?.Trim()
	                        .ToUpperInvariant(),
	                    var avatarImage when avatarImage == nameof(User.Address).ToUpperInvariant() => record.Address
	                        ?.Trim().ToUpperInvariant(),
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
		
		private async Task<bool> SendActivatedEmailAsync(
			string email, string password, 
			LibraryCardDto cardDto,
			TransactionDto transactionDto, string libName, string libContact, bool isEmployeeCreated = false)
		{
			try
			{
				// Email subject 
				var subject = "[ELIBRARY] Thẻ Thư Viện Đã Kích Hoạt";
                            
				// Progress send confirmation email
				var emailMessageDto = new EmailMessageDto( // Define email message
					// Define Recipient
					to: new List<string>() { email },
					// Define subject
					subject: subject,
					// Add email body content
					content: GetLibraryCardActivatedEmailBody(
						email: email,
						password: password,
						cardDto: cardDto,
						transactionDto: transactionDto,
						libName: libName,
						libContact:libContact,
						isEmployeeCreated: isEmployeeCreated)
				);
                            
				// Process send email
				return await _emailService.SendEmailAsync(emailMessageDto, isBodyHtml: true);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process send library card activated email");
			}
		}
		
		private string GetLibraryCardActivatedEmailBody(
		    string email, string password,
		    LibraryCardDto cardDto, TransactionDto transactionDto, 
		    string libName, string libContact, bool isEmployeeCreated = false)
		{
		    // Custom message based on who performed
		    string employeeMessage = !isEmployeeCreated ? "Vui lòng chờ để được xét duyệt." : "";
		    var culture = new CultureInfo("vi-VN");

		    return $$"""
		          <!DOCTYPE html>
		          <html>
		          <head>
		              <meta charset="UTF-8">
		              <title>Thông Báo Kích Hoạt Thẻ Thư Viện</title>
		              <style>
		                  body {
		                      font-family: Arial, sans-serif;
		                      line-height: 1.6;
		                      color: #333;
		                  }
		                  .header {
		                      font-size: 18px;
		                      color: #2c3e50;
		                      font-weight: bold;
		                  }
		                  .details {
		                      margin: 15px 0;
		                      padding: 10px;
		                      background-color: #f9f9f9;
		                      border-left: 4px solid #27ae60;
		                  }
		                  .details li {
		                      margin: 5px 0;
		                  }
		                  .barcode {
		                      color: #2980b9;
		                      font-weight: bold;
		                  }
		                  .expiry-date {
		                      color: #27ae60;
		                      font-weight: bold;
		                  }
		                  .status-label {
		                      color: #e74c3c;
		                      font-weight: bold;
		                  }
		                  .status-text {
		                      color: #f39c12;
		                      font-weight: bold;
		                  }
		                  .login-info {
		                      background-color: #eef5ff;
		                      padding: 10px;
		                      border-left: 4px solid #3498db;
		                      font-weight: bold;
		                  }
		                  .footer {
		                      margin-top: 20px;
		                      font-size: 14px;
		                      color: #7f8c8d;
		                  }
		              </style>
		          </head>
		          <body>
		              <p class="header">Thông Báo Kích Hoạt Thẻ Thư Viện</p>
		              <p>Xin chào {{cardDto.FullName}},</p>
		              <p>Thẻ thư viện của bạn đã được kích hoạt thành công. {{employeeMessage}}</p>
		              
		              <p><strong>Thông Tin Đăng Nhập:</strong></p>
		              <div class="login-info">
		                  <ul>
		                      <li><strong>Email:</strong> {{email}}</li>
		                      <li><strong>Mật khẩu:</strong> {{password}}</li>
		                  </ul>
		                  <p>Vui lòng đăng nhập và đổi mật khẩu để bảo mật tài khoản.</p>
		              </div>

		              <p><strong>Chi Tiết Thẻ Thư Viện:</strong></p>
		              <div class="details">
		                  <ul>
		                      <li><span class="barcode">Mã Thẻ Thư Viện:</span> {{cardDto.Barcode}}</li>
		                      <li><span class="expiry-date">Ngày Hết Hạn:</span> {{cardDto.ExpiryDate:dd/MM/yyyy}}</li>
		                      <li><span class="status-label">Trạng Thái Hiện Tại:</span> <span class="status-text">{{cardDto.Status.GetDescription()}}</span></li>
		                  </ul>
		              </div>

		              <p><strong>Chi Tiết Giao Dịch:</strong></p>
		              <div class="details">
		                  <ul>
		                      <li><strong>Mã Giao Dịch:</strong> {{transactionDto.TransactionCode}}</li>
		                      <li><strong>Ngày Giao Dịch:</strong> {{transactionDto.TransactionDate:dd/MM/yyyy}}</li>
		                      <li><strong>Số Tiền Đã Thanh Toán:</strong> {{transactionDto.Amount.ToString("C0", culture)}}</li>
		                      <li><strong>Phương Thức Thanh Toán:</strong> {{transactionDto.PaymentMethod?.MethodName ?? TransactionMethod.Cash.GetDescription()}}</li>
		                      <li><strong>Trạng Thái Giao Dịch:</strong> {{transactionDto.TransactionStatus.GetDescription()}}</li>
		                  </ul>
		              </div>

		              <p><strong>Chi Tiết Gói Thẻ Thư Viện:</strong></p>
		              <div class="details">
		                  <ul>
		                      <li><strong>Tên Gói:</strong> {{transactionDto.LibraryCardPackage?.PackageName}}</li>
		                      <li><strong>Thời Gian Hiệu Lực:</strong> {{transactionDto.LibraryCardPackage?.DurationInMonths}} tháng</li>
		                      <li><strong>Giá:</strong> {{transactionDto.LibraryCardPackage?.Price.ToString("C0", culture)}}</li>
		                      <li><strong>Mô Tả:</strong> {{transactionDto.LibraryCardPackage?.Description}}</li>
		                  </ul>
		              </div>

		              <p>Nếu bạn có bất kỳ câu hỏi nào hoặc cần hỗ trợ, vui lòng liên hệ với chúng tôi qua email: <strong>{{libContact}}</strong>.</p>

		              <p><strong>Trân trọng,</strong></p>
		              <p>{{libName}}</p>
		          </body>
		          </html>
		          """;
		}
		#endregion
		
		
		private async Task<List<string>> DetectWrongRecord(UserExcelRecord record, IUnitOfWork unitOfWork, SystemLanguage? lang)
	        {
		        var isEng = lang == SystemLanguage.English;
		        var errMsgs = new List<string>();
			
	            DateTime dob;
	            if (!DateTime.TryParseExact(
	                    record.Dob,
	                    "dd/MM/yyyy",
	                    null,
	                    System.Globalization.DateTimeStyles.None,
	                    out dob))
	            {
	                errMsgs.Add("Wrong Datetime format. The format should be dd/MM/yyyy");
	            }

	            if (dob < new DateTime(1900, 1, 1)
	                || dob > DateTime.Now)
	            {
	                errMsgs.Add("BirthDay is not valid");
	            }

	            // Detect validations
	            if (!Regex.IsMatch(record.Email, @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$"))
	            {
		            errMsgs.Add(isEng ? "Invalid email address" : "Email không hợp lệ");
	            }

	            if (!Regex.IsMatch(record.FirstName, @"^([A-ZÀ-Ỵ][a-zà-ỵ]*)(\s[A-ZÀ-Ỵ][a-zà-ỵ]*)*$"))
	            {
		            errMsgs.Add(isEng
			            ? "Firstname should start with an uppercase letter for each word"
			            : "Họ phải bắt đầu bằng chữ cái viết hoa cho mỗi từ");
	            }

	            if (!Regex.IsMatch(record.LastName, @"^([A-ZÀ-Ỵ][a-zà-ỵ]*)(\s[A-ZÀ-Ỵ][a-zà-ỵ]*)*$"))
	            {
		            errMsgs.Add(isEng
			            ? "Lastname should start with an uppercase letter for each word"
			            : "Tên phải bắt đầu bằng chữ cái viết hoa cho mỗi từ");
	            }
					
	            if (record.Phone is not null && !Regex.IsMatch(record.Phone, @"^0\d{9,10}$"))
	            {
		            errMsgs.Add(isEng
			            ? "Phone not valid"
			            : "SĐT không hợp lệ");
	            }

	            var baseSpec = new BaseSpecification<User>(u => u.Email.Equals(record.Email));
	            var user = await unitOfWork.Repository<User, Guid>().GetWithSpecAsync(baseSpec);
	            if (user is not null)
	            {
	                errMsgs.Add("This email has been used");
	            }

	            return errMsgs;
	        }
	}
}