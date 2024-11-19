namespace FPTU_ELibrary.Application.Services.Base
{
	//	Summary:
	//		This base interface contains both
	//		Write-only operations (Create, Update, Delete)
	//      Read-only operations
	public interface IGenericService<TEntity, TDto, TKey> : IReadOnlyService<TEntity, TDto, TKey>
		where TEntity : class
		where TDto : class
	{
		Task<IServiceResult> CreateAsync(TDto dto);
		Task<IServiceResult> UpdateAsync(TKey id, TDto dto);
		Task<IServiceResult> DeleteAsync(TKey id);
	}
}
