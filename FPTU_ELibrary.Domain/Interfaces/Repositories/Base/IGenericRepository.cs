using FPTU_ELibrary.Domain.Specifications.Interfaces;
using System.Linq.Expressions;

namespace FPTU_ELibrary.Domain.Interfaces.Repositories.Base
{
    public interface IGenericRepository<TEntity, TKey>
        where TEntity :class
    {
        #region READ DATA

        ///  Default Procedures
        Task<IEnumerable<TEntity>> GetAllAsync(bool tracked = true);
        Task<TEntity?> GetByIdAsync(TKey id);

        /// Retrieve with specifications
        Task<TEntity?> GetWithSpecAsync(ISpecification<TEntity> specification);
        Task<IEnumerable<TEntity>> GetAllWithSpecAsync(ISpecification<TEntity> specification, bool tracked = true);
        Task<int> CountAsync(ISpecification<TEntity> specification);

        #endregion

        #region WRITE DATA

        /// Synchronous operation
        void Add(TEntity entity);
        void AddRange(IEnumerable<TEntity> entities);
        void Delete(TKey id);
        void Update(TEntity entity);

        /// Asyncronous operation
        Task AddAsync(TEntity entity);
        Task AddRangeAsync(IEnumerable<TEntity> entities);
        Task DeleteAsync(TKey id);
		Task UpdateAsync(TEntity entity);

        #endregion

        #region OTHERS
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);
        bool HasChanges(TEntity original, TEntity modified);
        bool HasChanges(TEntity entity);
        #endregion
	}
}
