namespace FPTU_ELibrary.Domain.Interfaces.Services
{
	public interface IElasticService
	{
		Task InitializeAsync();
		Task<bool> CreateIndexIfNotExistAsync(string indexName);
		Task<bool> DocumentExistsAsync<TDocument>(string documentId) where TDocument : class;
		Task<bool> NestedExistAsync<TDocument>(string documentId, string nestedFieldName,
			string nestedKey, string nestedKeyValue) where TDocument : class;
		Task<bool> AddOrUpdateBulkAsync<T>(IEnumerable<T> documents, string? documentKeyName = null) where T : class;
		Task<bool> AddOrUpdateAsync<T>(T document, string? documentKeyName = null) where T : class;
		Task<bool> AddOrUpdateNestedAsync<TDocument, TNested>(
			string documentId, string nestedFieldName,
			TNested nestedObject, string nestedKey, string nestedKeyValue) where TDocument : class where TNested : class;
		Task<T?> GetAsync<T>(string key) where T : class;
		Task<List<T>?> GetAllAsync<T>() where T : class;
		Task<bool> DeleteAsync<T>(string key) where T : class;
		Task<long> DeleteAllAsync<T>() where T : class;
		Task<bool> DeleteNestedAsync<TDocument>(string documentId, string nestedFieldName,
			string nestedKey, string nestedKeyValue) where TDocument : class;
	}
}
