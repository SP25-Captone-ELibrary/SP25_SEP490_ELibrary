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
		Task<IServiceResult> GetWithSpecAsync(ISpecification<TEntity> specification, bool tracked = true);
		Task<IServiceResult> GetAllWithSpecAsync(ISpecification<TEntity> specification, bool tracked = true);
		Task<IServiceResult> GetWithSpecAndSelectorAsync<TResult>(ISpecification<TEntity> specification,
			Expression<Func<TEntity, TResult>> selector, bool tracked = true);
		Task<IServiceResult> GetAllWithSpecAndSelectorAsync<TResult>(ISpecification<TEntity> specification,
			Expression<Func<TEntity, TResult>> selector,
			bool tracked = true);
		Task<IServiceResult> AnyAsync(Expression<Func<TEntity, bool>> predicate);
		Task<IServiceResult> AnyAsync(ISpecification<TEntity> specification);
		Task<IServiceResult> SumAsync(Expression<Func<TEntity, int>> predicate);
		Task<IServiceResult> SumWithSpecAsync(ISpecification<TEntity> specification, Expression<Func<TEntity, int>> predicate);
		Task<IServiceResult> CountAsync(ISpecification<TEntity> specification);
		Task<IServiceResult> CountAsync();
	}
}
