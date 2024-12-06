using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using MapsterMapper;
using Nest;
using Serilog;

namespace FPTU_ELibrary.Application.Services
{
	public class GenericService<TEntity, TDto, TKey> : ReadOnlyService<TEntity, TDto, TKey>, IGenericService<TEntity, TDto, TKey>
        where TEntity : class
        where TDto : class
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected readonly ISystemMessageService _msgService;
        protected readonly IMapper _mapper;
        protected readonly ILogger _logger;
        
        public GenericService(
	        ISystemMessageService msgService,
	        IUnitOfWork unitOfWork, 
	        IMapper mapper,
	        ILogger logger) : base(msgService, unitOfWork, mapper, logger)
        {
	        _logger = logger;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _msgService = msgService;
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
					throw new UnprocessableEntityException("Invalid Validations", errors);
				}
				
				// Process add new entity
				await _unitOfWork.Repository<TEntity, TKey>().AddAsync(_mapper.Map<TEntity>(dto));
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
			catch(Exception ex)
            {
	            _logger.Error(ex.Message);
                throw new Exception("Error invoke when progress create new entity");
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
					serviceResult.ResultCode = ResultCodeConst.SYS_Success0004;
					serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004);
					serviceResult.Data = true;
				}
				else
				{
					serviceResult.ResultCode = ResultCodeConst.SYS_Fail0004;
					serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004);
					serviceResult.Data = false;
				}
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress delete entity");
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
					throw new UnprocessableEntityException("Invalid validations", errors);
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
					serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
					serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
					serviceResult.Data = true;
					return serviceResult;
				}

				// Progress update when all require passed
				await _unitOfWork.Repository<TEntity, TKey>().UpdateAsync(existingEntity);

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
				throw new Exception("Error invoke when progress update new entity");
			}

			return serviceResult;
		}
    }
}
