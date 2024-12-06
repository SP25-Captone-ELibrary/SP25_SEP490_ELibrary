using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using Mapster;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services
{
	public class UserService : GenericService<User, UserDto, Guid>, IUserService<UserDto>
	{
		private readonly ISystemRoleService<SystemRoleDto> _roleService;
		private readonly IEmailService _emailService;
		private readonly ILogger _logger;

		public UserService(
			ILogger logger,
			ISystemMessageService msgService,
			ISystemRoleService<SystemRoleDto> roleService,
			IEmailService emailService,
			IUnitOfWork unitOfWork, 
			IMapper mapper) 
			: base(msgService, unitOfWork, mapper, logger)
		{
			_roleService = roleService;
			_emailService = emailService;
			_logger = logger;
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
			catch(Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke while create user");
			}
			
			return serviceResult;
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

		public async Task<IServiceResult> GetByEmailAndPasswordAsync(string email, string password)
		{
			try
			{
				// Query specification
				var baseSpec = new BaseSpecification<User>(u => u.Email.Equals(email));
				// Include job role
				baseSpec.AddInclude(u => u.Role);

				// Get user by query specification
				var user = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(baseSpec);

				// Verify whether the given password match password hash or not
				if (user == null || !HashUtils.VerifyPassword(password, user.PasswordHash))
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
				baseSpec.AddInclude(u => u.Role);

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
	}
}
