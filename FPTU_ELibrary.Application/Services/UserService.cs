using System.Drawing;
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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MimeKit.Encodings;

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

        public async Task<IServiceResult> CreateAccountByAdmin(UserDto newUser)
        {
            //query specification
            var baseSpec = new BaseSpecification<User>(u => u.Email.Equals(newUser));
            // Include job role
            baseSpec.AddInclude(u => u.Role);

            // Get user by query specification
            var existedUser = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(baseSpec);

            if (existedUser is not null) throw new BadRequestException("This email has been used");

            var result = await _roleService.GetByNameAsync(Role.GeneralMember);
            if (result.Status == ResultConst.SUCCESS_READ_CODE)
            {
                // Assign role
                newUser.RoleId = (result.Data as SystemRoleDto)!.RoleId;
            }
            else
            {
                _logger.LogError("Not found any role with nameof General user");
                throw new NotFoundException("Role", "General user");
            }

            //Define who create this account
            newUser.ModifiedBy = "Administration";

            //Status of created account
            newUser.IsActive = true;

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
                return new ServiceResult(ResultConst.SUCCESS_INSERT_CODE, ResultConst.SUCCESS_INSERT_MSG);
            }
            else
            {
                _logger.LogError("Something went wrong, fail to create new user");
                throw new Exception("Something went wrong, fail to create new use");
            }
        }

        public async Task<IServiceResult> SearchAccount(string searchString)
        {
            //query specification
            var baseSpec = new BaseSpecification<User>(x => x.UserCode!.Contains(searchString)
                                                            || x.Email.Contains(searchString)
                                                            || x.FirstName!.Contains(searchString)
                                                            || x.LastName!.Contains(searchString)
                                                            || x.Phone!.Contains(searchString)
            );

            var result = await _unitOfWork.Repository<User, Guid>().GetAllWithSpecAsync(baseSpec);
            if (!result.Any())
                return new ServiceResult(ResultConst.WARNING_NO_DATA_CODE, ResultConst.WARNING_NO_DATA_MSG);
            return new ServiceResult(ResultConst.SUCCESS_READ_CODE, ResultConst.SUCCESS_READ_MSG,
                _mapper.Map<IEnumerable<UserDto>>(result));
        }

        public async Task<IServiceResult> ChangeAccountStatus(Guid userId)
        {
            var currentAccount = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(userId);
            if (currentAccount is null)
                return new ServiceResult(ResultConst.WARNING_NO_DATA_CODE, ResultConst.WARNING_NO_DATA_MSG);
            currentAccount.IsActive = !currentAccount.IsActive;
            var dto = _mapper.Map<UserDto>(currentAccount);
            await UpdateAsync(userId, dto);
            return new ServiceResult(ResultConst.SUCCESS_UPDATE_CODE, ResultConst.SUCCESS_UPDATE_MSG,
                _mapper.Map<UserDto>(currentAccount));
        }


        public async Task<IServiceResult> UpdateAccount(Guid userId, UserDto userUpdateDetail, string roleName)
        {
            var currentAccount = await _unitOfWork.Repository<User, Guid>().GetByIdAsync(userId);
            if (currentAccount is null)
                return new ServiceResult(ResultConst.WARNING_NO_DATA_CODE, ResultConst.WARNING_NO_DATA_MSG);
            if (roleName.Equals("Administration"))
            {
                string errorMessageResponse = "";
                if (userUpdateDetail.UserCode!.Trim() == "" || userUpdateDetail.UserCode is null)
                {
                    errorMessageResponse =
                        errorMessageResponse + "User code is require for updating the account role \n";
                }

                if (userUpdateDetail.RoleId <= 0 || userUpdateDetail.RoleId == 4 || userUpdateDetail.UserCode is null)
                {
                    errorMessageResponse = errorMessageResponse + "Please choose available role for this account";
                }

                if (!String.IsNullOrEmpty(errorMessageResponse)) throw new BadRequestException(errorMessageResponse);

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

            return new ServiceResult(ResultConst.SUCCESS_UPDATE_CODE, ResultConst.SUCCESS_UPDATE_MSG,
                _mapper.Map<UserDto>(currentAccount));
        }

        #region Temporary return. Offical return required worker to send many emails at the time.

        public async Task<IServiceResult> CreateManyAccountsByAdmin(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
                throw new BadRequestException("File is empty or null");

            List<string> emails = new List<string>();

            //Read email from sheet 1
            using (var stream = excelFile.OpenReadStream())
            {
                using (var package = new OfficeOpenXml.ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                        throw new BadRequestException("Excel file does not contain any worksheet");

                    int rowCount = worksheet.Dimension.Rows;

                    // Email begins from row 2 and lays in first column
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var email = worksheet.Cells[row, 1].Text;
                        if (!string.IsNullOrWhiteSpace(email))
                            emails.Add(email);
                    }
                }
            }

            if (!emails.Any())
                throw new BadRequestException("No valid emails found in the Excel file");

            var result = await _roleService.GetByNameAsync(Role.GeneralMember);
            if (result.Status != ResultConst.SUCCESS_READ_CODE)
            {
                _logger.LogError("Not found any role with nameof General user");
                throw new NotFoundException("Role", "General user");
            }

            // Process Create new account
            List<string> failedEmails = new List<string>();
            Dictionary<string, string> newAccounts = new Dictionary<string, string>();

            foreach (var email in emails)
            {
                // Check if email has been used or not
                var baseSpec = new BaseSpecification<User>(u => u.Email.Equals(email));
                baseSpec.AddInclude(u => u.Role);

                var existedUser = await _unitOfWork.Repository<User, Guid>().GetWithSpecAsync(baseSpec);

                if (existedUser is not null)
                {
                    failedEmails.Add(email);
                    continue;
                }

                // Create new account with given email
                var password = Utils.HashUtils.GenerateRandomPassword();
                var newUser = new UserDto
                {
                    Email = email,
                    RoleId = (result.Data as SystemRoleDto)!.RoleId,
                    PasswordHash = Utils.HashUtils.HashPassword(password),
                    CreateDate = DateTime.Now,
                };

                await CreateAsync(newUser);
                newAccounts.Add(email, password);
            }

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
            // return new ServiceResult(ResultConst.SUCCESS_UPDATE_CODE, ResultConst.SUCCESS_UPDATE_MSG,package.GetAsByteArray());
            return new ServiceResult(ResultConst.SUCCESS_UPDATE_CODE, ResultConst.SUCCESS_UPDATE_MSG
            );
        }

        #endregion

        public async Task<IServiceResult> DeleteAccount(Guid id)
        {
            await _unitOfWork.Repository<User, Guid>().DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
            return new ServiceResult(ResultConst.SUCCESS_REMOVE_CODE, ResultConst.SUCCESS_REMOVE_MSG);
        }

        #region User Own Sending Email and format function

        public async Task SendUserEmail(UserDto newUser, string rawPassword)
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

        #endregion
        
        // public IServiceResult Send
    }
}