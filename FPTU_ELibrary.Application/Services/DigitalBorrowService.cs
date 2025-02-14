using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class DigitalBorrowService : GenericService<DigitalBorrow, DigitalBorrowDto, int>,
    IDigitalBorrowService<DigitalBorrowDto>
{
    private readonly IUserService<UserDto> _userSvc;

    public DigitalBorrowService(
        IUserService<UserDto> userSvc,
        ISystemMessageService msgService, 
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
    {
        _userSvc = userSvc;
    }

    public async Task<IServiceResult> GetAllByUserIdAsync(Guid userId, int pageIndex, int pageSize)
    {
        try
        {
            // Build spec
            var baseSpec = new BaseSpecification<DigitalBorrow>(br => br.UserId == userId);   
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(db => db.LibraryResource)
                .Include(db => db.Transactions)
                    .ThenInclude(tr => tr.Invoice)
            );
            
            // Add default order by
            baseSpec.AddOrderByDescending(br => br.RegisterDate);
            
            // Count total borrow request
            var totalDigitalWithSpec = await _unitOfWork.Repository<DigitalBorrow, int>().CountAsync(baseSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalDigitalWithSpec / pageSize);

            // Set pagination to specification after count total digital borrow
            if (pageIndex > totalPage
                || pageIndex < 1) // Exceed total page or page index smaller than 1
            {
                pageIndex = 1; // Set default to first page
            }
            // Apply pagination
            baseSpec.ApplyPaging(skip: pageSize * (pageIndex - 1), take: pageSize);
            
            // Retrieve data with spec
            var entities = await _unitOfWork.Repository<DigitalBorrow, int>()
                .GetAllWithSpecAsync(baseSpec);
            if (entities.Any())
            {
                var digitalDtos = _mapper.Map<List<DigitalBorrowDto>>(entities);
                
                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<LibraryCardHolderDigitalBorrowDto>(
                    digitalDtos.Select(d => d.ToCardHolderDigitalBorrowDto()),
                    pageIndex, pageSize, totalPage, totalDigitalWithSpec);

                // Response with pagination 
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }
            
            // Data not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                new List<LibraryCardHolderDigitalBorrowDto>());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when get all digital borrow by user id");
        }
    }
    
    public async Task<IServiceResult> GetAllByEmailAsync(string email, int pageIndex, int pageSize)
    {
        try
        {
            // Try to get user information
            var userSpec = new BaseSpecification<User>(u => Equals(u.Email, email));
            var userDto = (await _userSvc.GetWithSpecAsync(userSpec)).Data as UserDto;
            if (userDto == null)
            {
                // Data not found or empty
                return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                    new List<LibraryCardHolderDigitalBorrowDto>());
            }
            
            // Build spec
            var baseSpec = new BaseSpecification<DigitalBorrow>(br => br.UserId == userDto.UserId);   
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(db => db.LibraryResource)
                .Include(db => db.Transactions)
                    .ThenInclude(tr => tr.Invoice)
            );
            
            // Add default order by
            baseSpec.AddOrderByDescending(br => br.RegisterDate);
            
            // Count total borrow request
            var totalDigitalWithSpec = await _unitOfWork.Repository<DigitalBorrow, int>().CountAsync(baseSpec);
            // Count total page
            var totalPage = (int)Math.Ceiling((double)totalDigitalWithSpec / pageSize);

            // Set pagination to specification after count total digital borrow
            if (pageIndex > totalPage
                || pageIndex < 1) // Exceed total page or page index smaller than 1
            {
                pageIndex = 1; // Set default to first page
            }
            // Apply pagination
            baseSpec.ApplyPaging(skip: pageSize * (pageIndex - 1), take: pageSize);
            
            // Retrieve data with spec
            var entities = await _unitOfWork.Repository<DigitalBorrow, int>()
                .GetAllWithSpecAsync(baseSpec);
            if (entities.Any())
            {
                var digitalDtos = _mapper.Map<List<DigitalBorrowDto>>(entities);
                
                // Pagination result 
                var paginationResultDto = new PaginatedResultDto<LibraryCardHolderDigitalBorrowDto>(
                    digitalDtos.Select(d => d.ToCardHolderDigitalBorrowDto()),
                    pageIndex, pageSize, totalPage, totalDigitalWithSpec);

                // Response with pagination 
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
            }
            
            // Data not found or empty
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
                new List<LibraryCardHolderDigitalBorrowDto>());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when get all digital borrow by email");
        }
    }
}