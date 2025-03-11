using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using MapsterMapper;
using System.Linq.Expressions;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Serilog;

namespace FPTU_ELibrary.Application.Services
{
	public class ReadOnlyService<TEntity, TDto, TKey> : IReadOnlyService<TEntity, TDto, TKey>
        where TEntity : class
        where TDto : class
    {
        protected IUnitOfWork _unitOfWork;
        protected IMapper _mapper;
        protected ILogger _logger;
        protected ISystemMessageService _msgService;

        public ReadOnlyService(
	        ISystemMessageService msgService,
	        IUnitOfWork unitOfWork, 
	        IMapper mapper,
	        ILogger logger)
        {
	        _logger = logger;
	        _msgService = msgService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

		public virtual async Task<IServiceResult> GetAllAsync(bool tracked = true)
        {
            try
            {
                var entities = await _unitOfWork.Repository<TEntity, TKey>().GetAllAsync();

                if (!entities.Any())
                {
                    return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
	                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004), 
                        _mapper.Map<IEnumerable<TDto>>(entities));
                }

                return new ServiceResult(ResultCodeConst.SYS_Success0002, 
	                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), 
                    _mapper.Map<IEnumerable<TDto>>(entities));
            }
            catch(Exception ex)
            {
	            _logger.Error(ex.Message);
                throw new Exception("Error invoke when progress get all data");
            }
        }

		public virtual async Task<IServiceResult> GetByIdAsync(TKey id)
        {
            try
            {
                var entity = await _unitOfWork.Repository<TEntity, TKey>().GetByIdAsync(id);

				if (entity == null)
				{
					return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
				}

				return new ServiceResult(ResultCodeConst.SYS_Success0002, 
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), 
					_mapper.Map<TDto>(entity));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress get data");
			}
		}

		public virtual async Task<IServiceResult> GetWithSpecAsync(ISpecification<TEntity> specification, bool tracked = true)
		{
			try
			{
				var entity = await _unitOfWork.Repository<TEntity, TKey>().GetWithSpecAsync(specification, tracked);

				if (entity == null)
				{
					return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
				}

				return new ServiceResult(ResultCodeConst.SYS_Success0002, 
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), 
					_mapper.Map<TDto>(entity));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress get data");
			}
		}
		
		public virtual async Task<IServiceResult> GetAllWithSpecAsync(ISpecification<TEntity> specification, bool tracked = true)
		{
			try
			{
				var entities = await _unitOfWork.Repository<TEntity, TKey>().GetAllWithSpecAsync(specification, tracked);

				if (!entities.Any())
				{
					return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
						_mapper.Map<IEnumerable<TDto>>(entities));
				}

				return new ServiceResult(ResultCodeConst.SYS_Success0002, 
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
					_mapper.Map<IEnumerable<TDto>>(entities));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress get all data");
			}
		}

		public virtual async Task<IServiceResult> GetWithSpecAndSelectorAsync<TResult>(ISpecification<TEntity> specification,
			Expression<Func<TEntity, TResult>> selector, bool tracked = true)
		{
			try
			{
				var tResult = await _unitOfWork.Repository<TEntity, TKey>().GetWithSpecAndSelectorAsync(
					specification, selector, tracked);

				if (tResult == null)
				{
					return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
				}

				return new ServiceResult(ResultCodeConst.SYS_Success0002, 
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), tResult);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress get data by selector");
			}
		}

		public virtual async Task<IServiceResult> GetAllWithSpecAndSelectorAsync<TResult>(
			ISpecification<TEntity> specification,
			Expression<Func<TEntity, TResult>> selector,
			bool tracked = true)
		{
			try
			{
				var tResults = await _unitOfWork.Repository<TEntity, TKey>()
					.GetAllWithSpecAndSelectorAsync(specification, selector, tracked);

				if (!tResults.Any())
				{
					return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004), tResults);
				}

				return new ServiceResult(ResultCodeConst.SYS_Success0002, 
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), tResults);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress get all data by selector");
			}
		}
		
		public virtual async Task<IServiceResult> AnyAsync(Expression<Func<TEntity, bool>> predicate)
		{
			try
			{
				var hasAny = await _unitOfWork.Repository<TEntity, TKey>().AnyAsync(predicate);

				if (!hasAny)
				{
					return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004), false);
				}

				return new ServiceResult(ResultCodeConst.SYS_Success0002, 
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), true);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress check total data");
			}
		}
		
		public virtual async Task<IServiceResult> AnyAsync(ISpecification<TEntity> specification)
		{
			try
			{
				var hasAny = await _unitOfWork.Repository<TEntity, TKey>().AnyAsync(specification);

				if (!hasAny)
				{
					return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004), false);
				}

				return new ServiceResult(ResultCodeConst.SYS_Success0002, 
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), true);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress check total data");
			}
		}

		public virtual async Task<IServiceResult> CountAsync(ISpecification<TEntity> specification)
		{
			try
			{
				var totalEntity = await _unitOfWork.Repository<TEntity, TKey>().CountAsync(specification);
				return new ServiceResult(ResultCodeConst.SYS_Success0002, 
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), totalEntity);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress count data");
			}
		}

		public async Task<IServiceResult> CountAsync()
		{
			try
			{
				var totalEntity = await _unitOfWork.Repository<TEntity, TKey>().CountAsync();
				return new ServiceResult(ResultCodeConst.SYS_Success0002, 
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), totalEntity);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress count data");
			}
		}
    }
}
