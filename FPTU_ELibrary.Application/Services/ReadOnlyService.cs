using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Services.Base;
using FPTU_ELibrary.Domain.Interfaces;
using MapsterMapper;

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
                var result = await _unitOfWork.Repository<TEntity, TKey>().GetAllAsync();

                if (!result.Any())
                {
                    return new ServiceResult(ResultConst.FAIL_READ_CODE, ResultConst.FAIL_READ_MSG, 
                        _mapper.Map<IEnumerable<TDto>>(result));
                }

                return new ServiceResult(ResultConst.SUCCESS_READ_CODE, ResultConst.SUCCESS_READ_MSG, 
                    _mapper.Map<IEnumerable<TDto>>(result));
            }
            catch (EntityNotFoundException)
            {
                var message = $"Error retrieving all {typeof(TDto).Name}s";
                return new ServiceResult(ResultConst.ERROR_EXCEPTION_CODE, message);
            }
            catch(Exception e)
            {
                return new ServiceResult(ResultConst.ERROR_EXCEPTION_CODE, e.Message);
            }
        }

        public virtual async Task<IServiceResult> GetByIdAsync(TKey id)
        {
            try
            {
                var result = await _unitOfWork.Repository<TEntity, TKey>().GetByIdAsync(id);

				if (result == null)
				{
					return new ServiceResult(ResultConst.FAIL_READ_CODE, ResultConst.FAIL_READ_MSG);
				}

				return new ServiceResult(ResultConst.SUCCESS_READ_CODE, ResultConst.SUCCESS_READ_MSG, _mapper.Map<TDto>(result));
			}
			catch (EntityNotFoundException)
			{
				var message = $"Error retrieving all {typeof(TDto).Name}s";
				return new ServiceResult(ResultConst.ERROR_EXCEPTION_CODE, message);
			}
			catch (Exception e)
			{
				return new ServiceResult(ResultConst.ERROR_EXCEPTION_CODE, e.Message);
			}
		}
    }
}
