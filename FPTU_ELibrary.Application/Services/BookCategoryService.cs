using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using MapsterMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using RoleEnum = FPTU_ELibrary.Domain.Common.Enums.Role;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class BookCategoryService : GenericService<BookCategory, BookCategoryDto, int>,
    IBookCategoryService<BookCategoryDto>
{
    public BookCategoryService(ISystemMessageService msgService, IUnitOfWork unitOfWork, IMapper mapper, ILogger logger)
        : base(msgService, unitOfWork, mapper, logger)
    {
    }

    public override async Task<IServiceResult> DeleteAsync(int id)
    {
        var baseSpec = new BaseSpecification<BookCategory>(bc => bc.CategoryId == id);
        var existedBookCategory = await _unitOfWork.Repository<BookCategory, int>().GetWithSpecAsync(baseSpec);
        if (existedBookCategory is null)
        {
            var errorMsg = await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0006);
            return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                StringUtils.Format(errorMsg, "book-category"));
        }
        else if (existedBookCategory.Books.Any())
        {
            return new ServiceResult(ResultCodeConst.BookCategory_Warning0001,
                await _msgService.GetMessageAsync(ResultCodeConst.BookCategory_Warning0001));
        }
        await _unitOfWork.Repository<BookCategory, int>().DeleteAsync(id);
        if (await _unitOfWork.SaveChangesAsync() > 0)
        {
            var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0008);
            return new ServiceResult(ResultCodeConst.SYS_Success0008,
                StringUtils.Format(msg, id.ToString()), true);
        }
        return new ServiceResult(ResultCodeConst.SYS_Fail0004
            , await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004)
            ,false);
    }

    public async Task<IServiceResult> HardDeleteRangeAsync(int[] bookCategoryIds)
    {
        try
        {
            // Get all matching book category 
            // Build spec
            var baseSpec = new BaseSpecification<BookCategory>(e => bookCategoryIds.Contains(e.CategoryId));
            var categoryEntities = await _unitOfWork.Repository<BookCategory, int>()
                .GetAllWithSpecAsync(baseSpec);
            var categoryList = categoryEntities.ToList();
            if (categoryList.Any(x => x.Books.Any()))
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }

            // Process delete range
            await _unitOfWork.Repository<BookCategory, int>().DeleteRangeAsync(bookCategoryIds);
            // Save to DB
            if (await _unitOfWork.SaveChangesAsync() > 0)
            {
                var msg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0008);
                return new ServiceResult(ResultCodeConst.SYS_Success0008,
                    StringUtils.Format(msg, categoryList.Count.ToString()), true);
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
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process delete range Book Category");
        }
    }
}