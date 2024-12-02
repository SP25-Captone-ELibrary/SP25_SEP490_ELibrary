using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Common.Constants;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Entities.Base;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Ocsp;

namespace FPTU_ELibrary.Application.Services
{
    public class AuthenticationService : IAuthenticationService<AuthenticateUserDto>
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

		public async Task<IServiceResult> RefreshTokenAsync(
			string email, string userType, string name,
			string roleName, string tokenId, string refreshTokenId)
		{
			// Check exist refresh token by tokenId and refreshTokenId
			var getRefreshTokenResult = await _refreshTokenService.GetByTokenIdAndRefreshTokenIdAsync(
				tokenId, refreshTokenId);
			if (getRefreshTokenResult.Data != null) // Exist refresh token
			{
				// Map to RefreshTokenDto
				var refreshTokenDto = (getRefreshTokenResult.Data as RefreshTokenDto)!;
				// Retrieve refresh token limit
				var maxRefreshTokenLifeSpan = _appSettings.MaxRefreshTokenLifeSpan;
				// Check whether valid refresh token limit
				if (refreshTokenDto.RefreshCount + 1 > maxRefreshTokenLifeSpan)
				{
					throw new ForbiddenException("Refresh token limit reached.");
				}
				
				// Generate new tokenId
				tokenId = Guid.NewGuid().ToString();
				// Update refresh token 
				refreshTokenDto.TokenId = tokenId;
				// refreshTokenDto.RefreshTokenId = new JwtUtils().GenerateRefreshToken();
				refreshTokenDto.RefreshCount += 1;
				
				// Progress update
				var updateResult = await _refreshTokenService.UpdateAsync(refreshTokenDto.Id, refreshTokenDto);
				if (updateResult.Status == ResultConst.SUCCESS_UPDATE_CODE) // Update success
				{
					// Generate authenticated user
					var authenticatedUserDto = new AuthenticateUserDto()
					{
						Email = email, 
						FirstName = name,
						LastName = string.Empty,
						RoleName = roleName,
						IsEmployee = userType.Equals(ClaimValues.EMPLOYEE_CLAIMVALUE),
						IsActive = true
					};
					
					// Generate access token
					var generateResult = await new JwtUtils(_webTokenSettings)
						.GenerateJwtTokenAsync(tokenId: tokenId, user: authenticatedUserDto);
					
					return new ServiceResult(ResultConst.SUCCESS_SIGNIN_CODE, "Refresh token successfully",
						new AuthenticateResultDto
							{
								AccessToken = generateResult.AccessToken,
								RefreshToken = refreshTokenDto.RefreshTokenId,
								ValidTo = generateResult.ValidTo
							});
				}
				else // Fail to update
				{
					_logger.LogError("Fail to save new refresh token");
					throw new Exception("Fail to save new refresh token");
				}
			}
			
			// Resp not found 
			throw new NotFoundException("Not found refresh token");
		}

		public async Task<IServiceResult> SignUpAsync(AuthenticateUserDto user)
		{
			// Validate user input
			await ValidateUserInputAsync(user);

			// Check exist email
			var checkAnyResult = await _userService.AnyAsync(u => u.Email.Equals(user.Email));
			if (checkAnyResult.Data is true) throw new BadRequestException("Email already exist");

			// Set default as general member role
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

			// Hash password
			user.Password = HashUtils.HashPassword(user.Password!);
			// Progress create new user
			var createdResult = await _userService.CreateAsync(user.ToUserDto());
			if (createdResult.Data is true)
			{
				// Generate random confirmation code
				var confirmationCode = StringUtils.GenerateCode();

				// Progress send confirmation email
				var emailMessageDto = new EmailMessageDto( // Define email message
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
				await _emailService.SendEmailAsync(message: emailMessageDto, isBodyHtml: true);

				// Get created user by email
				var getWithSpecResult = await _userService.GetWithSpecAsync(
					new BaseSpecification<User>(u => u.Email.Equals(user.Email)));
				if (getWithSpecResult.Data is null) throw new NotFoundException("User", user.Email);

				// Retrieve data object and map to UserDto
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
		
		public async Task<IServiceResult> SignInAsync(AuthenticateUserDto user, 
			bool isSignInFromExternalProvider = false)
		{
			// Validation
			await ValidateUserInputAsync(user, skipValidation: isSignInFromExternalProvider);
			
			// Determine sign-in type
			if (isSignInFromExternalProvider) // Is from external provider
			{
				// Is exist user 
				var userDto = (await _userService.GetByEmailAsync(user.Email)).Data as UserDto;
				if (userDto == null) // Add new 
				{
					// General member role
					var role = (await _roleService.GetByNameAsync(Role.GeneralMember)).Data as SystemRoleDto;
					if (role == null) throw new NotFoundException("GeneralMember role is not found to create new user");
					
					// User id 
					var userId = Guid.NewGuid();
					// User role
					user.RoleId = role.RoleId;
					user.RoleName = role.EnglishName;

					// Progress create new user
					var createResult = await _userService.CreateAsync(user.ToUserDto(
						// Create with specific ID and mark as external provider register
						userId: userId, isSignUpFromExternalProvider: true)); 
					
					// Assign ID and set to active if success to mark as authenticate success
					if (createResult.Status == ResultConst.SUCCESS_INSERT_CODE)
					{
						user.Id = userId;
						user.IsActive = true;
						user.IsEmployee = false;
					}
				}
				else // Add user details
				{
					user = new AuthenticateUserDto()
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
			}
			else // With user credentials (email, password)
			{
				// Concurrently query both User and Employee services
				var userResult = await _userService.GetByEmailAsync(user.Email);
				var employeeResult = await _employeeService.GetByEmailAsync(user.Email);

				// Handle User authentication
                if (userResult.Status == ResultConst.SUCCESS_READ_CODE 
                    && userResult.Data is UserDto userDto)
                {
                    if (ValidatePassword(user.Password, userDto.PasswordHash))
                    {
                        user = new AuthenticateUserDto
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
                }
				
                // Handle Employee authentication
                if (employeeResult.Status == ResultConst.SUCCESS_READ_CODE 
                    && employeeResult.Data is EmployeeDto employeeDto)
                {
	                if (ValidatePassword(user.Password, employeeDto.PasswordHash))
	                {
		                user = new AuthenticateUserDto
		                {
			                Id = employeeDto.EmployeeId,
			                Email = employeeDto.Email,
			                FirstName = employeeDto.FirstName,
			                LastName = employeeDto.LastName,
			                RoleId = employeeDto.JobRole.JobRoleId,
			                RoleName = employeeDto.JobRole.EnglishName,
			                IsEmployee = true,
			                IsActive = employeeDto.IsActive,
			                Password = string.Empty
		                };
	                }
                }
			} 
			
			// Handle authenticate user
			return await AuthenticateUserAsync(user);
		}
		
		// Authenticate user
		private async Task<ServiceResult> AuthenticateUserAsync(AuthenticateUserDto user)
		{
			// Validate user status
			if (user.Id == Guid.Empty || string.IsNullOrEmpty(user.RoleName))
				throw new UnauthorizedAccessException("Invalid user information");

			if (!user.IsActive)
				throw new ForbiddenException("You don’t have permission to access");
			
			// Generate token
			var tokenId = Guid.NewGuid().ToString();
			var jwtResponse = await new JwtUtils(_webTokenSettings).GenerateJwtTokenAsync(
				tokenId:tokenId, user:user);
			
			if (string.IsNullOrEmpty(jwtResponse.AccessToken) || jwtResponse.ValidTo <= DateTime.UtcNow)
				throw new Exception("Invalid JWT token generated");
			
			// Handle refresh token
			var refreshTokenDto = await HandleRefreshTokenAsync(user, tokenId);

			// Return success result
			return new ServiceResult(
				ResultConst.SUCCESS_SIGNIN_CODE,
				ResultConst.SUCCESS_SIGNIN_MSG,
				new AuthenticateResultDto
				{
					AccessToken = jwtResponse.AccessToken,
					RefreshToken = refreshTokenDto.RefreshTokenId,
					ValidTo = jwtResponse.ValidTo
				});
		}
		
		// Handle refresh token
		private async Task<RefreshTokenDto> HandleRefreshTokenAsync(AuthenticateUserDto user, string tokenId)
		{
			var getTokenResult = user.IsEmployee
				? await _refreshTokenService.GetByEmployeeIdAsync(user.Id)
				: await _refreshTokenService.GetByUserIdAsync(user.Id);

			if (getTokenResult.Data is null)
			{
				return await CreateNewRefreshTokenAsync(user, tokenId);
			}

			return await UpdateExistingRefreshTokenAsync((RefreshTokenDto)getTokenResult.Data, tokenId);
		}
		
		// Create new refresh token 
		private async Task<RefreshTokenDto> CreateNewRefreshTokenAsync(AuthenticateUserDto user, string tokenId)
		{
			var refreshTokenId = new JwtUtils().GenerateRefreshToken();

			var refreshTokenDto = new RefreshTokenDto
			{
				CreateDate = DateTime.UtcNow,
				ExpiryDate = DateTime.UtcNow.AddMinutes(_webTokenSettings.RefreshTokenLifeTimeInMinutes),
				RefreshTokenId = refreshTokenId,
				RefreshCount = 0,
				TokenId = tokenId,
				UserId = user.IsEmployee ? null : user.Id,
				EmployeeId = user.IsEmployee ? user.Id : null
			};

			var result = await _refreshTokenService.CreateAsync(refreshTokenDto);
			if (result.Status != ResultConst.SUCCESS_INSERT_CODE)
				ThrowServiceException("creating refresh token");

			return refreshTokenDto;
		}
		
		// Update existing refresh token
		private async Task<RefreshTokenDto> UpdateExistingRefreshTokenAsync(RefreshTokenDto refreshTokenDto, string tokenId)
		{
			refreshTokenDto.CreateDate = DateTime.UtcNow;
			refreshTokenDto.RefreshTokenId = new JwtUtils().GenerateRefreshToken();
			refreshTokenDto.TokenId = tokenId;
			refreshTokenDto.RefreshCount = 0;

			var result = await _refreshTokenService.UpdateAsync(refreshTokenDto.Id, refreshTokenDto);
			if (result.Status != ResultConst.SUCCESS_INSERT_CODE)
				ThrowServiceException("updating refresh token");

			return refreshTokenDto;
		}
		
		// Validate user input fields
		private async Task ValidateUserInputAsync(AuthenticateUserDto user, bool skipValidation = false)
		{
			if (!skipValidation)
			{
				var validationResult = await ValidatorExtensions.ValidateAsync(user);
				if (validationResult != null && !validationResult.IsValid)
				{
					throw new UnprocessableEntityException("Invalid credentials", validationResult.ToProblemDetails().Errors);
				}
			}
		}
		
		// Validate password
		private bool ValidatePassword(string? inputPassword, string? storedHash)
		{
			if (string.IsNullOrEmpty(storedHash))
			{
				throw new UnauthorizedException("Your password is not set. Please sign in with an external provider to update it.");
			}

			if (!HashUtils.VerifyPassword(inputPassword ?? string.Empty, storedHash))
			{
				throw new UnauthorizedException("Wrong email or password.");
			}

			return true;
		}

		// Service exception (log & throw exception)
		private void ThrowServiceException(string operation)
		{
			var errorMessage = $"Something went wrong while {operation}.";
			_logger.LogError(errorMessage);
			throw new Exception(errorMessage);
		}
	}
}
