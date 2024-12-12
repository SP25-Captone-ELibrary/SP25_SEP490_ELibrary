using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Roles;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Serilog;
using MimeKit.Encodings;
using OfficeOpenXml.Packaging.Ionic.Zip;
using RoleEnum = FPTU_ELibrary.Domain.Common.Enums.Role;
using Microsoft.AspNetCore.Http;
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
		
		public async Task<IServiceResult> UpdateRoleAsync(int roleId, Guid userId)
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
			}catch(Exception ex)
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
                //query specification
                var baseSpec = new BaseSpecification<User>(u => u.Email.Equals(newUser));
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

                //Define who create this account
                newUser.ModifiedBy = nameof(RoleEnum.Administration);

                //ResultCode of created account
                newUser.IsActive = true;
                newUser.IsDeleted = false;

                //Create password and send email
                var password = Utils.HashUtils.GenerateRandomPassword();

                newUser.PasswordHash = Utils.HashUtils.HashPassword(password);
                newUser.CreateDate = DateTime.Now;
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
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw new Exception("Error invoke when progress create account by admin");
            }
        }

        // public async Task<IServiceResult> SearchAccount(string searchString)
        // {
        //     //query specification
        //     var baseSpec = new BaseSpecification<User>(x => x.UserCode!.Contains(searchString)
        //                                                     || x.Email.Contains(searchString)
        //                                                     || x.FirstName!.Contains(searchString)
        //                                                     || x.LastName!.Contains(searchString)
        //                                                     || x.Phone!.Contains(searchString)
        //     );
        //
        //     var result = await _unitOfWork.Repository<User, Guid>().GetAllWithSpecAsync(baseSpec);
        //     if (!result.Any())
        //         return new ServiceResult(ResultCodeConst.WARNING_NO_DATA_CODE, ResultCodeConst.WARNING_NO_DATA_MSG);
        //
        //     return new ServiceResult(ResultCodeConst.SUCCESS_READ_CODE, ResultCodeConst.SUCCESS_READ_MSG,
        //         _mapper.Map<IEnumerable<UserDto>>(result));
        // }
        public async Task<IServiceResult> GetById(Guid id)
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

        public async Task<IServiceResult> ChangeAccountStatus(Guid userId)
        {
            var currentAccount = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(userId);
            if (currentAccount is null)
            {
                var errorMsg = await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0006);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errorMsg, "account"));
            }

            currentAccount.IsActive = !currentAccount.IsActive;
            var dto = _mapper.Map<UserDto>(currentAccount);
            await UpdateAsync(userId, dto);
            return new ServiceResult(ResultCodeConst.SYS_Success0003,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003),
                _mapper.Map<UserDto>(currentAccount));
        }

        public async Task<IServiceResult> UpdateAccount(Guid userId, UserDto userUpdateDetail, string roleName)
        {
            var currentAccount = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(userId);
            if (currentAccount is null)
            {
                var errorMsg = await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0006);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errorMsg, "account"));
            }

            if (roleName.Equals(nameof(RoleEnum.Administration)))
            {
                if (userUpdateDetail.UserCode!.Trim() == "" || userUpdateDetail.UserCode is null)
                {
                    return new ServiceResult(ResultCodeConst.User_Warning0001,
                        await _msgService.GetMessageAsync(ResultCodeConst.User_Warning0001));
                }

                if (userUpdateDetail.RoleId <= 0 || userUpdateDetail.RoleId == 4 || userUpdateDetail.UserCode is null)
                {
                    return new ServiceResult(ResultCodeConst.User_Warning0002,
                        await _msgService.GetMessageAsync(ResultCodeConst.User_Warning0002));
                }

                currentAccount.UserCode = userUpdateDetail.UserCode;
                currentAccount.RoleId = userUpdateDetail.RoleId;
                var dto = _mapper.Map<UserDto>(currentAccount);
                await UpdateAsync(userId, dto);
            }

            else
            {
                currentAccount.FirstName = userUpdateDetail.FirstName ?? currentAccount.FirstName;
                currentAccount.LastName = userUpdateDetail.LastName ?? currentAccount.LastName;
                currentAccount.Dob = userUpdateDetail.Dob ?? currentAccount.Dob;
                currentAccount.Phone = userUpdateDetail.Phone ?? currentAccount.Phone;
                currentAccount.Avatar = userUpdateDetail.Avatar ?? currentAccount.Avatar;
                var dto = _mapper.Map<UserDto>(currentAccount);
                await UpdateAsync(userId, dto);
            }

            return new ServiceResult(ResultCodeConst.SYS_Success0003, ResultCodeConst.SYS_Success0003,
                _mapper.Map<UserDto>(currentAccount));
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

        public async Task<IServiceResult> DeleteAccount(Guid id)
        {
            await DeleteAsync(id);
            return new ServiceResult(ResultCodeConst.SYS_Success0004, ResultCodeConst.SYS_Success0004);
        }

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


        public Task CreateManyAccountsWithSendEmail(IFormFile excelFile)
        {
            return Task.Run(async () =>
            {
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

                    using (var scope = _service.CreateScope())
                    {
                        var rolervice = scope.ServiceProvider.GetRequiredService<ISystemRoleService<SystemRoleDto>>();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger>();
                        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                        var genericService =
                            scope.ServiceProvider.GetRequiredService<IGenericService<User, UserDto, Guid>>();
                        try
                        {
                            var result = await rolervice.GetByNameAsync(RoleEnum.GeneralMember);
                            if (result.ResultCode != ResultCodeConst.SYS_Success0002)
                            {
                                logger.Error("Not found any role with nameof General user");
                                throw new NotFoundException("Role", "General user");
                            }

                            var users = new List<(string Email, string FirstName, string LastName)>();
                            var failedEmails = new List<string>();
                            var emailToSend = new List<EmailMessageDto>();

                            // Đọc và xử lý file Excel
                            using (var package = new OfficeOpenXml.ExcelPackage(memoryStream))
                            {
                                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                                if (worksheet == null)
                                {
                                    throw new BadRequestException("Excel file does not contain any worksheet");
                                }

                                int rowCount = worksheet.Dimension.Rows;

                                for (int row = 2; row <= rowCount; row++)
                                {
                                    var email = worksheet.Cells[row, 1].Text;
                                    var firstName = worksheet.Cells[row, 2].Text;
                                    var lastName = worksheet.Cells[row, 3].Text;

                                    if (Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                                    {
                                        users.Add((email, firstName, lastName));
                                    }
                                    else
                                    {
                                        failedEmails.Add(email);
                                    }
                                }

                                foreach (var user in users)
                                {
                                    var existingUser = await unitOfWork.Repository<User, Guid>()
                                        .GetWithSpecAsync(new BaseSpecification<User>(u => u.Email == user.Email));

                                    if (existingUser != null)
                                    {
                                        failedEmails.Add(user.Email);
                                        continue;
                                    }

                                    var password = Utils.HashUtils.GenerateRandomPassword();
                                    var role = (SystemRoleDto)result.Data!;
                                    var newUser = new UserDto
                                    {
                                        Email = user.Email,
                                        PasswordHash = Utils.HashUtils.HashPassword(password),
                                        FirstName = user.FirstName,
                                        LastName = user.LastName,
                                        CreateDate = DateTime.Now,
                                        RoleId = role.RoleId,
                                        IsDeleted = false
                                    };

                                    await genericService.CreateAsync(newUser);

                                    var emailMessageDto = new EmailMessageDto(
                                        new List<string> { newUser.Email },
                                        "ELibrary - Change password notification",
                                        $@"
                            <h3>Hi {newUser.FirstName} {newUser.LastName},</h3>
                            <p>Your account has been created. Your password is:</p>
                            <h1>{password}</h1>");

                                    emailToSend.Add(emailMessageDto);
                                }

                                foreach (var emailMessage in emailToSend)
                                {
                                    try
                                    {
                                        await emailService.SendEmailAsync(emailMessage, true);
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.Error(ex, "Failed to send email to {Email}",
                                            emailMessage.To.FirstOrDefault());
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, "An error occurred while processing accounts");
                        }
                    }
                }
            });
        }
    }
}