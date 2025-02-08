using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
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
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class BorrowRecordService : GenericService<BorrowRecord, BorrowRecordDto, int>,
    IBorrowRecordService<BorrowRecordDto>
{
    // Lazy services
    private readonly Lazy<ILibraryItemInstanceService<LibraryItemInstanceDto>> _itemInstanceSvc;
    
    private readonly IEmployeeService<EmployeeDto> _employeeSvc;
    private readonly ILibraryCardService<LibraryCardDto> _cardSvc;
    private readonly IBorrowRequestService<BorrowRequestDto> _borrowReqSvc;
    private readonly ICategoryService<CategoryDto> _cateSvc;

    public BorrowRecordService(
        // Lazy services
        Lazy<ILibraryItemInstanceService<LibraryItemInstanceDto>> itemInstanceSvc,
        
        // Normal services
        ICategoryService<CategoryDto> cateSvc,
        IBorrowRequestService<BorrowRequestDto> borrowReqSvc,
        ILibraryCardService<LibraryCardDto> cardSvc,
        IEmployeeService<EmployeeDto> employeeSvc,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _cateSvc = cateSvc;
        _cardSvc = cardSvc;
        _employeeSvc = employeeSvc;
        _borrowReqSvc = borrowReqSvc;
        _itemInstanceSvc = itemInstanceSvc;
    }

    public async Task<IServiceResult> ProcessRequestToBorrowRecordAsync(string processedByEmail, BorrowRecordDto dto)
    {
        try
        {
            // Determine current system lang 
            var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Retrieve user information
            // Build spec
            var employeeBaseSpec = new BaseSpecification<Employee>(u => Equals(u.Email, processedByEmail));
            // Apply include
            employeeBaseSpec.ApplyInclude(q => q
                .Include(u => u.Role)
            );
            // Retrieve user with spec
            var employeeDto = (await _employeeSvc.GetWithSpecAsync(employeeBaseSpec)).Data as EmployeeDto;
            // Not found or not proceeded by employee
            if (employeeDto == null || employeeDto.Role.RoleType != nameof(RoleType.Employee))
            {
                // Forbid 
                throw new ForbiddenException();
            }
                        
            // Check library card existence
            if (dto.LibraryCardId == Guid.Empty)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
                    StringUtils.Format(errMsg, isEng ? "library card" : "thẻ thư viện"));
            }
            
            // Validate library card 
            var validateCardRes = await _cardSvc.CheckCardValidityAsync(dto.LibraryCardId);
            // Return invalid card
            if (validateCardRes.ResultCode != ResultCodeConst.LibraryCard_Success0001) return validateCardRes;
            
            // Check whether request already handled to borrow record
            var isBorrowReqExist = await _unitOfWork.Repository<BorrowRecord,int>().AnyAsync(br => br.BorrowRequestId == dto.BorrowRequestId);
            if (isBorrowReqExist)
            {
                // Msg: Cannot create borrow record because borrow request has been processed
                return new ServiceResult(ResultCodeConst.Borrow_Warning0011,
                    await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0011));
            }
            
            // Retrieve borrow request information
            // Build borrow request spec
            var borrowReqSpec = new BaseSpecification<BorrowRequest>(br => br.BorrowRequestId == dto.BorrowRequestId);
            // Apply include
            borrowReqSpec.ApplyInclude(q => q
                .Include(br => br.BorrowRequestDetails)
            );
            var borrowReqDto = (await _borrowReqSvc.GetWithSpecAsync(borrowReqSpec)).Data as BorrowRequestDto;
            if (borrowReqDto == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
                    StringUtils.Format(errMsg, isEng ? "borrow request in request history" : "lịch sử đăng ký mượn"));
            }
            
            // Check for borrow request status
            // Only process for request with created status
            if (borrowReqDto.Status != BorrowRequestStatus.Created) 
            {
                switch (borrowReqDto.Status)
                {
                    case BorrowRequestStatus.Borrowed:
                        // Msg: Cannot create borrow record because borrow request has been processed
                        return new ServiceResult(ResultCodeConst.Borrow_Warning0011,
                            await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0011));
                    case BorrowRequestStatus.Cancelled:
                        // Msg: Cannot create borrow record because borrow request has been cancelled
                        return new ServiceResult(ResultCodeConst.Borrow_Warning0012,
                            await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0012));
                    case BorrowRequestStatus.Expired:
                        // Msg: Cannot create borrow record because borrow request has been expired
                        return new ServiceResult(ResultCodeConst.Borrow_Warning0013,
                            await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0013));
                    default:
                        // Msg: An error occured, failed to create borrow record
                        return new ServiceResult(ResultCodeConst.Borrow_Fail0002,
                            await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Fail0002));
                }
            }
            
            // Check whether card match with request
            if (!Equals(borrowReqDto.LibraryCardId, dto.LibraryCardId))
            {
                // Msg: Library card does not match the card registered to borrow
                return new ServiceResult(ResultCodeConst.LibraryCard_Warning0005,
                    await _msgService.GetMessageAsync(ResultCodeConst.LibraryCard_Warning0005));
            }
            
            // Check for total details
            var totalRequested = borrowReqDto.BorrowRequestDetails.Count;
            var totalEntered = dto.BorrowRecordDetails.Count;
            if (totalEntered < totalRequested)
            {
                // Msg: The total number of items entered is not enough compared to the total number registered to borrow
                return new ServiceResult(ResultCodeConst.Borrow_Warning0009,
                    await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0009));
            }else if (totalEntered > totalRequested)
            {
                // Msg: The total number of items entered exceeds the total number registered for borrowing
                return new ServiceResult(ResultCodeConst.Borrow_Warning0010,
                    await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Warning0010));
            }
            
            // Custom errors
            var customErrs = new Dictionary<string, string[]>();
            // Initialize field to determine the longest borrow days
            var longestBorrowDays = 0;
            // Initialize hash set to check duplicate instance
            var instanceIdHashSet = new HashSet<int>();
            // Extract all existing item in request
            var itemInRequestIds = borrowReqDto.BorrowRequestDetails.Select(bi => bi.LibraryItemId).ToList();
            // Iterate each borrow record details to check for item instance quantity status
            var borrowRecordDetailList = dto.BorrowRecordDetails.ToList();
            for (int i = 0; i <borrowRecordDetailList.Count; ++i)
            {
                var detail = borrowRecordDetailList[i];
                
                // Check duplicates instance in the same item
                if (!instanceIdHashSet.Add(detail.LibraryItemInstanceId))
                {
                    // Add error 
                    customErrs = DictionaryUtils.AddOrUpdate(customErrs, $"libraryItemInstanceIds[{i}]",
                        isEng 
                            ? "Item instance is duplicated" 
                            : "Bản sao đã bị trùng");
                }
                
                // Retrieving item instance
                var itemInstanceDto = (await _itemInstanceSvc.Value.GetByIdAsync(detail.LibraryItemInstanceId)).Data as LibraryItemInstanceDto;
                if (itemInstanceDto == null)
                {
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                    // Add error
                    customErrs = DictionaryUtils.AddOrUpdate(customErrs, $"libraryItemInstanceIds[{i}]",
                        StringUtils.Format(errMsg, isEng ? "any item instance match" : "bản sao"));
                }
                
                // Retrieve category
                var categoryDto = (await _cateSvc.GetWithSpecAsync(new BaseSpecification<Category>(
                    c => c.LibraryItems.Any(li => li.LibraryItemId == itemInstanceDto!.LibraryItemId)))).Data as CategoryDto;
                // Check whether category is not null and its total borrow days greater than current longest borrow days
                if (categoryDto != null && categoryDto.TotalBorrowDays > longestBorrowDays)
                {
                    // Assign value
                    longestBorrowDays = categoryDto.TotalBorrowDays;
                }
                
                // Check item instance exist in borrow item
                if (!itemInRequestIds.Contains(itemInstanceDto!.LibraryItemId))
                {
                    var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                    // Add error
                    customErrs = DictionaryUtils.AddOrUpdate(customErrs, $"libraryItemInstanceIds[{i}]",
                        StringUtils.Format(errMsg, isEng 
                            ? "item instance in registered borrow request" 
                            : "bản sao tồn tại trong lịch sử đăng ký mượn"));
                }
                
                // Check item instance status
                if (Enum.TryParse(itemInstanceDto.Status, true, out LibraryItemInstanceStatus status))
                {
                    switch (status)
                    {
                        case LibraryItemInstanceStatus.InShelf:
                            // Skip, continue to check for other instance
                            break;
                        case LibraryItemInstanceStatus.OutOfShelf:
                            // Announce that item has not in borrowing status yet
                            customErrs = DictionaryUtils.AddOrUpdate(customErrs, $"libraryItemInstanceIds[{i}]",
                                isEng 
                                    ? "Item instance is in out-of-shelf status, cannot process" 
                                    : "Trạng thái của bản sao đang ở trong kho, không thể xử lí");
                            break;
                        case LibraryItemInstanceStatus.Borrowed:
                            // Announce that item has not in borrowing status yet
                            customErrs = DictionaryUtils.AddOrUpdate(customErrs, $"libraryItemInstanceIds[{i}]",
                                isEng 
                                    ? "Item instance is in borrowed status" 
                                    : "Trạng thái của bản sao đang được mượn");
                            break;
                        case LibraryItemInstanceStatus.Reserved:
                            // Announce that item has not in borrowing status yet
                            customErrs = DictionaryUtils.AddOrUpdate(customErrs, $"libraryItemInstanceIds[{i}]",
                                isEng 
                                    ? "Item instance is in reserved status" 
                                    : "Bản sao đang ở trạng thái được đặt trước");
                            break;
                    }
                }
                else
                {
                    // Unknown error
                    return new ServiceResult(ResultCodeConst.SYS_Warning0006,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0006));
                }
            }
            
            // Check if invoke any errors
            if (customErrs.Any()) throw new UnprocessableEntityException("Invalid data", customErrs);

            // Current local datetime
            var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                // Vietnam timezone
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            // Set default values for borrow record
            dto.BorrowDate = currentLocalDateTime; // Current date
            dto.DueDate = currentLocalDateTime.AddDays(longestBorrowDays); // Due date = current date + longest borrow days
            dto.Status = BorrowRecordStatus.Borrowing;
            dto.SelfServiceBorrow = false;
            dto.TotalExtension = 0;
            dto.ProcessedBy = employeeDto.EmployeeId;
            
            // Process add borrow record
            await _unitOfWork.Repository<BorrowRecord, int>().AddAsync(_mapper.Map<BorrowRecord>(dto));
            // Update borrow request status
            await _borrowReqSvc.UpdateStatusWithoutSaveChangesAsync(borrowReqDto.BorrowRequestId,
                BorrowRequestStatus.Borrowed); // Update to borrowed status
            // Update range library item status
            await _itemInstanceSvc.Value.UpdateRangeStatusAndInventoryWithoutSaveChangesAsync(
                libraryItemInstanceIds: dto.BorrowRecordDetails.Select(x => x.LibraryItemInstanceId).ToList(),
                status: LibraryItemInstanceStatus.Borrowed,
                isProcessBorrowRequest: true);
            
            // Save DB with transaction
            var isSaved = await _unitOfWork.SaveChangesWithTransactionAsync() > 0;
            if (isSaved)
            {
                // Msg: Total {0} item(s) have been added to borrow record successfully
                var msg = await _msgService.GetMessageAsync(ResultCodeConst.Borrow_Success0003);
                return new ServiceResult(ResultCodeConst.Borrow_Success0003,
                    StringUtils.Format(msg, dto.BorrowRecordDetails.Count.ToString()));
            }
            
            // Fail to save
            return new ServiceResult(ResultCodeConst.SYS_Fail0001,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process borrow request to borrow record. Performed by: " + processedByEmail);
        }
    }
}