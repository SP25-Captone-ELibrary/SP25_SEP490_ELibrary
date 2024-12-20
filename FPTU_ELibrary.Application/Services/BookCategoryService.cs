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

    public async Task<IServiceResult?> Delete(int id)
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

        existedBookCategory.IsDeleted = true;
        return await UpdateAsync(existedBookCategory.CategoryId, _mapper.Map<BookCategoryDto>(existedBookCategory));
    }

    public async Task<IServiceResult> SoftDeleteAsync(int bookCategoryId)
    {
        try
        {
            // Check exist book category
            var existingEntity = await _unitOfWork.Repository<BookCategory,int>().GetByIdAsync(bookCategoryId);
            // Check if book category already mark as deleted
            if (existingEntity == null || existingEntity.IsDeleted)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, "Category"));
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
            throw new Exception("Error invoke when process soft delete Category");	
        }
    }

    public async Task<IServiceResult> SoftDeleteRangeAsync(int[] bookCategoryIds)
    {
        try
        {
            // Get all matching category 
            // Build spec
            var baseSpec = new BaseSpecification<BookCategory>(e => bookCategoryIds.Contains(e.CategoryId));
            var categoryEntities = await _unitOfWork.Repository<BookCategory, int>()
                .GetAllWithSpecAsync(baseSpec);
            // Check if any data already soft delete
            var categoryList = categoryEntities.ToList();
            if (categoryList.Any(x => x.IsDeleted))
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }
                
            // Progress update deleted status to true
            categoryList.ForEach(x => x.IsDeleted = true);
            	
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
            throw new Exception("Error invoke when remove range category");
        }
    }

    public async Task<IServiceResult> UndoDeleteAsync(int bookCategoryId)
    {
        try
        {
            // Check exist category
            var existingEntity = await _unitOfWork.Repository<BookCategory, int>().GetByIdAsync(bookCategoryId);
            // Check if category account already mark as deleted
            if (existingEntity == null || !existingEntity.IsDeleted)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, "category"));
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
            throw new Exception("Error invoke when process undo delete category");	
        }
    }

    public async Task<IServiceResult> UndoDeleteRangeAsync(int[] bookCategoryIds)
    {
        try
        {
            // Get all matching category 
            // Build spec
            var baseSpec = new BaseSpecification<BookCategory>(e => bookCategoryIds.Contains(e.CategoryId));
            var categoryEntities = await _unitOfWork.Repository<BookCategory, int>()
                .GetAllWithSpecAsync(baseSpec);
            // Check if any data already soft delete
            var categoryList = categoryEntities.ToList();
            if (categoryList.Any(x => !x.IsDeleted))
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0004,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004));
            }
                
            // Progress undo deleted status to false
            categoryList.ForEach(x => x.IsDeleted = false);
                        
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

    public async Task<IServiceResult> HardDeleteRangeAsync(int[] bookCategoryIds)
    {
        try
        {
            // Get all matching book category 
            // Build spec
            var baseSpec = new BaseSpecification<BookCategory>(e => bookCategoryIds.Contains(e.CategoryId));
            var categoryEntities = await _unitOfWork.Repository<BookCategory, int>()
                .GetAllWithSpecAsync(baseSpec);
            // Check if any data already soft delete
            var categoryList = categoryEntities.ToList();
            if (categoryList.Any(x => !x.IsDeleted))
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