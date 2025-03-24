using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class BorrowRequestResourceService : GenericService<BorrowRequestResource, BorrowRequestResourceDto, int>,
    IBorrowRequestResourceService<BorrowRequestResourceDto>
{
    private readonly Lazy<IUserService<UserDto>> _userSvc;
    private readonly Lazy<IBorrowRequestService<BorrowRequestDto>> _borrowReqSvc;

    public BorrowRequestResourceService(
        Lazy<IBorrowRequestService<BorrowRequestDto>> borrowReqSvc,
        Lazy<IUserService<UserDto>> userSvc,
        
        ISystemMessageService msgService,
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _userSvc = userSvc;
        _borrowReqSvc = borrowReqSvc;
    }

    public async Task<IServiceResult> GetAllByRequestIdToConfirmCreateTransactionAsync(string email, int borrowRequestId)
    {
        try
        {
            // Retrieve user information
            var userSpec = new BaseSpecification<User>(u => u.Email == email);
            var userDto = (await _userSvc.Value.GetWithSpecAsync(userSpec)).Data as UserDto;
            if (userDto == null) throw new ForbiddenException("Not allow to access");
            
            // Retrieve borrow request by id
            var isExistBorrowReq = (await _borrowReqSvc.Value.AnyAsync(b => b.BorrowRequestId == borrowRequestId)).Data is true;
            if (!isExistBorrowReq)
            {
                // Data is empty or null
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                    new List<BorrowRequestResource>());
            }
            
            // Build spec
            var baseSpec = new BaseSpecification<BorrowRequestResource>(br => 
                (
                    br.TransactionId == null || // Has not created transaction yet
                    br.Transaction != null && 
                    (
                        br.Transaction.TransactionStatus != TransactionStatus.Pending &&
                        br.Transaction.TransactionStatus != TransactionStatus.Paid
                    )
                ) && 
                br.BorrowRequestId == borrowRequestId && 
                br.BorrowRequest.LibraryCardId == userDto.LibraryCardId);
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(b => b.LibraryResource)
            );
            // Retrieve all borrow request resources by request id
            var entities = (await _unitOfWork.Repository<BorrowRequestResource,int >().GetAllWithSpecAsync(baseSpec)).ToList();
            if (entities.Any())
            {
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                    _mapper.Map<List<BorrowRequestResourceDto>>(entities));
            }
            
            // Data is empty or null
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                new List<BorrowRequestResourceDto>());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get all borrow resources by borrow request id to confirm create transaction");
        }
    }
    
    public async Task<IServiceResult> AddRangeResourceTransactionsWithoutSaveChangesAsync(List<BorrowRequestResourceDto> borrowResources)
    {
        try
        {
            // Process add borrow request resource transaction
            if (borrowResources.Any())
            {
                // Iterate each borrow request resource to add transaction
                foreach (var reqSrc in borrowResources)
                {
                    // Retrieve by id 
                    var existingEntity = await _unitOfWork.Repository<BorrowRequestResource, int>().GetByIdAsync(reqSrc.BorrowRequestResourceId);
                    if (existingEntity != null)
                    {
                        // Add transaction
                        if (reqSrc.Transaction != null) existingEntity.Transaction = _mapper.Map<Transaction>(reqSrc.Transaction);
                        // Process update
                        await _unitOfWork.Repository<BorrowRequestResource, int>().UpdateAsync(existingEntity);
                    }
                }
                
                // Mark as success to add
                return new ServiceResult(ResultCodeConst.SYS_Success0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001), true);
            }            
            
            // Mark as failed to add
            return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001), false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process add resource transactions");
        }
    }
}