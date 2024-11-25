using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using MapsterMapper;
using Nest;
using System.Linq.Expressions;

namespace FPTU_ELibrary.Application.Services
{
	public class ReadOnlyService<TEntity, TDto, TKey> : IReadOnlyService<TEntity, TDto, TKey>
        where TEntity : class
        where TDto : class
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ReadOnlyService(IUnitOfWork unitOfWork, IMapper mapper)
        {
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
                    return new ServiceResult(ResultConst.WARNING_NO_DATA_CODE, ResultConst.WARNING_NO_DATA_MSG, 
                        _mapper.Map<IEnumerable<TDto>>(entities));
                }

                return new ServiceResult(ResultConst.SUCCESS_READ_CODE, ResultConst.SUCCESS_READ_MSG, 
                    _mapper.Map<IEnumerable<TDto>>(entities));
            }
            catch(Exception)
            {
                throw;
            }
        }

		public virtual async Task<IServiceResult> GetByIdAsync(TKey id)
        {
            try
            {
                var entity = await _unitOfWork.Repository<TEntity, TKey>().GetByIdAsync(id);

				if (entity == null)
				{
					return new ServiceResult(ResultConst.WARNING_NO_DATA_CODE, ResultConst.WARNING_NO_DATA_MSG);
				}

				return new ServiceResult(ResultConst.SUCCESS_READ_CODE, ResultConst.SUCCESS_READ_MSG, _mapper.Map<TDto>(entity));
			}
			catch (Exception)
			{
                throw;
			}
		}

		public virtual async Task<IServiceResult> GetWithSpecAsync(ISpecification<TEntity> specification)
		{
			try
			{
				var entity = await _unitOfWork.Repository<TEntity, TKey>().GetWithSpecAsync(specification);

				if (entity == null)
				{
					return new ServiceResult(ResultConst.WARNING_NO_DATA_CODE, ResultConst.WARNING_NO_DATA_MSG);
				}

				return new ServiceResult(ResultConst.SUCCESS_READ_CODE, ResultConst.SUCCESS_READ_MSG, _mapper.Map<TDto>(entity));
			}
			catch (Exception)
			{
				throw;
			}
		}
		
		public async Task<IServiceResult> GetAllWithSpecAsync(ISpecification<TEntity> specification, bool tracked = true)
		{
			try
			{
				var entities = await _unitOfWork.Repository<TEntity, TKey>().GetAllWithSpecAsync(specification, tracked);

				if (!entities.Any())
				{
					return new ServiceResult(ResultConst.WARNING_NO_DATA_CODE, ResultConst.WARNING_NO_DATA_MSG,
						_mapper.Map<IEnumerable<TDto>>(entities));
				}

				return new ServiceResult(ResultConst.SUCCESS_READ_CODE, ResultConst.SUCCESS_READ_MSG,
					_mapper.Map<IEnumerable<TDto>>(entities));
			}
			catch (Exception)
			{
				throw;
			}
		}

		public virtual async Task<IServiceResult> AnyAsync(Expression<Func<TEntity, bool>> predicate)
		{
			try
			{
				var hasAny = await _unitOfWork.Repository<TEntity, TKey>().AnyAsync(predicate);

				if (!hasAny)
				{
					return new ServiceResult(ResultConst.WARNING_NO_DATA_CODE, ResultConst.WARNING_NO_DATA_MSG, false);
				}

				return new ServiceResult(ResultConst.SUCCESS_READ_CODE, ResultConst.SUCCESS_READ_MSG, true);
			}
			catch (Exception)
			{
				throw;
			}
		}

		public async Task<IServiceResult> CountAsync(ISpecification<TEntity> specification)
		{
			try
			{
				var totalEntity = await _unitOfWork.Repository<TEntity, TKey>().CountAsync(specification);
				return new ServiceResult(ResultConst.SUCCESS_READ_CODE, ResultConst.SUCCESS_READ_MSG, totalEntity);
			}
			catch (Exception)
			{
				throw;
			}
		}
	}
}
