using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;

namespace FPTU_ELibrary.Application.Services
{
	public class RefreshTokenService : GenericService<RefreshToken, RefreshTokenDto, int>,
		IRefreshTokenService<RefreshTokenDto>
	{
		private readonly IUserService<UserDto> _userService;
		private readonly IEmployeeService<EmployeeDto> _employeeService;

		public RefreshTokenService(
	        IUserService<UserDto> userService,
	        IEmployeeService<EmployeeDto> employeeService,
	        IUnitOfWork unitOfWork,
	        IMapper mapper)
            : base(unitOfWork, mapper)
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
					return new ServiceResult(ResultConst.WARNING_NO_DATA_CODE, ResultConst.WARNING_NO_DATA_MSG);
				}

				return new ServiceResult(ResultConst.SUCCESS_READ_CODE, ResultConst.SUCCESS_READ_MSG, 
					_mapper.Map<RefreshTokenDto>(refreshToken));
			}
			catch (Exception)
			{
				throw;
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
					return new ServiceResult(ResultConst.WARNING_NO_DATA_CODE, ResultConst.WARNING_NO_DATA_MSG);
				}

				return new ServiceResult(ResultConst.SUCCESS_READ_CODE, ResultConst.SUCCESS_READ_MSG,
					_mapper.Map<RefreshTokenDto>(refreshToken));
			}
			catch (Exception)
			{
				throw;
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
				if (userResult.Status == ResultConst.SUCCESS_READ_CODE)
				{
					// Map data object to UserDto
					var user = userResult.Data as UserDto;
					// Not exist user
					if (user == null) throw new BadRequestException("Something went wrong while retrieving data.");
					
					// Retrieve refresh token
					var refreshToken = await _unitOfWork.Repository<RefreshToken, int>().GetWithSpecAsync(
						new BaseSpecification<RefreshToken>(rft => rft.UserId == user.UserId));
					// Not exist refresh token
					if(refreshToken == null) return new ServiceResult(ResultConst.FAIL_READ_CODE, ResultConst.FAIL_READ_MSG);
					
					// Response success
					return new ServiceResult(ResultConst.SUCCESS_READ_CODE, ResultConst.SUCCESS_READ_MSG, 
						_mapper.Map<RefreshTokenDto>(refreshToken));
				}
				// Is Employee
				else if(employeeResult.Status != ResultConst.SUCCESS_READ_CODE)
				{
					// Map data object to UserDto
					var employee = employeeResult.Data as EmployeeDto;
					// Not exist user
					if (employee == null) throw new BadRequestException("Something went wrong while retrieving data.");
					
					// Retrieve refresh token
					var refreshToken = await _unitOfWork.Repository<RefreshToken, int>().GetWithSpecAsync(
						new BaseSpecification<RefreshToken>(rft => rft.EmployeeId == employee.EmployeeId));
					// Not exist refresh token
					if(refreshToken == null) return new ServiceResult(ResultConst.FAIL_READ_CODE, ResultConst.FAIL_READ_MSG);
					
					// Response success
					return new ServiceResult(ResultConst.SUCCESS_READ_CODE, ResultConst.SUCCESS_READ_MSG, 
						_mapper.Map<RefreshTokenDto>(refreshToken));
				}
				
				return new ServiceResult(ResultConst.FAIL_READ_CODE, ResultConst.FAIL_READ_MSG);
			}
			catch (Exception)
			{
				throw;
			}
		}
	}
}
