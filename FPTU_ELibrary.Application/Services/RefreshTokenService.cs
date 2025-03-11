using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Serilog;

namespace FPTU_ELibrary.Application.Services
{
	public class RefreshTokenService : GenericService<RefreshToken, RefreshTokenDto, int>,
		IRefreshTokenService<RefreshTokenDto>
	{
		private readonly IUserService<UserDto> _userService;
		private readonly IEmployeeService<EmployeeDto> _employeeService;

		public RefreshTokenService(
			ISystemMessageService msgService,
	        IUserService<UserDto> userService,
	        IEmployeeService<EmployeeDto> employeeService,
	        IUnitOfWork unitOfWork,
	        IMapper mapper,
	        ILogger logger)
            : base(msgService, unitOfWork, mapper, logger)
        {
	        _userService = userService;
	        _employeeService = employeeService;
        }

		public async Task<IServiceResult> GetByUserIdAsync(Guid userId)
		{
			try
			{
				var refreshToken = await _unitOfWork.Repository<RefreshToken, int>().GetWithSpecAsync(
					new BaseSpecification<RefreshToken>(r => r.UserId != null &&
						r.UserId.ToString()!.Equals(userId.ToString())));

				if(refreshToken is null)
				{
					return new ServiceResult(ResultCodeConst.SYS_Warning0004, "Data not found or empty");
				}

				return new ServiceResult(ResultCodeConst.SYS_Success0002, "Get data successfully", 
					_mapper.Map<RefreshTokenDto>(refreshToken));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress get user by id"); 
			}
		}

		public async Task<IServiceResult> GetByEmployeeIdAsync(Guid employeeId)
		{
			try
			{
				var refreshToken = await _unitOfWork.Repository<RefreshToken, int>().GetWithSpecAsync(
					new BaseSpecification<RefreshToken>(r => r.EmployeeId != null &&
						r.EmployeeId.ToString()!.Equals(employeeId.ToString())));

				if (refreshToken is null)
				{
					return new ServiceResult(ResultCodeConst.SYS_Warning0004, "Data not found or empty");
				}

				return new ServiceResult(ResultCodeConst.SYS_Success0002, "Get data successfully",
					_mapper.Map<RefreshTokenDto>(refreshToken));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress get employee by id"); 
			}
		}

		public async Task<IServiceResult> GetByEmailAsync(string email)
		{
			try
			{
				// Check whether email belongs to user 
				var userResult = await _userService.GetByEmailAsync(email);
				var employeeResult = await _employeeService.GetByEmailAsync(email);

				// Is user
				if (userResult.ResultCode == ResultCodeConst.SYS_Success0002)
				{
					// Map data object to UserDto
					var user = userResult.Data as UserDto;
					// Not exist user
					if (user == null) throw new BadRequestException("Something went wrong while retrieving data.");
					
					// Retrieve refresh token
					var refreshToken = await _unitOfWork.Repository<RefreshToken, int>().GetWithSpecAsync(
						new BaseSpecification<RefreshToken>(rft => rft.UserId == user.UserId));
					// Not exist refresh token
					if(refreshToken == null) return new ServiceResult(ResultCodeConst.SYS_Fail0002, "Fail to get data");
					
					// Response success
					return new ServiceResult(ResultCodeConst.SYS_Success0002, "Get data successfully", 
						_mapper.Map<RefreshTokenDto>(refreshToken));
				}
				// Is Employee
				else if(employeeResult.ResultCode != ResultCodeConst.SYS_Success0002)
				{
					// Map data object to UserDto
					var employee = employeeResult.Data as EmployeeDto;
					// Not exist user
					if (employee == null) throw new BadRequestException("Something went wrong while retrieving data.");
					
					// Retrieve refresh token
					var refreshToken = await _unitOfWork.Repository<RefreshToken, int>().GetWithSpecAsync(
						new BaseSpecification<RefreshToken>(rft => rft.EmployeeId == employee.EmployeeId));
					// Not exist refresh token
					if(refreshToken == null) return new ServiceResult(ResultCodeConst.SYS_Fail0002, "Fail to get data");
					
					// Response success
					return new ServiceResult(ResultCodeConst.SYS_Success0002, "Get data successfully", 
						_mapper.Map<RefreshTokenDto>(refreshToken));
				}
				
				return new ServiceResult(ResultCodeConst.SYS_Warning0004, "Data not found or empty");
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress get user by email"); 
			}
		}

		public async Task<IServiceResult> GetByTokenIdAndRefreshTokenIdAsync(string tokenId, string refreshTokenId)
		{
			try
			{
				var refreshToken = await _unitOfWork.Repository<RefreshToken, int>().GetWithSpecAsync(
					new BaseSpecification<RefreshToken>(r => r.TokenId == tokenId 
					                                         && r.RefreshTokenId == refreshTokenId));

				if(refreshToken is null)
				{
					return new ServiceResult(ResultCodeConst.SYS_Warning0004, "Data not found or empty or empty");
				}

				return new ServiceResult(ResultCodeConst.SYS_Success0002, "Get data successfully", 
					_mapper.Map<RefreshTokenDto>(refreshToken));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress get token and refresh token"); 
			}
		}
	}
}
