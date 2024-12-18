using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Roles;
using FPTU_ELibrary.Application.Exceptions;
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
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Serilog;
using RoleEnum = FPTU_ELibrary.Domain.Common.Enums.Role;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace FPTU_ELibrary.Application.Services
{
    public class UserService : GenericService<User, UserDto, Guid>, IUserService<UserDto>
    {
        private readonly ISystemRoleService<SystemRoleDto> _roleService;
        private readonly IEmailService _emailService;
        private readonly IServiceProvider _service;

        public UserService(
            ILogger logger,
            ISystemMessageService msgService,
            ISystemRoleService<SystemRoleDto> roleService,
            IEmailService emailService,
            IUnitOfWork unitOfWork,
            IMapper mapper, IServiceProvider service) // to get the service and not depend on http lifecycle) 
            : base(msgService, unitOfWork, mapper, logger)
        {
            _roleService = roleService;
            _emailService = emailService;
            _service = service;
        }

        public override async Task<IServiceResult> GetByIdAsync(Guid id)
        {
            //query specification
            var baseSpec = new BaseSpecification<User>(u => u.UserId.Equals(id));
            // Include job role
            baseSpec.ApplyInclude(u => u.Include(u => u.Role));
            // Get user by query specification
            var existedUser = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(baseSpec);

            if (existedUser is null)
            {
                var errorMsg = await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0006);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errorMsg, "account"));
            }

            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), existedUser);
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
                baseSpec.ApplyInclude(q =>
                    q.Include(u => u.Role));

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

        public async Task<IServiceResult> CreateAccountByAdmin(UserDto newUser)
        {
            try
            {
	            // Validate inputs using the generic validator
	            var validationResult = await ValidatorExtensions.ValidateAsync(newUser);
	            // Check for valid validations
	            if (validationResult != null && !validationResult.IsValid)
	            {
		            // Convert ValidationResult to ValidationProblemsDetails.Errors
		            var errors = validationResult.ToProblemDetails().Errors;
		            throw new UnprocessableEntityException("Invalid Validations", errors);
	            }
	            
                //query specification
                var baseSpec = new BaseSpecification<User>(u => u.Email.Equals(newUser.Email));
                // Include job role
                baseSpec.ApplyInclude(u => u.Include(u => u.Role));

                // Get user by query specification
                var existedUser = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(baseSpec);
                if (existedUser is not null)
                {
                    return new ServiceResult(ResultCodeConst.Auth_Warning0006,
                        await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0006));
                }

                var result = await _roleService.GetByNameAsync(RoleEnum.GeneralMember);
                if (result.ResultCode == ResultCodeConst.SYS_Success0002)
                {
                    // Assign role
                    newUser.RoleId = (result.Data as SystemRoleDto)!.RoleId;
                }
                else
                {
                    var errorMsg = await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0006);
                    return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                        StringUtils.Format(errorMsg, "role"));
                }

                //Create password and send email
                var password = HashUtils.GenerateRandomPassword();
                newUser.PasswordHash = HashUtils.HashPassword(password);
                
                // Progress create new user
                var createdResult = await CreateAsync(newUser);

                if (createdResult.Data is true)
                {
                    #region Old User Sending Email handler

                    //progress send current password for email
                    //           // Define email message
                    //           var emailMessageDto = new EmailMessageDto(
                    //               // Define Recipient
                    //               to: new List<string>() { newUser.Email },
                    //               // Define subject
                    //               // Add email body content
                    //               subject: "ELibrary - Change password notification",
                    //               // Add email body content
                    //               content: $@"
                    // <div style='font-family: Arial, sans-serif; color: #333; line-height: 1.6;'>
                    // 	<h3>Hi {newUser.FirstName} {newUser.LastName},</h3>
                    // 	<p> ELibrary has created account with your email and here is your password:</p>
                    // 	<h1 style='font-weight: bold; color: #2C3E50;'>{password}</h1>
                    // 	<p> Please login and change the password as soon as posible.</p>
                    // 	<br />
                    // 	<p style='font-size: 16px;'>Thanks,</p>
                    // <p style='font-size: 16px;'>The ELibrary Team</p>
                    // </div>"
                    //           );
                    //           // Send email
                    //           var isEmailSent = await _emailService.SendEmailAsync(message: emailMessageDto, isBodyHtml: true);

                    #endregion

                    await SendUserEmail(newUser, password);
                    return new ServiceResult(ResultCodeConst.SYS_Success0001,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001));
                }
                else
                {
                    return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
                }
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
        		// Check exist user
        		var existingEntity = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(userId);
        		if (existingEntity == null)
        		{
        			var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
        			return new ServiceResult(ResultCodeConst.SYS_Warning0002,
        				StringUtils.Format(errMsg, "user"));
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
        
        public async Task<IServiceResult> UpdateMfaSecretAndBackupAsync(string email, string mfaKey, IEnumerable<string> backupCodes)
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

        public async Task<IServiceResult> SoftDeleteAsync(Guid userId)
        {
            try
            {
            	// Check exist user
            	var existingEntity = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(userId);
            	// Check if user account already mark as deleted
            	if (existingEntity == null || existingEntity.IsDeleted)
            	{
            		var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
            		return new ServiceResult(ResultCodeConst.SYS_Warning0002,
            			StringUtils.Format(errMsg, "user"));
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
				// Check exist user
				var existingEntity = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(userId);
				// Check if user account already mark as deleted
				if (existingEntity == null || !existingEntity.IsDeleted)
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errMsg, "user"));
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
        
        #region Temporary return. Offical return required worker to send many emails at the time.

        // public async Task<IServiceResult> CreateManyAccountsByAdmin(IFormFile excelFile)
        // {
        //     if (excelFile == null || excelFile.Length == 0)
        //         throw new BadRequestException("File is empty or null");
        //
        //     List<string> emails = new List<string>();
        //
        //     //Read email from sheet 1
        //     using (var stream = excelFile.OpenReadStream())
        //     {
        //         using (var package = new OfficeOpenXml.ExcelPackage(stream))
        //         {
        //             var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        //             if (worksheet == null)
        //                 throw new BadRequestException("Excel file does not contain any worksheet");
        //
        //             int rowCount = worksheet.Dimension.Rows;
        //
        //             // Email begins from row 2 and lays in first column
        //             for (int row = 2; row <= rowCount; row++)
        //             {
        //                 var email = worksheet.Cells[row, 1].Text;
        //                 if (!string.IsNullOrWhiteSpace(email))
        //                     emails.Add(email);
        //             }
        //         }
        //     }
        //
        //     if (!emails.Any())
        //         throw new BadRequestException("No valid emails found in the Excel file");
        //
        //     var result = await _roleService.GetByNameAsync(Role.GeneralMember);
        //     if (result.ResultCode != ResultCodeConst.SUCCESS_READ_CODE)
        //     {
        //         _logger.Error("Not found any role with nameof General user");
        //         throw new NotFoundException("Role", "General user");
        //     }
        //
        //     // Process Create new account
        //     List<string> failedEmails = new List<string>();
        //     Dictionary<string, string> newAccounts = new Dictionary<string, string>();
        //
        //     foreach (var email in emails)
        //     {
        //         // Check if email has been used or not
        //         var baseSpec = new BaseSpecification<User>(u => u.Email.Equals(email));
        //         baseSpec.AddInclude(u => u.Role);
        //
        //         var existedUser = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(baseSpec);
        //
        //         if (existedUser is not null)
        //         {
        //             failedEmails.Add(email);
        //             continue;
        //         }
        //
        //         // Create new account with given email
        //         var password = Utils.HashUtils.GenerateRandomPassword();
        //         var newUser = new UserDto
        //         {
        //             Email = email,
        //             RoleId = (result.Data as SystemRoleDto)!.RoleId,
        //             PasswordHash = Utils.HashUtils.HashPassword(password),
        //             CreateDate = DateTime.Now,
        //         };
        //
        //         await CreateAsync(newUser);
        //         newAccounts.Add(email, password);
        //     }

        // Return Excel File 
        // using (var package = new OfficeOpenXml.ExcelPackage())
        // {
        //     // Sheet 1: New Accounts
        //     var sheet1 = package.Workbook.Worksheets.Add("New Accounts");
        //     sheet1.Cells[1, 1].Value = "Email";
        //     sheet1.Cells[1, 2].Value = "Password";
        //
        //     int newRow = 2;
        //     foreach (var account in newAccounts)
        //     {
        //         sheet1.Cells[newRow, 1].Value = account.Key;
        //         sheet1.Cells[newRow, 2].Value = account.Value;
        //         newRow++;
        //     }
        //
        //     // Sheet 2: Existed Emails
        //     var sheet2 = package.Workbook.Worksheets.Add("Existed Emails");
        //     sheet2.Cells[1, 1].Value = "Existed Email";
        //
        //     int existedRow = 2;
        //     foreach (var email in failedEmails)
        //     {
        //         sheet2.Cells[existedRow, 1].Value = email;
        //         existedRow++;
        //     }

        // return with return file  
        // return new ServiceResult(ResultCodeConst.SUCCESS_UPDATE_CODE, ResultCodeConst.SUCCESS_UPDATE_MSG,package.GetAsByteArray());
        //     return new ServiceResult(ResultCodeConst.SUCCESS_UPDATE_CODE, ResultCodeConst.SUCCESS_UPDATE_MSG
        //     );
        // }

        #endregion

        //  #region User Own Sending Email and format function
        private async Task SendUserEmail(UserDto newUser, string rawPassword)
        {
            var emailMessageDto = new EmailMessageDto(
                // Define Recipient
                to: new List<string>() { newUser.Email },
                // Define subject
                // Add email body content
                subject: "ELibrary - Change password notification",
                // Add email body content
                content: $@"
						<div style='font-family: Arial, sans-serif; color: #333; line-height: 1.6;'>
							<h3>Hi {newUser.FirstName} {newUser.LastName},</h3>
							<p> ELibrary has created account with your email and here is your password:</p>
							<h1 style='font-weight: bold; color: #2C3E50;'>{rawPassword}</h1>
							<p> Please login and change the password as soon as posible.</p>
							<br />
							<p style='font-size: 16px;'>Thanks,</p>
						<p style='font-size: 16px;'>The ELibrary Team</p>
						</div>"
            );
            // Send email
            var isEmailSent = await _emailService.SendEmailAsync(message: emailMessageDto, isBodyHtml: true);
        }

        public async Task<IServiceResult> CreateManyAccountsWithSendEmail(string email, IFormFile? excelFile,
            DuplicateHandle duplicateHandle)
        {
            // Query specification
            var baseSpec = new BaseSpecification<User>(u => u.Email.Equals(email));
            // Include job role
            baseSpec.ApplyInclude(q =>
                q.Include(u => u.Role));
            var user = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(baseSpec);
            if (user is null)
            {
                return new ServiceResult(ResultCodeConst.Auth_Warning0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0002));
            }
            else if (user.RoleId != 1)
            {
                return new ServiceResult(ResultCodeConst.Auth_Warning0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0001));
            }

            _ = Task.Run(async () =>
            {
                var scope = _service.CreateScope();
                var rolerService = scope.ServiceProvider.GetRequiredService<ISystemRoleService<SystemRoleDto>>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var genericService =
                    scope.ServiceProvider.GetRequiredService<IGenericService<User, UserDto, Guid>>();
                var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<AccountHub>>();

                if (excelFile == null || excelFile.Length == 0)
                {
                    throw new BadRequestException("File is empty or null");
                }

                // Validate file format (e.g., only allow .xlsx files)
                var allowedExtensions = new[] { ".xlsx" };
                var fileExtension = Path.GetExtension(excelFile.FileName);
                if (!allowedExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
                {
                    throw new BadRequestException("Invalid file type. Only .xlsx files are allowed.");
                }

                using (var memoryStream = new MemoryStream())
                {
                    await excelFile.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    try
                    {
                        var result = await rolerService.GetByNameAsync(RoleEnum.GeneralMember);
                        if (result.ResultCode != ResultCodeConst.SYS_Success0002)
                        {
                            logger.Error("Not found any role with nameof General user");
                            throw new NotFoundException("Role", "General user");
                        }

                        var failedMsgs = new List<UserFailedMessage>();

                        // Đọc và xử lý file Excel
                        using (var package = new OfficeOpenXml.ExcelPackage(memoryStream))
                        {
                            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                            if (worksheet == null)
                            {
                                throw new BadRequestException("Excel file does not contain any worksheet");
                            }

                            int rowCount = worksheet.Dimension.Rows;

                            // Tập hợp lưu các email đã xử lý
                            var processedEmails = new Dictionary<string, int>();
                            // dictionary for email-password
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

                                // check the exist record if it has been add
                                if (processedEmails.ContainsKey(userRecord.Email))
                                {
                                    if (duplicateHandle.ToString().ToLower() == "skip")
                                    {
                                        //  skip the current record
                                        continue;
                                    }
                                    else if (duplicateHandle.ToString().ToLower() == "replace")
                                    {
                                        // Remove old record + failed msg of old msg
                                        failedMsgs.RemoveAll(f => f.Row == processedEmails[userRecord.Email]);
                                        processedEmails[userRecord.Email] = row;
                                    }
                                }
                                else
                                {
                                    processedEmails[userRecord.Email] = row;
                                }

                                // Error found
                                var rowErr = await DetectWrongRecord(userRecord, unitOfWork);
                                if (rowErr.Count != 0)
                                {
                                    failedMsgs.Add(new UserFailedMessage()
                                    {
                                        Row = row,
                                        ErrMsg =rowErr 
                                    });
                                }
                                else
                                {
                                    var newUser = userRecord.ToUserDto();
                                    var existingUser = await unitOfWork.Repository<User, Guid>()
                                        .GetWithSpecAsync(new BaseSpecification<User>(u => u.Email == user.Email));
                                    var password = Utils.HashUtils.GenerateRandomPassword();
                                    var role = (SystemRoleDto)result.Data!;
                                    newUser.RoleId = role.RoleId;
                                    newUser.PasswordHash = password;
                                    userToAdd.Add(newUser);
                                    emailPasswords.Add(newUser.Email, password);
                                }
                            }

                            if (failedMsgs.Count > 0)
                            {
                                // Gửi thông báo lỗi qua Hub
                                await hubContext.Clients.User(email).SendAsync("ReceiveFailErrorMessage", failedMsgs);
                            }

                            // add available users
                            await unitOfWork.Repository<User, Guid>().AddRangeAsync(mapper.Map<List<User>>(userToAdd));
                            await unitOfWork.SaveChangesAsync();
                            //send email to users
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
            });
            return new ServiceResult(ResultCodeConst.SYS_Success0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001));
        }

        private async Task<List<string>> DetectWrongRecord(UserExcelRecord record, IUnitOfWork unitOfWork)
        {
            var errMsgs = new List<string>();
            //check validation

            //for DOB
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

            //email
            if (!Regex.IsMatch(record.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                errMsgs.Add("Email is not valid");
            }

            if (!Regex.IsMatch(record.FirstName, @"^([A-ZÀ-Ỵ][a-zà-ỵ]*)(\s[A-ZÀ-Ỵ][a-zà-ỵ]*)*$"))
            {
                errMsgs.Add("First name is not valid");
            }

            if (!Regex.IsMatch(record.LastName, @"^([A-ZÀ-Ỵ][a-zà-ỵ]*)(\s[A-ZÀ-Ỵ][a-zà-ỵ]*)*$"))
            {
                errMsgs.Add("Last name is not valid");
            }

            if (record.Phone is not null && !Regex.IsMatch(record.Phone, @"^0\d{9,10}$"))
            {
                errMsgs.Add("Phone is not valid");
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