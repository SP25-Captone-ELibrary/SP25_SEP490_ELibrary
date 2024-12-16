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
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ELibrary.Application.Services;

public class BookCategoryService :GenericService<BookCategory, BookCategoryDto, int>,
    IBookCategoryService<BookCategoryDto>
{
    public BookCategoryService(ISystemMessageService msgService, IUnitOfWork unitOfWork, IMapper mapper, ILogger logger)
        : base(msgService, unitOfWork, mapper, logger)
    {
    }

    public async Task<IServiceResult> GetAll()
    {
        throw new NotImplementedException();
    }

    public async Task<IServiceResult> Update(int id, BookCategoryDto dto, string roleName)
    {
        throw new NotImplementedException();
    }

    public async Task<IServiceResult?> Delete(int id, string roleName)
    {
        if (!roleName.Equals(nameof(RoleEnum.Administration)))
        {
            return new ServiceResult(ResultCodeConst.Auth_Warning0001,
                await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0001));
        }
        var baseSpec = new BaseSpecification<BookCategory>(bc => bc.CategoryId == id);
        var existedBookCategory = await _unitOfWork.Repository<BookCategory,int>().GetWithSpecAsync(baseSpec);
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

        return await DeleteAsync(existedBookCategory.CategoryId);
    }

    public async Task<IServiceResult> SoftDelete(int id, string roleName)
    {
        if (!roleName.Equals(nameof(RoleEnum.Administration)))
        {
            return new ServiceResult(ResultCodeConst.Auth_Warning0001,
                await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0001));
        }

        var baseSpec = new BaseSpecification<BookCategory>(bc => bc.CategoryId == id);
        var existedBookCategory = await _unitOfWork.Repository<BookCategory,int>().GetWithSpecAsync(baseSpec);
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

        existedBookCategory.IsDelete = true;
        return await UpdateAsync(existedBookCategory.CategoryId, _mapper.Map<BookCategoryDto>(existedBookCategory));
    }

    public async Task<IServiceResult> Create(BookCategoryDto dto, string roleName)
    {
        if (!roleName.Equals(nameof(RoleEnum.Administration)))
        {
            return new ServiceResult(ResultCodeConst.Auth_Warning0001,
                await _msgService.GetMessageAsync(ResultCodeConst.Auth_Warning0001));
        }
        return await CreateAsync(dto);
    }
}