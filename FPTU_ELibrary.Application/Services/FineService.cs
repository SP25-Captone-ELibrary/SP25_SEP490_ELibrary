using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MimeKit.Encodings;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class FineService : GenericService<Fine, FineDto, int>, IFineService<FineDto>
{
    private readonly IUserService<UserDto> _userSvc;
    private readonly IEmployeeService<EmployeeDto> _employeeService;
    private readonly Lazy<IBorrowRecordService<BorrowRecordDto>> _borrowRecordService;

    public FineService(ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        IMapper mapper,IUserService<UserDto>userSvc,
        IEmployeeService<EmployeeDto>employeeService,
        Lazy<IBorrowRecordService<BorrowRecordDto>> borrowRecordService,
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _userSvc = userSvc;
        _employeeService = employeeService;
        _borrowRecordService = borrowRecordService;
    }

    public async Task<IServiceResult> CreateFineForBorrowRecord(int finePolicyId, int borrowRecordId,string email)
    {
        try
        {
            var employeeBaseSpec = new BaseSpecification<Employee>(u => u.Email.Equals(email));
            //get user
            var employee = await _employeeService.GetWithSpecAsync(employeeBaseSpec);
            if (employee.Data is null)
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                        StringUtils.Format(ResultCodeConst.SYS_Warning0002, "user"))
                    ;
            var employeeValue = (EmployeeDto)employee.Data!;
            FineDto dto = new FineDto()
            {
                FinePolicyId = finePolicyId,
                BorrowRecordId = borrowRecordId,
                CreatedAt = DateTime.Now,
                CreatedBy = employeeValue.EmployeeId,
                // Todo : use config to get the expiry date
                ExpiryAt = DateTime.Now.AddDays(1),
                Status = TransactionStatus.Pending.ToString()
            };
            var entity = _mapper.Map<Fine>(dto);
            await _unitOfWork.Repository<Fine, int>().AddAsync(entity);
            if (await _unitOfWork.SaveChangesAsync() <= 0)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
            }

            var extendEntity = new BaseSpecification<Fine>(f => f.FineId == entity.FineId);
            extendEntity.EnableSplitQuery();
            extendEntity.ApplyInclude(q => q.Include(f => f.FinePolicy));
            var borrowRecordSpecBase = new BaseSpecification<BorrowRecord>(br => br.BorrowRecordId == borrowRecordId);
            borrowRecordSpecBase.EnableSplitQuery();
            borrowRecordSpecBase.ApplyInclude(q =>
                q.Include(br => br.LibraryCard)
                    .ThenInclude(li => li.Users)
                    .Include(br => br.BorrowRecordDetails)
                    .ThenInclude(brd => brd.LibraryItemInstance)
                    .ThenInclude(lii => lii.LibraryItem));
            var borrow = await _borrowRecordService.Value.GetWithSpecAsync(borrowRecordSpecBase);
            if (borrow.Data is null)
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002),
                        "borrow-record"));
            var borrowValue = (BorrowRecordDto)borrow.Data!;
            TransactionDto response = new TransactionDto();
            response.TransactionCode = Guid.NewGuid().ToString();
            // fine caused by damaged or lost would base on the amount of item
            response.Amount =
                borrowValue.BorrowRecordDetails.Sum(brd => brd.LibraryItemInstance.LibraryItem.EstimatedPrice) ?? 0;
            response.TransactionType = TransactionType.Fine;
            response.UserId = borrowValue.LibraryCard.Users.First().UserId;
            response.TransactionStatus = TransactionStatus.Pending;
            response.FineId = entity.FineId;
            response.CreatedAt = DateTime.Now;
            response.PaymentMethodId = 1;
            var transactionEntity = _mapper.Map<Transaction>(response);
                await _unitOfWork.Repository<Transaction, int>().AddAsync(transactionEntity);
            if (await _unitOfWork.SaveChangesAsync() <= 0)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
            }

            return new ServiceResult(ResultCodeConst.SYS_Success0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process create fine");
        }
    }
}