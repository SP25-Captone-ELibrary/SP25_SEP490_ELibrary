using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.Roles;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Common.Constants;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Serilog;

namespace FPTU_ELibrary.Application.Services
{
    public class AuthenticationService : IAuthenticationService<AuthenticateUserDto>
	{
		private readonly HttpClient _httpClient;
		private readonly IEmailService _emailService;
		private readonly IUserService<UserDto> _userService;
		private readonly IEmployeeService<EmployeeDto> _employeeService;
		private readonly ILogger _logger;
		private readonly ISystemRoleService<SystemRoleDto> _roleService;
		private readonly IRefreshTokenService<RefreshTokenDto> _refreshTokenService;
		private readonly TokenValidationParameters _tokenValidationParameters;
		private readonly WebTokenSettings _webTokenSettings;
		private readonly AppSettings _appSettings;
		private readonly GoogleAuthSettings _googleAuthSettings;
		private readonly FacebookAuthSettings _facebookAuthSettings;
		private readonly ISystemMessageService _msgService;

		public AuthenticationService(
			HttpClient httpClient,
			ILogger logger,
			IEmailService emailService,
			IUserService<UserDto> userService,
			ISystemMessageService msgService,
			ISystemRoleService<SystemRoleDto> roleService,
			IEmployeeService<EmployeeDto> employeeService,
			IRefreshTokenService<RefreshTokenDto> refreshTokenService,
			TokenValidationParameters tokenValidationParameters,
			IOptionsMonitor<WebTokenSettings> monitor,
			IOptionsMonitor<AppSettings> appSettingMonitor,
			IOptionsMonitor<GoogleAuthSettings> googleAuthMonitor,
			IOptionsMonitor<FacebookAuthSettings> facebookAuthMonitor)
		{
			_logger = logger;
			_httpClient = httpClient;
			_emailService = emailService;
			_userService = userService;
			_msgService = msgService;
			_employeeService = employeeService;
			_refreshTokenService = refreshTokenService;
			_roleService = roleService;
			_tokenValidationParameters = tokenValidationParameters;
			_webTokenSettings = monitor.CurrentValue;
			_appSettings = appSettingMonitor.CurrentValue;
			_googleAuthSettings = googleAuthMonitor.CurrentValue;
			_facebookAuthSettings = facebookAuthMonitor.CurrentValue;
		}

		public async Task<IServiceResult> SignInAsync(string email)
		{
			try
			{
				// Initialize authenticate user 
				AuthenticateUserDto? authUser = null;
				bool isAdmin = false; // Default is not as admin
				
				// Check whether the email belongs to (User/Employee)
				var userResult = await _userService.GetByEmailAsync(email);
				var employeeResult = await _employeeService.GetByEmailAsync(email);
				
				if (userResult.Data == null &&
				    employeeResult.Data == null) // Not found email match any type of user
				{
					var message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
						StringUtils.Format(message, "email"));
				}else if (userResult.Data is UserDto userDto) // Detect email as user
				{
					 // Map to authenticate user
					 authUser = userDto.ToAuthenticateUserDto();
					 // Check whether user is admin
					 if (userDto.Role.EnglishName.Equals(nameof(Role.Administration)))
					 {
						 isAdmin = true; // Mark as admin
					 }
				}else if (employeeResult.Data is EmployeeDto employeeDto) // Detect email as employee
				{
					// Map to authenticate user
					authUser = employeeDto.ToAuthenticateUserDto();
				}
				
				// Handle authentication
				if (authUser != null)
				{
					// UserType determination
                    var userTypeResult = new UserTypeResultDto()
                    {
                    	UserType = isAdmin
                    		// Admin
                    		? UserTypeConstants.Admin
                    		// User/Employee
                    		: authUser.IsEmployee ? UserTypeConstants.Employee : UserTypeConstants.User
                    };
					
					// Check whether account of employee or admin has not updated password yet
					var allowSkipAuthRequired = (
						// Allow skip required when account password is empty and must be employee or admin
						string.IsNullOrEmpty(authUser.PasswordHash) && (authUser.IsEmployee || isAdmin));
							
                    // Check email confirmation
                    if (!authUser.EmailConfirmed && !allowSkipAuthRequired)
                    {
                    	return new ServiceResult(ResultCodeConst.Auth_Warning0008,
                    		await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0008));
                    }
                    
                    // User is not yet in-active or not in deleted status
                    if ((!authUser.IsActive || authUser.IsDeleted) && !allowSkipAuthRequired)
                    {
                        return new ServiceResult(ResultCodeConst.Auth_Warning0001,
                            await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0001));
                    }
                    
					// Response to keep on sign-in with username/password
					if (!string.IsNullOrEmpty(authUser.PasswordHash)) // Exist password
					{
						return new ServiceResult(ResultCodeConst.Auth_Success0003,
							await _msgService.GetMessageAsync(ResultCodeConst.Auth_Success0003), userTypeResult);
					}
					// Response to keep on sign-in with OTP
					// since user sign-up with external provider
					else 
					{
						// Progress sending OTP to user's email
						// Generate confirmation code
						var otpCode = StringUtils.GenerateUniqueCode();
						// Email subject
						var emailSubject = "ELibrary - Xác nhận đăng nhập";
						// Email content
						var emailContent = $@"
						    <div style='font-family: Arial, sans-serif; color: #333; line-height: 1.6;'>
						        <h3>Chào {authUser.FirstName} {authUser.LastName},</h3>
						        <p>Đây là mã xác nhận của bạn:</p>
						        <h1 style='font-weight: bold; color: #2C3E50;'>{otpCode}</h1>
						        <p>Vui lòng sử dụng mã này để hoàn tất quá trình đăng nhập.</p>
						        <br />
						        <p style='font-size: 16px;'>Cảm ơn,</p>
						        <p style='font-size: 16px;'>{_appSettings.LibraryName}</p>
						    </div>";

						var isOtpSent = await SendAndSaveOtpAsync(otpCode, authUser, emailSubject, emailContent);
						if (isOtpSent) // Email sent
						{
							return new ServiceResult(ResultCodeConst.Auth_Success0005,
								await _msgService.GetMessageAsync(ResultCodeConst.Auth_Success0005), userTypeResult);
						}
						else // Fail to send email
						{
							return new ServiceResult(ResultCodeConst.Auth_Fail0002,
								await _msgService.GetMessageAsync(ResultCodeConst.Auth_Fail0002));				
						}
					}
				}
				
				// Unknown error
				return new ServiceResult(ResultCodeConst.SYS_Fail0002, 
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
			}
			catch (UnprocessableEntityException)
			{
				throw;
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress sign-in");
			}
		}

		public async Task<IServiceResult> SignInWithPasswordAsync(AuthenticateUserDto user)
		{
			try
			{
				// Get user by email 
				var userResult = await _userService.GetByEmailAsync(user.Email);

				// Handle User authentication
				if (userResult.ResultCode == ResultCodeConst.SYS_Success0002
				    && userResult.Data is UserDto userDto)
				{
					if (ValidatePassword(user.Password, userDto.PasswordHash))
					{
						if (userDto.TwoFactorEnabled) // Is enable MFA account
						{
							return new ServiceResult(ResultCodeConst.Auth_Warning0010,
								await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0010));
						}
						
						user = new AuthenticateUserDto
						{
							Id = userDto.UserId,
							LibraryCardId = userDto.LibraryCardId,
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
					else // Password not match
					{
						return new ServiceResult(ResultCodeConst.Auth_Warning0007,
							await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0007));
					}

					// Check whether user is admin
					if (userDto.Role.EnglishName.Equals(nameof(Role.Administration)))
					{
						// Not allow to access
						throw new ForbiddenException("Not allow to access");
					}
				}
				else
				{
					var message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(message, "email"));
				}

				// Handle authenticate user
				return await AuthenticateUserAsync(user);
			}
			catch (ForbiddenException)
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
				throw new Exception("Error invoke when progress sign-in");
			}
		}

		public async Task<IServiceResult> SignInWithOtpAsync(string otp, AuthenticateUserDto user)
		{
			try
			{
				// Get user by email 
                var userResult = await _userService.GetByEmailAsync(user.Email);
                	
                // Handle User authentication
                if (userResult.ResultCode == ResultCodeConst.SYS_Success0002
                    && userResult.Data is UserDto userDto)
                {
                	// Check match confirmation code 
                	if (userDto.EmailVerificationCode == otp) // Match
                	{
                		user = new AuthenticateUserDto
                		{
                			Id = userDto.UserId,
			                LibraryCardId = userDto.LibraryCardId,
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
                	else // Not match
                	{
                		return new ServiceResult(ResultCodeConst.Auth_Warning0005, 
                			await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0005));
                	}
                }
                else
                {
                	var message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                	return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
                		StringUtils.Format(message, "email"));
                }
                
                // Handle authenticate user
                return await AuthenticateUserAsync(user);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress sign-in");
			}
		}
		
		public async Task<IServiceResult> SignInAsAdminAsync(AuthenticateUserDto user)
		{
			try
			{
				// Get user by email 
				var userResult = await _userService.GetByEmailAsync(user.Email);

				// Handle User authentication
				if (userResult.ResultCode == ResultCodeConst.SYS_Success0002
				    && userResult.Data is UserDto userDto)
				{
					// Check if user is admin
					if (userDto.Role.EnglishName != Role.Administration.ToString())
					{
						// Not allow to access
						throw new ForbiddenException("Not allow to access");
					}
					
					if (ValidatePassword(user.Password, userDto.PasswordHash))
					{
						if (!userDto.TwoFactorEnabled) 
						{
							return new ServiceResult(ResultCodeConst.Auth_Warning0011,
								await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0011));
						}
						
						// 2FA always required for admin 
						return new ServiceResult(ResultCodeConst.Auth_Warning0010,
							await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0010));
					}
					else // Password not match
					{
						return new ServiceResult(ResultCodeConst.Auth_Warning0007,
							await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0007));
					}
				}
				else
				{
					var message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(message, "email"));
				}
			}
			catch (ForbiddenException)
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
				throw new Exception("Error invoke when progress sign-in");
			}
		}

		public async Task<IServiceResult> SignInAsEmployeeAsync(AuthenticateUserDto user)
		{
			try
            {
            	// Get user by email 
                var employeeResult = await _employeeService.GetByEmailAsync(user.Email);
                    
                // Handle User authentication
                if (employeeResult.ResultCode == ResultCodeConst.SYS_Success0002
                    && employeeResult.Data is EmployeeDto employeeDto)
                {
                    if (ValidatePassword(user.Password, employeeDto.PasswordHash))
                    {
	                    if (employeeDto.TwoFactorEnabled) // Is enable MFA account
	                    {
		                    return new ServiceResult(ResultCodeConst.Auth_Warning0010,
			                    await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0010));
	                    }
	                    
                        user = new AuthenticateUserDto
                        {
                            Id = employeeDto.EmployeeId,
                            Email = employeeDto.Email,
                            FirstName = employeeDto.FirstName,
                            LastName = employeeDto.LastName,
                            RoleId = employeeDto.RoleId,
                            RoleName = employeeDto.Role.EnglishName,
                            IsEmployee = true,
                            IsActive = employeeDto.IsActive,
                            Password = string.Empty
                        };
                    }
                    else // Password not match
                    {
                        return new ServiceResult(ResultCodeConst.Auth_Warning0007, 
                            await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0007));
                    }
                }
                else
                {
                    var message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                    return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
                        StringUtils.Format(message, "email"));
                }
                
                // Handle authenticate user
                return await AuthenticateUserAsync(user);
            }
            catch (UnprocessableEntityException)
            {
            	throw;
            }
            catch (Exception ex)
            {
            	_logger.Error(ex.Message);
            	throw new Exception("Error invoke when progress sign-in");
            }
		}

		public async Task<IServiceResult> SignInWithGoogleAsync(string code)
		{
			try
			{
				// Exchange code for access token
				var tokenResponse = await ExchangeCodeForAccessTokenAsync(code);

				// Validate the access token
				GoogleJsonWebSignature.Payload payload =
					await GoogleJsonWebSignature.ValidateAsync(tokenResponse.IdToken);

				// Validate the audience (client ID)
				if (!payload.Audience.Equals(_googleAuthSettings.ClientId))
				{
					throw new BadRequestException("Invalid audience: Client ID mismatch.");
				}

				// Validate the issuer
				if (!payload.Issuer.Equals("accounts.google.com") &&
				    !payload.Issuer.Equals("https://accounts.google.com"))
				{
					throw new BadRequestException("Invalid issuer: Not a trusted Google account issuer.");
				}

				// Validate the expiration time
				if (payload.ExpirationTimeSeconds == null)
				{
					throw new BadRequestException("Invalid token: Missing expiration time.");
				}
				else
				{
					DateTime now = DateTime.Now.ToUniversalTime();
					DateTime expiration = DateTimeOffset.FromUnixTimeSeconds((long)payload.ExpirationTimeSeconds)
						.DateTime;
					if (now > expiration)
					{
						throw new BadRequestException("Invalid token: Token has expired.");
					}
				}

				// Initialize authenticated user
				AuthenticateUserDto? authenticateUser = null!;
				// Is exist user 
				var userDto = (await _userService.GetByEmailAsync(payload.Email)).Data as UserDto;
				if (userDto == null) // Add new 
				{
					// Initialize authenticate user
					var userId = Guid.NewGuid();
					authenticateUser = new AuthenticateUserDto()
					{
						Id = userId,
						Email = payload.Email,
						Avatar = payload.Picture,
						FirstName = payload.GivenName ?? string.Empty,
						LastName = payload.FamilyName ?? string.Empty,
						IsEmployee = false
					};

					// Progress create new user
					authenticateUser = await CreateNewUserAsync(authenticateUser, createFromExternalProvider: true);
				}
				else
				{
					// Check if account required MFA
					if (userDto.TwoFactorEnabled)
					{
						return new ServiceResult(ResultCodeConst.Auth_Warning0010,
							await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0010), userDto.Email);
					}

					// Add account details
					authenticateUser = new AuthenticateUserDto()
					{
						Id = userDto.UserId,
						LibraryCardId = userDto.LibraryCardId,
						Email = userDto.Email,
						FirstName = userDto.FirstName ?? string.Empty,
						LastName = userDto.LastName ?? string.Empty,
						RoleId = userDto.RoleId,
						RoleName = userDto.Role.EnglishName,
						IsActive = true,
						IsEmployee = false
					};
				}

				// Try to authenticate user
				return await AuthenticateUserAsync(authenticateUser);
			}
			catch (UnprocessableEntityException)
			{
				throw;
			}
			catch (InvalidJwtException ex)
			{
				_logger.Error(ex.Message);
				// Invalid JWT exception handling
				throw new UnauthorizedAccessException("Invalid token: invalid JWT.", ex);
			}
			catch (UnauthorizedAccessException ex)
			{
				_logger.Error(ex.Message);
				throw;
			}
			catch (TokenResponseException trEx)
			{
				_logger.Error(
					"Token exchange failed: error = {Error}, description = {Desc}, uri = {Uri}",
					trEx.Error.Error,
					trEx.Error.ErrorDescription,
					trEx.Error.ErrorUri);
				throw new Exception(trEx.Message);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception(ex.Message);
				// throw new Exception("An error occurred during Google sign-in.", ex);
			}
		}
		
		public async Task<IServiceResult> SignInWithFacebookAsync(string accessToken, int expiresIn)
		{
			try
			{
				// Set base address to HttpClient
				_httpClient.BaseAddress = new Uri(_facebookAuthSettings.BaseGraphAPIUrl);
				
				// Get user info by access token
				HttpResponseMessage response = await _httpClient.GetAsync(
					$"/me?fields=first_name,last_name,middle_name,picture,email&access_token={accessToken}");

				if (response.IsSuccessStatusCode) // Is success response
				{
					// Serialize HttpContent as string
					var result = await response.Content.ReadAsStringAsync();
					// Deserialize json to dynamic object
					var jsonRes = JsonConvert.DeserializeObject<dynamic>(result);	
					
					// Firstname
					var firstname = jsonRes?["first_name"]?.ToString() ?? string.Empty;
					// MiddleName
					var middleName = jsonRes?["middle_name"]?.ToString() ?? string.Empty;
					// Lastname
					var lastName = jsonRes?["last_name"]?.ToString() ?? string.Empty;
					// Email
					var email = jsonRes?["email"]?.ToString();
					// Picture
					var pictureUrl = jsonRes?["picture"]?["data"]?["url"]?.ToString();
					
					// Initialize authenticated user
					AuthenticateUserDto? authenticateUser = null;
					// Is exist user 
					var userDto = (await _userService.GetByEmailAsync(email)).Data as UserDto;
					if (userDto == null) // Add new 
					{
						// Initialize user
						var userId = Guid.NewGuid();
						authenticateUser = new AuthenticateUserDto()
						{
							Id = userId,
							Email = email!,
							Avatar = pictureUrl,
							FirstName = firstname,
							LastName = $"{middleName} {lastName}",
							IsEmployee = false
						};
						
						// Progress create new user
						authenticateUser = await CreateNewUserAsync(authenticateUser, createFromExternalProvider: true);
					}
					else
					{
						// Check if account required MFA
						if (userDto.TwoFactorEnabled) 
						{
							return new ServiceResult(ResultCodeConst.Auth_Warning0010,
								await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0010), userDto.Email);
						}
						
						// Add user details
						authenticateUser = new AuthenticateUserDto()
						{
							Id = userDto.UserId,
							LibraryCardId = userDto.LibraryCardId,
							Email = userDto.Email,
							FirstName = userDto.FirstName ?? string.Empty,
							LastName = userDto.LastName ?? string.Empty,
							RoleId = userDto.RoleId,
							RoleName = userDto.Role.EnglishName,
							IsActive = true,
							IsEmployee = false
						};
					} 
					
					// Try to authenticate user
					return await AuthenticateUserAsync(authenticateUser);
				}
				
				// Fail to get user information 
				throw new Exception("Fail to get user information from Facebook.");
			}
			catch (UnprocessableEntityException)
			{
				throw;
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("An error occurred during Facebook sign-in.", ex);
			}
		}
		
		public async Task<IServiceResult> SignUpAsync(AuthenticateUserDto user)
		{
			try
			{
				// Validate user input
				await ValidateUserInputAsync(user);

				// Check exist email
				var checkUserAnyResult = await _userService.AnyAsync(u => u.Email.Equals(user.Email));
				var checkEmployeeAnyResult = await _employeeService.AnyAsync(e => e.Email.Equals(user.Email));
				if (checkUserAnyResult.Data is true 
				    || checkEmployeeAnyResult.Data is true)
				{
					// Initialize custom errors dic
					var customErrors = new Dictionary<string, string[]>();
					// Add email exist error
					customErrors.Add(
						StringUtils.ToCamelCase(nameof(User.Email)),
						[await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0006)]);
					
					throw new UnprocessableEntityException("Invalid Data", customErrors);
				}

				// Hash password
				user.PasswordHash = HashUtils.HashPassword(user.Password!);
				// Progress create new user
				user = await CreateNewUserAsync(user) ?? null!;

				if (user != null!) // Create user successfully
				{
					// Generate confirmation code
					var otpCode = StringUtils.GenerateUniqueCode();
					// Email subject
					var emailSubject = "ELibrary - Xác nhận đăng ký tài khoản";

					// Email content
					var emailContent = $@"
					    <div style='font-family: Arial, sans-serif; color: #333; line-height: 1.6;'>
					        <h3>Chào {user.FirstName} {user.LastName},</h3>
					        <p>Đây là mã xác nhận của bạn:</p>
					        <h1 style='font-weight: bold; color: #2C3E50;'>{otpCode}</h1>
					        <p>Vui lòng sử dụng mã này để hoàn tất quá trình đăng ký.</p>
					        <br />
					        <p style='font-size: 16px;'>Cảm ơn,</p>
					        <p style='font-size: 16px;'>{_appSettings.LibraryName}</p>
					    </div>";

					var isOtpSent = await SendAndSaveOtpAsync(otpCode, user, emailSubject, emailContent);
					if (isOtpSent) // Email sent
					{
						return new ServiceResult(ResultCodeConst.Auth_Success0005,
							await _msgService.GetMessageAsync(ResultCodeConst.Auth_Success0005));
					}
					else
					{
						return new ServiceResult(ResultCodeConst.Auth_Fail0002,
							await _msgService.GetMessageAsync(ResultCodeConst.Auth_Fail0002));
					}
				}

				return new ServiceResult(ResultCodeConst.SYS_Fail0001,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
			}
			catch (UnprocessableEntityException)
			{
				throw;
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when create new user");
			}
		}
		
        public async Task<IServiceResult> ConfirmEmailForSignUpAsync(string email, string emailVerificationCode)
		{
			try
			{
				// Get created employee by email
                var getWithSpecResult = await _userService.GetWithSpecAsync(
                	new BaseSpecification<User>(u => u.Email.Equals(email)));
                if (getWithSpecResult.Data is null)
                {
                	var errorMSg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                	return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                		StringUtils.Format(errorMSg, "email"));
                }
    
                // Map service response data to UserDto
                var userDto = (getWithSpecResult.Data as UserDto)!;
                // Check if account already confirm
                if (userDto.IsActive)
                {
                	return new ServiceResult(ResultCodeConst.Auth_Warning0004,
                		await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0004));
                };
    
                // Check if verification code match
                var isMatched = userDto.EmailVerificationCode == emailVerificationCode;
                if (isMatched) // Matched
                {
                	// Remove current confirm code
                	userDto.EmailVerificationCode = null!;
                	// Change to active account
                	userDto.IsActive = true;
	                // Mark as email confirmed
	                userDto.EmailConfirmed = true;
                	// Save change
                	var isSaveResult = await _userService.UpdateWithoutValidationAsync(userDto.UserId, userDto);
                	if (isSaveResult.Data is true) // Save successfully
                	{
                		return new ServiceResult(ResultCodeConst.Auth_Success0007, 
                			await _msgService.GetMessageAsync(ResultCodeConst.Auth_Success0007));
                	}
                	else
                	{
                		_logger.Error("Something went wrong while update email verification code");
                		return new ServiceResult(ResultCodeConst.SYS_Fail0003, 
                			await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
                	}
                }
    
                // BadRequest
                _logger.Error("User with email {0} give wrong verification code", userDto.Email);
                // Not match OTP
                return new ServiceResult(ResultCodeConst.Auth_Warning0005, 
                	await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0005));
			}
			catch (Exception ex)
			{
				_logger.Error(ex, ex.Message);
				throw new Exception("Error invoke when process confirm email");
			}
		}

		public async Task<IServiceResult> VerifyChangePasswordOtpAsync(string email, string otp)
		{
			try
			{
				// Concurrently query both User and Employee services
                var userResult = await _userService.GetByEmailAsync(email);
                var employeeResult = await _employeeService.GetByEmailAsync(email);
                if (userResult.Data == null && employeeResult.Data == null) // Not exist
                {
                	var errorMSg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                	return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                		StringUtils.Format(errorMSg, "email"));
                }
                
                // As user account
                if (userResult.Data != null
                    && userResult.Data is UserDto userDto 
                    && userDto.EmailVerificationCode == otp)
                {
	                // Generate password reset token (for user)
                    var recoveryPasswordToken = await new JwtUtils(_webTokenSettings)
                        .GeneratePasswordResetTokenAsync(userDto.ToAuthenticateUserDto());
	                
                    // Remove confirmation code
                    userDto.EmailVerificationCode = null!;
                    var isUpdated =
	                    (await _userService.UpdateWithoutValidationAsync(
		                    userDto.UserId, userDto)).Data is true;
                    
                    if (isUpdated) // Update email confirmed status success
                    {
	                    return new ServiceResult(ResultCodeConst.Auth_Success0009, 
		                    await _msgService.GetMessageAsync(ResultCodeConst.Auth_Success0009),
		                    new RecoveryPasswordResultDto()
		                    {
			                    Token = recoveryPasswordToken,
			                    Email = userDto.Email
		                    });
                    }

                    // Mark as update fail
                    return new ServiceResult(ResultCodeConst.SYS_Fail0003, 
	                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
                }
				// As employee account
				if (employeeResult.Data != null
				    && employeeResult.Data is EmployeeDto employeeDto 
				    && employeeDto.EmailVerificationCode == otp)
                {
	                // Generate password reset token (for employee)
	                var recoveryPasswordToken = await new JwtUtils(_webTokenSettings)
		                .GeneratePasswordResetTokenAsync(employeeDto.ToAuthenticateUserDto());
	                
	                // Update email confirmed status
	                employeeDto.EmailConfirmed = true;
	                // Remove confirmation code
	                employeeDto.EmailVerificationCode = null!;
	                // Change account status
	                employeeDto.IsActive = true;
	                var isUpdated =
		                (await _employeeService.UpdateWithoutValidationAsync(
			                employeeDto.EmployeeId, employeeDto)).Data is true;

	                if (isUpdated) // Update email confirmed status success
	                {
	                    return new ServiceResult(ResultCodeConst.Auth_Success0009, 
	                        await _msgService.GetMessageAsync(ResultCodeConst.Auth_Success0009),
	                        new RecoveryPasswordResultDto()
	                        {
		                        Token = recoveryPasswordToken,
		                        Email = employeeDto.Email
	                        });
	                }

	                // Mark as update fail
	                return new ServiceResult(ResultCodeConst.SYS_Fail0003, 
		                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
                }
                
				// Not match OTP
				return new ServiceResult(ResultCodeConst.Auth_Warning0005, 
					await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0005));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress verify otp");
			}
		}

		public async Task<IServiceResult> ResendOtpAsync(string email)
		{
			try
			{
				// Concurrently query both User and Employee services
				var userResult = await _userService.GetByEmailAsync(email);
				var employeeResult = await _employeeService.GetByEmailAsync(email);
				if (userResult.Data == null && employeeResult.Data == null) // Not exist
				{
					var errorMSg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errorMSg, "email"));
				}
				
				// Check whether email belongs to user or employee
				var authenticatedUser = new AuthenticateUserDto();
				// Initialize user, employee
				if (userResult.ResultCode == ResultCodeConst.SYS_Success0002 
				    && userResult.Data is UserDto userDto)
				{
					authenticatedUser = new AuthenticateUserDto
					{
						// Add necessary detail to process re-send OTP for user
						Id = userDto.UserId,
						Email = userDto.Email,
						IsEmployee = false
					};
				} else if (employeeResult.ResultCode == ResultCodeConst.SYS_Success0002
				           && employeeResult.Data is EmployeeDto employeeDto)
				{
					authenticatedUser = new AuthenticateUserDto
					{
						// Add necessary detail to process re-send OTP for employee
						Id = employeeDto.EmployeeId,
						Email = employeeDto.Email,
						IsEmployee = true,
					};
				}
				
				// Generate confirmation code
				var otpCode = StringUtils.GenerateUniqueCode();
				// Email subject
				var emailSubject = "ELibrary - Gửi lại email xác nhận";

				// Email content
				var emailContent = $@"
				    <div style='font-family: Arial, sans-serif; color: #333; line-height: 1.6;'>
				        <h3>Chào {authenticatedUser.FirstName} {authenticatedUser.LastName},</h3>
				        <p>Đây là mã xác nhận của bạn:</p>
				        <h1 style='font-weight: bold; color: #2C3E50;'>{otpCode}</h1>
				        <p>Vui lòng sử dụng mã này để hoàn tất quá trình.</p>
				        <br />
				        <p style='font-size: 16px;'>Cảm ơn,</p>
				        <p style='font-size: 16px;'>{_appSettings.LibraryName}</p>
				    </div>";

				var isOtpSent = await SendAndSaveOtpAsync(otpCode, authenticatedUser, emailSubject,emailContent);
				if (isOtpSent) // Email sent
				{
					return new ServiceResult(ResultCodeConst.Auth_Success0005,
						await _msgService.GetMessageAsync(ResultCodeConst.Auth_Success0005));
				}
				else // Fail to send email
				{
					return new ServiceResult(ResultCodeConst.Auth_Fail0002,
						await _msgService.GetMessageAsync(ResultCodeConst.Auth_Fail0002));				
				}
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress resend otp");
			}
		}

		public async Task<IServiceResult> ForgotPasswordAsync(string email)
		{
			try
			{
				// Concurrently query both User and Employee services
				var userResult = await _userService.GetByEmailAsync(email);
				var employeeResult = await _employeeService.GetByEmailAsync(email);
				if (userResult.Data == null && employeeResult.Data == null) // Not exist
				{
					var errorMSg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errorMSg, "email"));
				}
				
				// Check whether email belongs to user or employee
				var authenticatedUser = new AuthenticateUserDto();
				if (userResult.ResultCode == ResultCodeConst.SYS_Success0002 
				    && userResult.Data is UserDto userDto)
				{
					authenticatedUser = new AuthenticateUserDto
					{
						Id = userDto.UserId,
						LibraryCardId = userDto.LibraryCardId,
						Email = userDto.Email,
						FirstName = userDto.FirstName ?? string.Empty,
						LastName = userDto.LastName ?? string.Empty,
						RoleId = userDto.RoleId,
						RoleName = userDto.Role.EnglishName,
						IsEmployee = false,
						IsActive = userDto.IsActive,
						Password = string.Empty,
						CreateDate = userDto.CreateDate,
						ModifiedDate = userDto.ModifiedDate,
						Gender = userDto.Gender,
						Address = userDto.Address,
						Avatar = userDto.Avatar,
						EmailVerificationCode = userDto.EmailVerificationCode,
						TwoFactorEnabled = userDto.TwoFactorEnabled,
						TwoFactorSecretKey = userDto.TwoFactorSecretKey,
						TwoFactorBackupCodes = userDto.TwoFactorBackupCodes,
						PhoneVerificationCode = userDto.PhoneVerificationCode,
						PhoneVerificationExpiry = userDto.PhoneVerificationExpiry,
						EmailConfirmed = userDto.EmailConfirmed,
						PhoneNumberConfirmed = userDto.PhoneNumberConfirmed,
						PasswordHash = userDto.PasswordHash,
						Dob = userDto.Dob,
						Phone = userDto.Phone,
					};
				} else if (employeeResult.ResultCode == ResultCodeConst.SYS_Success0002
				           && employeeResult.Data is EmployeeDto employeeDto)
				{
					authenticatedUser = new AuthenticateUserDto
					{
						Id = employeeDto.EmployeeId,
						Email = employeeDto.Email,
						FirstName = employeeDto.FirstName,
						LastName = employeeDto.LastName,
						RoleId = employeeDto.Role.RoleId,
						RoleName = employeeDto.Role.EnglishName,
						IsEmployee = true,
						IsActive = employeeDto.IsActive,
						Password = string.Empty,
						CreateDate = employeeDto.CreateDate,
						ModifiedDate = employeeDto.ModifiedDate,
						Gender = employeeDto.Gender,
						Address = employeeDto.Address,
						Avatar = employeeDto.Avatar,
						EmailVerificationCode = employeeDto.EmailVerificationCode,
						TwoFactorEnabled = employeeDto.TwoFactorEnabled,
						TwoFactorSecretKey = employeeDto.TwoFactorSecretKey,
						TwoFactorBackupCodes = employeeDto.TwoFactorBackupCodes,
						PhoneVerificationCode = employeeDto.PhoneVerificationCode,
						PhoneVerificationExpiry = employeeDto.PhoneVerificationExpiry,
						EmailConfirmed = employeeDto.EmailConfirmed,
						PhoneNumberConfirmed = employeeDto.PhoneNumberConfirmed,
						PasswordHash = employeeDto.PasswordHash,
						UserCode = employeeDto.EmployeeCode,
						Dob = employeeDto.Dob,
						Phone = employeeDto.Phone,
					};
				}
				
				// Generate confirmation code
				var otpCode = StringUtils.GenerateUniqueCode();
				// Email subject
				var emailSubject = "ELibrary - Khôi phục mật khẩu";
				// Email content
				var emailContent = $@"
				    <div style='font-family: Arial, sans-serif; color: #333; line-height: 1.6;'>
				        <h3>Chào {authenticatedUser.FirstName} {authenticatedUser.LastName},</h3>
				        <p>Đây là mã xác nhận để khôi phục mật khẩu của bạn:</p>
				        <h1 style='font-weight: bold; color: #2C3E50;'>{otpCode}</h1>
				        <p>Vui lòng sử dụng mã này để hoàn thành quy trình khôi phục mật khẩu.</p>
				        <br />
				        <p style='font-size: 16px;'>Cảm ơn,</p>
				        <p style='font-size: 16px;'>{_appSettings.LibraryName}</p>
				    </div>";

				var isOtpSent = await SendAndSaveOtpAsync(otpCode, authenticatedUser, emailSubject, emailContent);
				if (isOtpSent) // Email sent
				{
					return new ServiceResult(ResultCodeConst.Auth_Success0005,
						await _msgService.GetMessageAsync(ResultCodeConst.Auth_Success0005),
						new RecoveryPasswordResultDto()
						{
							Email = authenticatedUser.Email
						});
				}

				// Fail to send email
				return new ServiceResult(ResultCodeConst.Auth_Fail0002,
					await _msgService.GetMessageAsync(ResultCodeConst.Auth_Fail0002));	
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception(ex.Message);
			}
		}
	
		public async Task<IServiceResult> ChangePasswordAsync(
			string email, string password, string? token = null)
		{
			try
			{
				// Validate token (if any)
                if (!string.IsNullOrEmpty(token))
                {
                	var jwtToken = await (new JwtUtils(_tokenValidationParameters).ValidateAccessTokenAsync(token));
    
                	if (jwtToken == null)
                	{
                		return new ServiceResult(ResultCodeConst.Auth_Fail0001,
                			await _msgService.GetMessageAsync(ResultCodeConst.Auth_Fail0001));
                	}
                }
                
                // Check exist email
                var getUserResult = await _userService.GetByEmailAsync(email);
                if (getUserResult.ResultCode != ResultCodeConst.SYS_Success0002) // Not exist
                {
                    var errorMSg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                        StringUtils.Format(errorMSg, "email"));
                } else if (getUserResult.Data is UserDto userDto)
                {
                	// Hash password
                    var newPasswordHash = HashUtils.HashPassword(password);
                    // Update password field
                    userDto.PasswordHash = newPasswordHash;
                    // Progress update 
                    var updateResult = await _userService.UpdateWithoutValidationAsync(userDto.UserId, userDto);
                    if (updateResult.Data is true) return new ServiceResult(ResultCodeConst.Auth_Success0006, 
                        await _msgService.GetMessageAsync(ResultCodeConst.Auth_Success0006), true);
                }
                
                return new ServiceResult(ResultCodeConst.Auth_Fail0001, 
                	await _msgService.GetMessageAsync(ResultCodeConst.Auth_Fail0001), false);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception(ex.Message);	
			}
		}
		
		public async Task<IServiceResult> ChangePasswordAsEmployeeAsync(
			string email, string password, string? token = null)
		{
			try
			{
				// Validate token (if any)
				if (!string.IsNullOrEmpty(token))
				{
					var jwtToken = await (new JwtUtils(_tokenValidationParameters).ValidateAccessTokenAsync(token));

					if (jwtToken == null)
					{
						return new ServiceResult(ResultCodeConst.Auth_Fail0001,
							await _msgService.GetMessageAsync(ResultCodeConst.Auth_Fail0001));
					}
				}
			
				// Check exist email
				var getEmployeeResult = await _employeeService.GetByEmailAsync(email);
				if (getEmployeeResult.ResultCode != ResultCodeConst.SYS_Success0002) // Not exist
				{
					var errorMSg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errorMSg, "email"));
				} else if (getEmployeeResult.Data is EmployeeDto employeeDto)
				{
					// Hash password
					var newPasswordHash = HashUtils.HashPassword(password);
					// Update password field
					employeeDto.PasswordHash = newPasswordHash;
					// Progress update 
					var updateResult = await _employeeService.UpdateWithoutValidationAsync(employeeDto.EmployeeId, employeeDto);
					if (updateResult.Data is true) return new ServiceResult(ResultCodeConst.Auth_Success0006, 
						await _msgService.GetMessageAsync(ResultCodeConst.Auth_Success0006), true);
				}
			
				return new ServiceResult(ResultCodeConst.Auth_Fail0001, 
					await _msgService.GetMessageAsync(ResultCodeConst.Auth_Fail0001), false);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception(ex.Message);
			}
		}

		public async Task<IServiceResult> RefreshTokenAsync(string accessToken, string refreshTokenId)
		{
			// Try to validate and extract claims from access token
			var token = new JwtUtils(_tokenValidationParameters).ValidateExpiredAccessToken(accessToken);

			// Retrieve claims from the authenticated user's identity
			var roleName = token?.Claims.FirstOrDefault(c => c.Type == CustomClaimTypes.Role)?.Value;
			var userType = token?.Claims.FirstOrDefault(c => c.Type == CustomClaimTypes.UserType)?.Value;
			var email = token?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;
			var name = token?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Name)?.Value;
			var tokenId = token?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
			if (string.IsNullOrEmpty(email) // Is not exist email claim
			    || string.IsNullOrEmpty(userType) // Is not exist user type claim
			    || string.IsNullOrEmpty(roleName) // Is not exist role claim
			    || string.IsNullOrEmpty(name) // Is not exist name claim
			    || string.IsNullOrEmpty(tokenId)) // Is not exist tokenId claim
			{
				// 401
				throw new UnauthorizedException("Missing token claims.");
			}
			
			// Check exist refresh token by tokenId and refreshTokenId
            var getRefreshTokenResult = await _refreshTokenService.GetByTokenIdAndRefreshTokenIdAsync(
                tokenId, refreshTokenId);
            if (getRefreshTokenResult.Data != null) // Exist refresh token
            {
                // Map to RefreshTokenDto
                var refreshTokenDto = (getRefreshTokenResult.Data as RefreshTokenDto)!;
                // Retrieve refresh token limit
                var maxRefreshTokenLifeSpan = _webTokenSettings.MaxRefreshTokenLifeSpan;
                // Check whether valid refresh token limit
                if (refreshTokenDto.RefreshCount + 1 > maxRefreshTokenLifeSpan)
                {
                	throw new ForbiddenException(
                		await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0002));
                }
                
                // Generate new tokenId
                tokenId = Guid.NewGuid().ToString();
                // Update refresh token 
                refreshTokenDto.TokenId = tokenId;
                // refreshTokenDto.RefreshTokenId = new JwtUtils().GenerateRefreshToken();
                refreshTokenDto.RefreshCount += 1;
                
                // Progress update
                var updateResult = await _refreshTokenService.UpdateAsync(refreshTokenDto.Id, refreshTokenDto);
                if (updateResult.ResultCode == ResultCodeConst.SYS_Success0003) // Update success
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
                	
                	return new ServiceResult(ResultCodeConst.Auth_Success0008, 
                		await _msgService.GetMessageAsync(ResultCodeConst.Auth_Success0008),
                		new AuthenticateResultDto
                			{
                				AccessToken = generateResult.AccessToken,
                				RefreshToken = refreshTokenDto.RefreshTokenId,
                				ValidTo = generateResult.ValidTo
                			});
                }
            }
            
            // Resp not found 
            return new ServiceResult(ResultCodeConst.Auth_Warning0002,
                await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0002));
		}
		
		public async Task<IServiceResult> GetCurrentUserAsync(string email)
		{
			try
			{
				// Concurrently query both User and Employee services
				var userResult = await _userService.GetByEmailAsync(email);
				var employeeResult = await _employeeService.GetByEmailAsync(email);
				if (userResult.Data == null && employeeResult.Data == null) // Not exist
				{
					var errorMSg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errorMSg, "email"));
				}
				
				// Add user/employee detail to authenticate user
				if (userResult.ResultCode == ResultCodeConst.SYS_Success0002 
				    && userResult.Data is UserDto userDto)
				{
					// Ignore fields
					userDto.PasswordHash = null;
					userDto.TwoFactorSecretKey = null;
					userDto.TwoFactorBackupCodes = null;
					userDto.PhoneVerificationCode = null;
					userDto.PhoneVerificationExpiry = null;
					userDto.PasswordHash = null;
					userDto.RoleId = 0;
					userDto.Role.RoleId = 0;
					
					return new ServiceResult(ResultCodeConst.SYS_Success0002, 
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), userDto);
				} else if (employeeResult.ResultCode == ResultCodeConst.SYS_Success0002
				           && employeeResult.Data is EmployeeDto employeeDto)
				{
					// Ignore fields
					employeeDto.PasswordHash = null;
					employeeDto.TwoFactorSecretKey = null;
					employeeDto.TwoFactorBackupCodes = null;
					employeeDto.PhoneVerificationCode = null;
					employeeDto.PhoneVerificationExpiry = null;
					employeeDto.PasswordHash = null;
					employeeDto.RoleId = 0;
					employeeDto.Role.RoleId = 0;
					
					return new ServiceResult(ResultCodeConst.SYS_Success0002, 
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), employeeDto);
				}

				return new ServiceResult(ResultCodeConst.SYS_Fail0002,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress get current user");
			}
		}

		public async Task<IServiceResult> EnableMfaAsync(string email)
		{
			try
			{
				// Initialize authenticate user 
				AuthenticateUserDto? authUser = null;
				
				// Check whether the email belongs to (User/Employee)
				var userResult = await _userService.GetByEmailAsync(email);
				var employeeResult = await _employeeService.GetByEmailAsync(email);
				
				if (userResult.Data == null &&
				    employeeResult.Data == null) // Not found email match any type of user
				{
					var message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
						StringUtils.Format(message, "email"));
				}else if (userResult.Data is UserDto userDto) // Detect email as user
				{
					// Map to authenticate user
					authUser = userDto.ToAuthenticateUserDto();
				}else if (employeeResult.Data is EmployeeDto employeeDto) // Detect email as employee
				{
					// Map to authenticate user
					authUser = employeeDto.ToAuthenticateUserDto();
				}
				
				// Handle enable MFA
				if (authUser != null)
				{
					// Check whether user or employee already enable MFA
					if (authUser.TwoFactorEnabled) // Already enabled
					{
						return new ServiceResult(ResultCodeConst.Auth_Warning0009, 
							await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0009));
					}
					
					// Generate secret key
					var secretKey = TwoFactorAuthUtils.GenerateSecretKey();
					// Generate backup codes
					var backupCodes = TwoFactorAuthUtils.GenerateBackupCodes();
					// Generate QrCode URI
					var uri = TwoFactorAuthUtils.GenerateQrCodeUri(email, secretKey, "ELibrarySystem");
					// Generate QrCode bytes from URI
					var qrCode = TwoFactorAuthUtils.GenerateQrCode(uri);
					// Hash backup codes
					var hashedBackupCodes = TwoFactorAuthUtils.EncryptBackupCodes(backupCodes, _appSettings);
					
					IServiceResult? enableResult;
					// Stored secret key and backup codes
					if (authUser.IsEmployee) enableResult = await _employeeService.UpdateMfaSecretAndBackupAsync(email, secretKey, hashedBackupCodes);
					else enableResult = await _userService.UpdateMfaSecretAndBackupAsync(email, secretKey, hashedBackupCodes);
					
					// Check whether enable success
					if (enableResult.Data is true)
					{
						return new ServiceResult(ResultCodeConst.SYS_Success0001, 
							await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001),
							new EnableMfaResultDto()
							{
								QrCodeImage = $"data:image/png;base64, {Convert.ToBase64String(qrCode)}",
								BackupCodes = backupCodes,
							});
					}
					
					// Fail to update
					return new ServiceResult(ResultCodeConst.SYS_Fail0003, 
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
				}
				
				// Unknown error
                return new ServiceResult(ResultCodeConst.SYS_Fail0002, 
                	await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress enable mfa");
			}
		}
		
		public async Task<IServiceResult> ValidateMfaAsync(string email, string otp)
		{
			try
			{
				// Initialize authenticate user 
				AuthenticateUserDto? authUser = null;
				
				// Check whether the email belongs to (User/Employee)
				var userResult = await _userService.GetByEmailAsync(email);
				var employeeResult = await _employeeService.GetByEmailAsync(email);
				
				if (userResult.Data == null &&
				    employeeResult.Data == null) // Not found email match any type of user
				{
					var message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
						StringUtils.Format(message, "email"));
				}else if (userResult.Data is UserDto userDto) // Detect email as user
				{
					// Map to authenticate user
					authUser = userDto.ToAuthenticateUserDto();
					// Assign role name for JWT claims
					authUser.RoleName = userDto.Role.EnglishName;
				}else if (employeeResult.Data is EmployeeDto employeeDto) // Detect email as employee
				{
					// Map to authenticate user
					authUser = employeeDto.ToAuthenticateUserDto();
					// Assign role name for JWT claims
                    authUser.RoleName = employeeDto.Role.EnglishName;
				}
				
				// Handle validate MFA
				if (authUser != null)
				{
					// Check whether user or employee already enable MFA
					if (string.IsNullOrEmpty(authUser.TwoFactorSecretKey)) // Not enabled yet
					{
						return new ServiceResult(ResultCodeConst.Auth_Warning0011, 
							await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0011));
					}
					
					// Verify OTP base on user MFA secret 
					var isValid = TwoFactorAuthUtils.VerifyOtp(authUser.TwoFactorSecretKey ?? string.Empty, otp);

					if (isValid)
					{
						// Update MFA status (if not yet)
						if (authUser.IsEmployee && !authUser.TwoFactorEnabled) await _employeeService.UpdateMfaStatusAsync(authUser.Id);
						else if(!authUser.IsEmployee && !authUser.TwoFactorEnabled) await _userService.UpdateMfaStatusAsync(authUser.Id);

						// Authenticate user
						return await AuthenticateUserAsync(authUser);
					}
					
					// Invalid OTP 
					return new ServiceResult(ResultCodeConst.Auth_Warning0005,
						await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0005));
				}
				
				// Unknown error
                return new ServiceResult(ResultCodeConst.SYS_Fail0002, 
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress validate mfa");
			}
		}
		
		public async Task<IServiceResult> ValidateMfaBackupCodeAsync(string email, string backupCode)
		
		{
			try
			{
				// Initialize authenticate user 
				AuthenticateUserDto? authUser = null;
				
				// Check whether the email belongs to (User/Employee)
				var userResult = await _userService.GetByEmailAsync(email);
				var employeeResult = await _employeeService.GetByEmailAsync(email);
				
				if (userResult.Data == null &&
				    employeeResult.Data == null) // Not found email match any type of user
				{
					var message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
						StringUtils.Format(message, "email"));
				}else if (userResult.Data is UserDto userDto) // Detect email as user
				{
					// Map to authenticate user
					authUser = userDto.ToAuthenticateUserDto();
					// Assign role name for JWT generator
					authUser.RoleName = userDto.Role.EnglishName;
				}else if (employeeResult.Data is EmployeeDto employeeDto) // Detect email as employee
				{
					// Map to authenticate user
					authUser = employeeDto.ToAuthenticateUserDto();
					// Assign role name for JWT generator
					authUser.RoleName = employeeDto.Role.EnglishName;
				}
				
				// Handle validate MFA
				if (authUser != null)
				{
					// Check whether user or employee already enable MFA
					if (string.IsNullOrEmpty(authUser.TwoFactorSecretKey)
					    || !authUser.TwoFactorEnabled) // Not enabled yet
					{
						return new ServiceResult(ResultCodeConst.Auth_Warning0011,
							await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0011));
					}

					// Mark as all backup codes of user have been used
					if (string.IsNullOrEmpty(authUser.TwoFactorBackupCodes)) 
					{
						return new ServiceResult(ResultCodeConst.Auth_Warning0012,
							await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0012));
					}
					
					// Split user backup code by comma
					var hashedCodes = authUser.TwoFactorBackupCodes.Split(',');
					
					// Verify Backup Code
					var matchingHash = TwoFactorAuthUtils.VerifyBackupCodeAndGetMatch(backupCode, hashedCodes, _appSettings);
					if (matchingHash != null)
					{
						// Remove the matching hash 
						hashedCodes = hashedCodes.Where(hash => hash != matchingHash).ToArray();

						// Update backup codes
						authUser.TwoFactorBackupCodes = string.Join(",", hashedCodes);
						
						if (authUser.IsEmployee) // Is employee
						{
							await _employeeService.UpdateMfaSecretAndBackupAsync(
								authUser.Email, authUser.TwoFactorSecretKey, hashedCodes);
						}
						else // Is user
						{
							await _userService.UpdateMfaSecretAndBackupAsync(
								authUser.Email, authUser.TwoFactorSecretKey, hashedCodes);
						}
						
						// Authenticate user
						return await AuthenticateUserAsync(authUser);
					}
					
					// Invalid backup code
					return new ServiceResult(ResultCodeConst.Auth_Warning0012, 
						await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0012));
				}

				// Unknown error
				return new ServiceResult(ResultCodeConst.SYS_Fail0002, 
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process validate backup code");
			}
		}

		public async Task<IServiceResult> RegenerateMfaBackupCodeAsync(string email)
		{
			try
			{
				// Initialize authenticate user 
				AuthenticateUserDto? authUser = null;
				
				// Check whether the email belongs to (User/Employee)
				var userResult = await _userService.GetByEmailAsync(email);
				var employeeResult = await _employeeService.GetByEmailAsync(email);
				
				if (userResult.Data == null &&
				    employeeResult.Data == null) // Not found email match any type of user
				{
					var message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
						StringUtils.Format(message, "email"));
				}else if (userResult.Data is UserDto userDto) // Detect email as user
				{
					// Map to authenticate user
					authUser = userDto.ToAuthenticateUserDto();
				}else if (employeeResult.Data is EmployeeDto employeeDto) // Detect email as employee
				{
					// Map to authenticate user
					authUser = employeeDto.ToAuthenticateUserDto();
				}
				
				// Handle regenerate backup code
				if (authUser != null)
				{
					// Check whether user or employee already enable MFA
					if (string.IsNullOrEmpty(authUser.TwoFactorSecretKey) 
					    || !authUser.TwoFactorEnabled) // Not enabled yet
					{
						return new ServiceResult(ResultCodeConst.Auth_Warning0011,
							await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0011));
					}
					
					// Sending email & generate confirm token
					// Generate confirmation code
					var otpCode = StringUtils.GenerateUniqueCode();
					// Email subject
					var emailSubject = "ELibrary - Xác nhận yêu cầu tạo lại mã";
					// Email content
					var emailContent = $@"
					    <div style='font-family: Arial, sans-serif; color: #333; line-height: 1.6;'>
					        <h3>Chào {authUser.FirstName} {authUser.LastName},</h3>
					        <p>Đây là mã xác nhận của bạn:</p>
					        <h1 style='font-weight: bold; color: #2C3E50;'>{otpCode}</h1>
					        <p>Vui lòng sử dụng mã này để hoàn thành quá trình tạo lại mã dự phòng.</p>
					        <br />
					        <p style='font-size: 16px;'>Cảm ơn,</p>
					        <p style='font-size: 16px;'>{_appSettings.LibraryName}</p>
					    </div>";

					
					// Generate MFA backup token
					var token = await new JwtUtils(_webTokenSettings).GenerateMfaTokenAsync(authUser);
					
					var isOtpSent = await SendAndSaveOtpAsync(otpCode, authUser, emailSubject, emailContent);
					if (isOtpSent) // Email sent
					{
						return new ServiceResult(ResultCodeConst.Auth_Success0005,
							await _msgService.GetMessageAsync(ResultCodeConst.Auth_Success0005), 
								new RegenerateMfaBackupResultDto()
								{
									Token = token
								});
					}
					else
					{
						return new ServiceResult(ResultCodeConst.Auth_Fail0002,
							await _msgService.GetMessageAsync(ResultCodeConst.Auth_Fail0002));
					}
				}
				
				// Unknown error
				return new ServiceResult(ResultCodeConst.SYS_Fail0002, 
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke while regenerating backup code");
			}
		}

		public async Task<IServiceResult> ConfirmRegenerateMfaBackupCodeAsync(string email, string otp, string token)
		{
			try
			{
				// Initialize authenticate user 
				AuthenticateUserDto? authUser = null;
				
				// Check whether the email belongs to (User/Employee)
				var userResult = await _userService.GetByEmailAsync(email);
				var employeeResult = await _employeeService.GetByEmailAsync(email);
				
				if (userResult.Data == null &&
				    employeeResult.Data == null) // Not found email match any type of user
				{
					var message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
						StringUtils.Format(message, "email"));
				}else if (userResult.Data is UserDto userDto) // Detect email as user
				{
					// Map to authenticate user
					authUser = userDto.ToAuthenticateUserDto();
				}else if (employeeResult.Data is EmployeeDto employeeDto) // Detect email as employee
				{
					// Map to authenticate user
					authUser = employeeDto.ToAuthenticateUserDto();
				}
				
				// Verify OTP 
				if (authUser != null 
				    && authUser.EmailVerificationCode == otp) // Is valid OTP
				{
					// Check whether user or employee already enable MFA
					if (string.IsNullOrEmpty(authUser.TwoFactorSecretKey) 
					    || !authUser.TwoFactorEnabled) // Not enabled yet
					{
						return new ServiceResult(ResultCodeConst.Auth_Warning0011,
							await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0011));
					}
					
					// Validate Mfa token 
					var jwtToken = await new JwtUtils(_tokenValidationParameters).ValidateMfaTokenAsync(token);
					if (jwtToken == null || authUser.TwoFactorSecretKey == null)
					{
						return new ServiceResult(ResultCodeConst.Auth_Fail0003,
							await _msgService.GetMessageAsync(ResultCodeConst.Auth_Fail0003));
					}
					
					// Generate new backup codes
					// Generate backup codes
					var backupCodes = TwoFactorAuthUtils.GenerateBackupCodes();
					// Hash backup codes
					var hashedBackupCodes = TwoFactorAuthUtils.EncryptBackupCodes(backupCodes, _appSettings);

					IServiceResult? enableResult;
					// Stored secret key and backup codes
					if (authUser.IsEmployee) enableResult = await _employeeService.UpdateMfaSecretAndBackupAsync(email, authUser.TwoFactorSecretKey, hashedBackupCodes);
					else enableResult = await _userService.UpdateMfaSecretAndBackupAsync(email, authUser.TwoFactorSecretKey, hashedBackupCodes);

					if (enableResult.Data is true)
					{
						// Create MFA backup codes message
						return new ServiceResult(ResultCodeConst.Auth_Success0010,
							await _msgService.GetMessageAsync(ResultCodeConst.Auth_Success0010));
					}
					
					// Fail to regenerate MFA backup codes
					return new ServiceResult(ResultCodeConst.Auth_Fail0003,
						await _msgService.GetMessageAsync(ResultCodeConst.Auth_Fail0003));
				}
				
				// Not match OTP
				return new ServiceResult(ResultCodeConst.Auth_Warning0005, 
					await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0005));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke while process confirm regenerate MFA backup code");
			}
		}

		public async Task<IServiceResult> GetMfaBackupAsync(string email)
		{
			try
			{
				// Initialize authenticate user 
				AuthenticateUserDto? authUser = null;
				
				// Check whether the email belongs to (User/Employee)
				var userResult = await _userService.GetByEmailAsync(email);
				var employeeResult = await _employeeService.GetByEmailAsync(email);
				
				if (userResult.Data == null &&
				    employeeResult.Data == null) // Not found email match any type of user
				{
					var message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
						StringUtils.Format(message, "email"));
				}else if (userResult.Data is UserDto userDto) // Detect email as user
				{
					// Map to authenticate user
					authUser = userDto.ToAuthenticateUserDto();
				}else if (employeeResult.Data is EmployeeDto employeeDto) // Detect email as employee
				{
					// Map to authenticate user
					authUser = employeeDto.ToAuthenticateUserDto();
				}
				
				// Handle get MFA code
				if (authUser != null)
				{
					// Check whether user or employee already enable MFA
					if (string.IsNullOrEmpty(authUser.TwoFactorSecretKey)
					    || !authUser.TwoFactorEnabled) // Not enabled yet
					{
						return new ServiceResult(ResultCodeConst.Auth_Warning0011,
							await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0011));
					}

					if (string.IsNullOrEmpty(authUser.TwoFactorBackupCodes))
					{
						return new ServiceResult(ResultCodeConst.SYS_Warning0004,
							await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
					}					
					
					// Split user backup code by comma
					var hashedCodes = authUser.TwoFactorBackupCodes.Split(',');
					
					// Verify Backup Code
					var decryptedBackupCodes = TwoFactorAuthUtils.DecryptBackupCodes(hashedCodes, _appSettings);
					if (decryptedBackupCodes.Any())
					{
						return new ServiceResult(ResultCodeConst.SYS_Success0002,
							await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), decryptedBackupCodes);
					}
				}
				
				// Fail to get data 
				return new ServiceResult(ResultCodeConst.SYS_Fail0002,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke while process get MFA backup codes");
			}
		}

		public async Task<IServiceResult> UpdateProfileAsync(AuthenticateUserDto dto)
		{
			try
			{
				if (dto.IsEmployee)
				{
					return await _employeeService.UpdateProfileAsync(dto.Email, dto.ToEmployeeDto());
				}
				else
				{
					return await _userService.UpdateProfileAsync(dto.Email, dto.ToUserDto());
				}
			}
			catch (UnprocessableEntityException)
			{
				throw;
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process update profile");
			}
		}
		
		// Authenticate user
		private async Task<ServiceResult> AuthenticateUserAsync(AuthenticateUserDto? user)
		{
			// Check not exist authenticate user (save fail,...) 
			if (user == null)
				throw new Exception("Unknown error invoke while authenticating user.");
			
			// Validate user status
			if (user.Id == Guid.Empty || string.IsNullOrEmpty(user.RoleName))
			{
				return new ServiceResult(ResultCodeConst.Auth_Warning0007,
					await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0007));
			}

			if (!user.IsActive)
			{
				return new ServiceResult(ResultCodeConst.Auth_Warning0001,
					await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0001));
			}
			
			// Generate token
			var tokenId = Guid.NewGuid().ToString();
			var jwtResponse = await new JwtUtils(_webTokenSettings).GenerateJwtTokenAsync(
				tokenId:tokenId, user:user);
			
			if (string.IsNullOrEmpty(jwtResponse.AccessToken) || jwtResponse.ValidTo <= DateTime.UtcNow)
				throw new Exception("Invalid JWT token generated");
			
			// Handle refresh token
			var refreshTokenDto = await HandleRefreshTokenAsync(user, tokenId);

			return new ServiceResult(
				ResultCodeConst.Auth_Success0002,
				await _msgService.GetMessageAsync(ResultCodeConst.Auth_Success0002),
				new AuthenticateResultDto
				{
					AccessToken = jwtResponse.AccessToken,
					RefreshToken = refreshTokenDto.RefreshTokenId,
					ValidTo = jwtResponse.ValidTo
				});
		}
		
		// Create user 
		private async Task<AuthenticateUserDto?> CreateNewUserAsync(AuthenticateUserDto user, 
			bool createFromExternalProvider = false)
		{
			// Not create from external provider, and not provide password
			if (!createFromExternalProvider && string.IsNullOrEmpty(user.Password)) 
				return null;
					
			// General member role
			var role = (await _roleService.GetByNameAsync(Role.LibraryPatron)).Data as SystemRoleDto;
			if (role == null)
			{
				throw new NotFoundException("GeneralMember role is not found to create new user");
			};
					
			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
			
			// Add UserId
			user.Id = Guid.NewGuid();
			// Update shared fields
			user.CreateDate = currentLocalDateTime;
			user.RoleId = role.RoleId;
			user.RoleName = role.EnglishName;
			// Update security fields based on provider
			user.EmailConfirmed = createFromExternalProvider; // Mark as email confirmed when create with external provider
			user.IsActive = createFromExternalProvider; // Mark as active when create with external provider
			user.PhoneNumberConfirmed = false;
			user.TwoFactorEnabled = false;
			
			// Progress create new user
			var createResult = await _userService.CreateAsync(user.ToUserDto()); 
					
			// Assign ID and set to active if success to mark as authenticate success
			if (createResult.ResultCode == ResultCodeConst.SYS_Success0001)
			{
				return user;
			}

			return null;
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
			var refreshTokenId = await new JwtUtils().GenerateRefreshTokenAsync();

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
			if (result.ResultCode != ResultCodeConst.SYS_Success0001)
			{
				var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001);
				throw new Exception(StringUtils.Format(errMsg, "refresh token"));
			}

			return refreshTokenDto;
		}
		
		// Update existing refresh token
		private async Task<RefreshTokenDto> UpdateExistingRefreshTokenAsync(RefreshTokenDto refreshTokenDto, string tokenId)
		{
			refreshTokenDto.CreateDate = DateTime.UtcNow;
			refreshTokenDto.RefreshTokenId = await new JwtUtils().GenerateRefreshTokenAsync();
			refreshTokenDto.TokenId = tokenId;
			refreshTokenDto.RefreshCount = 0;

			var result = await _refreshTokenService.UpdateAsync(refreshTokenDto.Id, refreshTokenDto);
			if (result.ResultCode != ResultCodeConst.SYS_Success0003)
			{
				throw new Exception(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
			}

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
		
		// Send email (confirmation/OTP)
		private async Task<bool> SendAndSaveOtpAsync(string otpCode, AuthenticateUserDto user,
			string subject, string emailContent)
		{
			try
			{
				// Progress send confirmation email
				var emailMessageDto = new EmailMessageDto( // Define email message
					// Define Recipient
					to: new List<string>() { user.Email },
					// Define subject
					subject: subject,
					// Add email body content
					content: emailContent
				);

				// Send email
				await _emailService.SendEmailAsync(message: emailMessageDto, isBodyHtml: true);
				
				// Progress update email confirmation code to DB (either user, employeee)
				IServiceResult updateResult;
				if (!user.IsEmployee) // Is user
				{
					updateResult = await _userService.UpdateEmailVerificationCodeAsync(user.Id, otpCode);
				}
				else // Is employee
				{
					updateResult = await _employeeService.UpdateEmailVerificationCodeAsync(user.Id, otpCode);
				}
			
				// Save success
				if (updateResult.Data is true) return true;
			}
			catch (Exception ex)
			{
				// Log the exception or handle errors
				_logger.Error("Failed to send confirmation email: {msg}", ex.Message);
			}
			
			return false;
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
				return false;
			}

			return true;
		}

		// Handle get google token response
		private async Task<TokenResponse> ExchangeCodeForAccessTokenAsync(string code)
		{
			// Use GoogleAuthorizationCodeFlow to handle the token exchange
			var tokenRequest = new AuthorizationCodeTokenRequest()
			{
				Code = code,
				ClientId = _googleAuthSettings.ClientId,
				ClientSecret = _googleAuthSettings.ClientSecret,
				RedirectUri = _googleAuthSettings.RedirectUri,
				GrantType = OpenIdConnectGrantTypes.AuthorizationCode,
			};
			
			// Execute the token exchange
			var tokenResponse = await tokenRequest.ExecuteAsync(
				clock: SystemClock.Default,
				httpClient: _httpClient,
				taskCancellationToken: CancellationToken.None, 
				tokenServerUrl: GoogleAuthConsts.OidcTokenUrl);
			
			// Return the token response
			return tokenResponse;
		}
	}
}
