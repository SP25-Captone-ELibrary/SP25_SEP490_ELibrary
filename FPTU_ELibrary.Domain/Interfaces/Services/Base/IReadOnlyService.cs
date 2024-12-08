using FPTU_ELibrary.Domain.Specifications.Interfaces;
using System.Linq.Expressions;

namespace FPTU_ELibrary.Domain.Interfaces.Services.Base
{
	//	Summary:
	//		This base interface contains common use Read-only operations
	public interface IReadOnlyService<TEntity, TDto, TKey>
		where TEntity : class
		where TDto : class
	{
		Task<IServiceResult> GetByIdAsync(TKey id);
		Task<IServiceResult> GetAllAsync(bool tracked = true);
		Task<IServiceResult> GetWithSpecAsync(ISpecification<TEntity> specification);
		Task<IServiceResult> GetAllWithSpecAsync(ISpecification<TEntity> specification, bool tracked = true);
		Task<IServiceResult> AnyAsync(Expression<Func<TEntity, bool>> predicate);
		Task<IServiceResult> CountAsync(ISpecification<TEntity> specification);
		Task<IServiceResult> CountAsync();
	}
}
