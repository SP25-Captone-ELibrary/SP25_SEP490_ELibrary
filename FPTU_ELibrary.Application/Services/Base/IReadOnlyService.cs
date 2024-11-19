namespace FPTU_ELibrary.Application.Services.Base
{
	//	Summary:
	//		This base interface contains common use Read-only operations
	public interface IReadOnlyService<TEntity, TOut, TKey>
		where TEntity : class
		where TOut : class
	{
		Task<IServiceResult> GetAllAsync(bool tracked = true);
		Task<IServiceResult> GetByIdAsync(TKey id);
	}
}
