using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Ocsp;

namespace FPTU_ELibrary.Application.Services
{
    public class AuthenticationService : IAuthenticationService<AuthenticatedUserDto>
	{
		private readonly ILogger<AuthenticationService> _logger;
		private readonly IEmailService _emailService;
		private readonly IUserService<UserDto> _userService;
		private readonly IEmployeeService<EmployeeDto> _employeeService;
		private readonly IRefreshTokenService<RefreshTokenDto> _refreshTokenService;
		private readonly ISystemRoleService<SystemRoleDto> _roleService;
		private readonly WebTokenSettings _webTokenSettings;
		private readonly AppSettings _appSettings;

		public AuthenticationService(
			ILogger<AuthenticationService> logger,
			IEmailService emailService,
			IUserService<UserDto> userService,
			ISystemRoleService<SystemRoleDto> roleService,
			IEmployeeService<EmployeeDto> employeeService,
			IRefreshTokenService<RefreshTokenDto> refreshTokenService,
			IOptionsMonitor<WebTokenSettings> monitor,
			IOptionsMonitor<AppSettings> appSettingMonitor)
        {
			_logger = logger;
			_emailService = emailService;
			_userService = userService;
			_employeeService = employeeService;
			_refreshTokenService = refreshTokenService;
			_roleService = roleService;
			_webTokenSettings = monitor.CurrentValue;
			_appSettings = appSettingMonitor.CurrentValue;
        }

        public async Task<IServiceResult> ConfirmEmailAsync(string email, string emailVerificationCode)
		{
			// Get created user by email
			var getWithSpecResult = await _userService.GetWithSpecAsync(
				new BaseSpecification<User>(u => u.Email.Equals(email)));
			if (getWithSpecResult.Data is null) throw new NotFoundException("User", email);

			// Map service response data to UserDto
			var userDto = (getWithSpecResult.Data as UserDto)!;
			// Check if account already confirm
			if (userDto.IsActive) throw new BadRequestException("Account already confirmed.");

			// Check if verification code match
			var isMatched = userDto.EmailVerificationCode == emailVerificationCode;
			if (isMatched) // Matched
			{
				// Remove current confirm code
				userDto.EmailVerificationCode = null!;
				// Change to active account
				userDto.IsActive = true;
				// Save change
				var isSaveResult = await _userService.UpdateAsync(userDto.UserId, userDto);
				if (isSaveResult.Data is true) // Save successfully
				{
					return new ServiceResult(ResultConst.SUCCESS_UPDATE_CODE, "Account confirmation successfully");
				}
				else
				{
					_logger.LogError("Something went wrong while update email verification code");
					throw new Exception("Something went wrong while update email verification code");
				}
			}

			// BadRequest
			_logger.LogError("User with email {0} give wrong verification code", userDto.Email);
			throw new BadRequestException("Email verification code not match, please check again or resend the code.");
		}

		public async Task<IServiceResult> RefreshTokenAsync(string email, string refreshToken)
		{
			// Check exist user email
			var getRefreshTokenResult = await _refreshTokenService.GetByEmailAsync(email);
			if (getRefreshTokenResult.Data != null) // Exist refresh token
			{
				// Map to RefreshTokenDto
				var refreshTokenDto = getRefreshTokenResult.Data as RefreshTokenDto;
				if (refreshTokenDto == null) throw new NotFoundException("Refresh token not found.");
				// Retrieve refresh token limit
				var maxRefreshTokenLifeSpan = _appSettings.MaxRefreshTokenLifeSpan;
				// Check whether valid refresh token limit
				if (refreshTokenDto.RefreshCount + 1 > maxRefreshTokenLifeSpan)
				{
					throw new ForbiddenException("Refresh token limit reached.");
				}
								
				// Update refresh token 
				refreshTokenDto.RefreshTokenId = new JwtUtils().GenerateRefreshToken();
				refreshTokenDto.RefreshCount += 1;
				
				// Progress update
				var updateResult = await _refreshTokenService.UpdateAsync(refreshTokenDto.Id, refreshTokenDto);
			}
			else // Not exist refresh token
			{
				// Generate new refresh token
			}
			
			// Generate new refresh token 
			var refreshTokenId = new JwtUtils().GenerateRefreshToken();
			// Update
			return null!;
		}

		public async Task<IServiceResult> SignInAsync(AuthenticatedUserDto user)
		{
			// Validation
			var authenValidationResult = await ValidatorExtensions.ValidateAsync(user);
			if (authenValidationResult != null && !authenValidationResult.IsValid)
			{
				throw new UnprocessableEntityException("Invalid credentials", 
					authenValidationResult.ToProblemDetails().Errors);
			}

			// Try to authenticate with user
			var getUserResult = await _userService.GetByEmailAndPasswordAsync(
							user.Email, user.Password);
			if (getUserResult.Status == ResultConst.SUCCESS_READ_CODE)
			{
				var userDto = (getUserResult.Data as UserDto)!;

				// Initialize new authenticated user
				user = new AuthenticatedUserDto()
				{
					Id = userDto.UserId,
					Email = userDto.Email,
					FirstName = userDto.FirstName ?? string.Empty,
					LastName = userDto.LastName ?? string.Empty,
					RoleId = userDto.RoleId,
					RoleName = userDto.Role.EnglishName,
					IsEmployee = false,
					IsActive = userDto.IsActive,
					Password = string.Empty
				};
			}

			// Try to authenticate with employee
			var getemployeeResult = await _employeeService.GetByEmailAndPasswordAsync(
							user.Email, user.Password);
			if (getemployeeResult.Status == ResultConst.SUCCESS_READ_CODE)
			{
				var employee = (getUserResult.Data as EmployeeDto)!;

				// Initialize new authenticated user
				user = new AuthenticatedUserDto()
				{
					Id = employee.EmployeeId,
					FirstName = employee.FirstName,
					LastName = employee.LastName,
					Email = employee.Email,
					RoleId = employee.JobRole.JobRoleId,
					RoleName = employee.JobRole.EnglishName,
					IsEmployee = true,
					IsActive = employee.IsActive,
					Password = string.Empty
				};
			}

			// Check whether authentication success
			if (user.Id != Guid.Empty && !string.IsNullOrEmpty(user.RoleName)) // Is exist user/emloyee infor
			{
				// Check user status
				if (!user.IsActive) throw new ForbiddenException("You don’t have permission to access");

				// Generate authentication resp
				var resp = await new JwtUtils(_webTokenSettings).GenerateJWTTokenAsync(user);

				if (!string.IsNullOrEmpty(resp.AccessToken) // Exist access token
					&& resp.ValidTo > DateTime.UtcNow) // Valid token expiration date
				{
					// Check exist user refresh token 
					var getTokenResult = !user.IsEmployee
						? await _refreshTokenService.GetByUserIdAsync(user.Id)
						: await _refreshTokenService.GetByEmployeeIdAsync(user.Id);

					// Initialize RefreshTokenDto
					var refreshTokenDto = new RefreshTokenDto();
					// Progress add refresh token whenever user refresh token not exist
					if(getTokenResult.Data is null)
					{
						// Current local datetime
						var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
							// SE Asia timezone
							TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
						// Generate refresh token id
						var refreshTokenId = new JwtUtils().GenerateRefreshToken();
						// Assign properties
						refreshTokenDto = new RefreshTokenDto()
						{
							CreateDate = currentLocalDateTime,
							ExpiryDate = DateTime.UtcNow.AddMinutes(_webTokenSettings.RefreshTokenLifeTimeInMinutes),
							RefreshTokenId = refreshTokenId,
							RefreshCount = 0
						};

						// Check whether is User or Employee
						if (user.IsEmployee) // Is employee
						{
							refreshTokenDto.EmployeeId = user.Id;
						}
						else // Is user
						{
							refreshTokenDto.UserId = user.Id;
						}

						// Add & Save refresh token
						var serviceResult = await _refreshTokenService.CreateAsync(refreshTokenDto);
						if (serviceResult.Status != ResultConst.SUCCESS_INSERT_CODE)
						{
							_logger.LogError("Something went wrong while add create refresh token");
							throw new Exception("Something went wrong while add create refresh token");
						}
					}
					else
					{
						refreshTokenDto = (getTokenResult.Data as RefreshTokenDto)!;
					}

					// Response success
					return new ServiceResult(ResultConst.SUCCESS_SIGNIN_CODE, ResultConst.SUCCESS_SIGNIN_MSG,
						new AuthenticateResultDto
						{
							AccessToken = resp.AccessToken,
							RefreshToken = refreshTokenDto.RefreshTokenId.ToString(),
							ValidTo = resp.ValidTo
						});
				}
			}
			
			throw new UnauthorizedException("Wrong email or password.");
		}

		public async Task<IServiceResult> SignUpAsync(AuthenticatedUserDto user, bool isSignUpFromExternal = false)
		{
			// Validations 
			var validationResult = await ValidatorExtensions.ValidateAsync(user);
			if (validationResult != null 
				&& !validationResult.IsValid
				// Validate only from internal sign-up
				&& !isSignUpFromExternal) // Invoke validation errors
			{
				// Throw exception to middleware pipeline
				throw new UnprocessableEntityException("Validation errors", validationResult.ToProblemDetails().Errors);
			}

			// Check exist email
			var checkAnyResult = await _userService.AnyAsync(u => u.Email.Equals(user.Email));
			if (checkAnyResult.Data is true) throw new BadRequestException("Email already exist");

			// Check exist & valid for user code
			// TODO: Check exist for user code within the student management system
			if (!string.IsNullOrEmpty(user.UserCode)) // Create as student
			{
				// Get student role
				var result = await _roleService.GetByNameAsync(Role.Student);
				if (result.Status == ResultConst.SUCCESS_READ_CODE)
				{
					// Assign role
					user.RoleId = (result.Data as SystemRoleDto)!.RoleId;
				}
				else
				{
					_logger.LogError("Not found any role with nameof Student");
					throw new NotFoundException("Role", "Student");
				}
			}
			else // Create as general member
			{
				// Get general member role
				var result = await _roleService.GetByNameAsync(Role.GeneralMember);
				if (result.Status == ResultConst.SUCCESS_READ_CODE)
				{
					// Assign role
					user.RoleId = (result.Data as SystemRoleDto)!.RoleId;
				}
				else
				{
					_logger.LogError("Not found any role with nameof GeneralMember");
					throw new NotFoundException("Role", "GeneralMember");
				}
			}

			// Hash password (if any)
			if(!isSignUpFromExternal) user.Password = HashUtils.HashPassword(user.Password);
			// Progress create new user
			var createdResult = await _userService.CreateAsync(user.ToUserDto());
			if (createdResult.Data is true)
			{
				// Generate random confirmation code
				var confirmationCode = StringUtils.GenerateCode();

				// Progress send confirmation email
				// Define email message
				var emailMessageDto = new EmailMessageDto(
					// Define Recipient
					to: new List<string>() { user.Email },
					// Define subject
					subject: "ELibrary - confirmation code",
					// Add email body content
					content: $@"
						<div style='font-family: Arial, sans-serif; color: #333; line-height: 1.6;'>
							<h3>Hi {user.FirstName} {user.LastName},</h3>
							<p>Thank you for registering with <b>ELibrary</b>. Here's your confirmation code:</p>
							<h1 style='font-weight: bold; color: #2C3E50;'>{confirmationCode}</h1>
							<p>Use this code to complete your registration.</p>
							<br />
							<p style='font-size: 16px;'>Thanks,</p>
							<p style='font-size: 16px;'>The ELibrary Team</p>
						</div>"
				);
				// Send email
				var isEmailSent = await _emailService.SendEmailAsync(message: emailMessageDto, isBodyHtml: true);

				// Get created user by email
				var getWithSpecResult = await _userService.GetWithSpecAsync(
					new BaseSpecification<User>(u => u.Email.Equals(user.Email)));
				if (getWithSpecResult.Data is null) throw new NotFoundException("User", user.Email);

				var userDto = (getWithSpecResult.Data as UserDto)!;
				// Update confirmation code
				userDto.EmailVerificationCode = confirmationCode;
				// Save to DB
				var saveResult = await _userService.UpdateAsync(userDto.UserId, userDto);
				if (saveResult.Data is false) // Save failed
				{
					_logger.LogError("Error invoke while saving confirmation code");
					throw new Exception("Something went wrong, save confirmation code");
				}

				return new ServiceResult(ResultConst.SUCCESS_INSERT_CODE, ResultConst.SUCCESS_INSERT_MSG);
			}
			else
			{
				_logger.LogError("Something went wrong, fail to create new user");
				throw new Exception("Something went wrong, fail to create new user");
			}
		}
	}
}
