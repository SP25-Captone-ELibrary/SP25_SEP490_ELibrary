using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Exceptions;
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
using Microsoft.Extensions.Logging;

namespace FPTU_ELibrary.Application.Services
{
	public class UserService : GenericService<User, UserDto, Guid>, IUserService<UserDto>
	{
		private readonly ISystemRoleService<SystemRoleDto> _roleService;
		private readonly IEmailService _emailService;
		private readonly ILogger<UserService> _logger;

		public UserService(
			ILogger<UserService> logger,
			ISystemRoleService<SystemRoleDto> roleService,
			IEmailService emailService,
			IUnitOfWork unitOfWork, 
			IMapper mapper) 
			: base(unitOfWork, mapper)
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
					serviceResult.Status = ResultConst.SUCCESS_INSERT_CODE;
					serviceResult.Message = ResultConst.SUCCESS_INSERT_MSG;
					serviceResult.Data = true;
				}
				else
				{
					serviceResult.Status = ResultConst.FAIL_INSERT_CODE;
					serviceResult.Message = ResultConst.FAIL_INSERT_MSG;
					serviceResult.Data = false;
				}
			}
			catch(Exception)
			{
				throw;
			}
			
			return serviceResult;
		}

		public async Task<IServiceResult> GetByEmailAndPasswordAsync(string email, string password)
		{
			// Query specification
			var baseSpec = new BaseSpecification<User>(u => u.Email.Equals(email));
			// Include job role
			baseSpec.AddInclude(u => u.Role);

			// Get user by query specification
			var user = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(baseSpec);

			// Verify whether the given password match password hash or not
			if (user == null || !HashUtils.VerifyPassword(password, user.PasswordHash))
				return new ServiceResult(ResultConst.FAIL_READ_CODE, ResultConst.FAIL_READ_MSG);

			return new ServiceResult(ResultConst.SUCCESS_READ_CODE, ResultConst.SUCCESS_READ_MSG,
				_mapper.Map<UserDto?>(user));
		}

		public async Task<IServiceResult> GetByEmailAsync(string email)
		{
			// Query specification
			var baseSpec = new BaseSpecification<User>(u => u.Email.Equals(email));
			// Include job role
			baseSpec.AddInclude(u => u.Role);

			// Get user by query specification
			var user = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(baseSpec);
			
			// Not exist user
			if (user == null)
				return new ServiceResult(ResultConst.FAIL_READ_CODE, ResultConst.FAIL_READ_MSG);

			// Response read success
			return new ServiceResult(ResultConst.SUCCESS_READ_CODE, ResultConst.SUCCESS_READ_MSG,
				_mapper.Map<UserDto?>(user));
		}
	}
}
