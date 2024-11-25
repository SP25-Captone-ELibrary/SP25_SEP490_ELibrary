using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using MapsterMapper;
using Nest;

namespace FPTU_ELibrary.Application.Services
{
	public class GenericService<TEntity, TDto, TKey> : ReadOnlyService<TEntity, TDto, TKey>, IGenericService<TEntity, TDto, TKey>
        where TEntity : class
        where TDto : class
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected readonly IMapper _mapper;

        public GenericService(IUnitOfWork unitOfWork, IMapper mapper) : base(unitOfWork, mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public virtual async Task<IServiceResult> CreateAsync(TDto dto)
        {
			// Initiate service result
			var serviceResult = new ServiceResult();

			try
            {
				// Validate inputs using the generic validator
				var validationResult = await ValidatorExtensions.ValidateAsync(dto);
				// Check for valid validations
				if (validationResult != null && !validationResult.IsValid)
				{
					// Convert ValidationResult to ValidationProblemsDetails.Errors
					var errors = validationResult.ToProblemDetails().Errors;
					throw new UnprocessableEntityException("Validation errors", errors);
				}
				
				// Process add new entity
				await _unitOfWork.Repository<TEntity, TKey>().AddAsync(_mapper.Map<TEntity>(dto));
				// Save to DB
				if (await _unitOfWork.SaveChangesAsync() > 0)
				{
					serviceResult.Status = ResultConst.SUCCESS_INSERT_CODE;
					serviceResult.Message = ResultConst.SUCCESS_INSERT_MSG;
					serviceResult.Data = true;
				}
				else
				{
					serviceResult.Status = ResultConst.FAIL_INSERT_CODE;
					serviceResult.Message = ResultConst.FAIL_INSERT_MSG;
					serviceResult.Data = false;
				}
			}
			catch(Exception)
            {
                throw;
            }
			
			return serviceResult;
        }

        public virtual async Task<IServiceResult> DeleteAsync(TKey id)
        {
			// Initiate service result
			var serviceResult = new ServiceResult();

			try
			{
				// Retrieve the entity
				var existingEntity = await _unitOfWork.Repository<TEntity, TKey>().GetByIdAsync(id);
				if (existingEntity == null)
				{
					throw new NotFoundException(nameof(TEntity), id!.ToString()!);
				}

				// Process add delete entity
				await _unitOfWork.Repository<TEntity, TKey>().DeleteAsync(id);
				// Save to DB
				if (await _unitOfWork.SaveChangesAsync() > 0)
				{
					serviceResult.Status = ResultConst.SUCCESS_REMOVE_CODE;
					serviceResult.Message = ResultConst.SUCCESS_REMOVE_MSG;
					serviceResult.Data = true;
				}
				else
				{
					serviceResult.Status = ResultConst.FAIL_REMOVE_CODE;
					serviceResult.Message = ResultConst.FAIL_REMOVE_MSG;
					serviceResult.Data = false;
				}
			}
			catch (Exception)
			{
				throw;
			}

			return serviceResult;
        }

        public virtual async Task<IServiceResult> UpdateAsync(TKey id, TDto dto)
        {
			// Initiate service result
			var serviceResult = new ServiceResult();

			try
			{
				// Validate inputs using the generic validator
				var validationResult = await ValidatorExtensions.ValidateAsync(dto);
				// Check for valid validations
				if (validationResult != null && !validationResult.IsValid)
				{
					// Convert ValidationResult to ValidationProblemsDetails.Errors
					var errors = validationResult.ToProblemDetails().Errors;
					throw new UnprocessableEntityException("Validation errors", errors);
				}

				// Retrieve the entity
				var existingEntity = await _unitOfWork.Repository<TEntity, TKey>().GetByIdAsync(id);
				if (existingEntity == null)
				{
					throw new NotFoundException(nameof(TEntity), id!.ToString()!);
				}

				// Process add update entity
				// Map properties from dto to existingEntity
				_mapper.Map(dto, existingEntity);

				// Check if there are any differences between the original and the updated entity
				if (!_unitOfWork.Repository<TEntity, TKey>().HasChanges(existingEntity))
				{
					serviceResult.Status = ResultConst.SUCCESS_UPDATE_CODE;
					serviceResult.Message = ResultConst.SUCCESS_UPDATE_MSG;
					serviceResult.Data = true;
					return serviceResult;
				}

				// Progress update when all require passed
				await _unitOfWork.Repository<TEntity, TKey>().UpdateAsync(existingEntity);

				// Save changes to DB
				var rowsAffected = await _unitOfWork.SaveChangesAsync();
				if (rowsAffected == 0)
				{
					serviceResult.Status = ResultConst.FAIL_UPDATE_CODE;
					serviceResult.Message = ResultConst.FAIL_UPDATE_MSG;
					serviceResult.Data = false;
					return serviceResult;
				}

				// Mark as update success
				serviceResult.Status = ResultConst.SUCCESS_UPDATE_CODE;
				serviceResult.Message = ResultConst.SUCCESS_UPDATE_MSG;
				serviceResult.Data = true;
			}
			catch (Exception)
			{
				throw;
			}

			return serviceResult;
		}
    }
}
