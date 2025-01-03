using Elasticsearch.Net;
using FPTU_ELibrary.API.Mappings;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Elastic.Models;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Nest;
using Serilog;

namespace FPTU_ELibrary.Application.Services
{
	public class ElasticService : IElasticService
	{
		private string _indexName = string.Empty;
		private readonly IConfiguration _configuration;
		private readonly ILogger _logger;
		private readonly IElasticClient _elasticClient;
		private readonly ElasticSettings _elasticSettings;
		private readonly IBookService<BookDto> _bookService;

		public ElasticService(
			IBookService<BookDto> bookService, 
			IElasticClient elasticClient,
			IConfiguration configuration,
			IOptionsMonitor<ElasticSettings> monitor,
			ILogger logger)
        {
	        _elasticSettings = monitor.CurrentValue;
			_bookService = bookService;
			_elasticClient = elasticClient;
			_configuration = configuration;
			_logger = logger;
        }

		private ElasticService Index(string indexName)
		{
			_indexName = indexName;
			return this;
		}
		
        public async Task InitializeAsync()
        {
	        var indexName = ElasticIndexConst.BookIndex;
	        
			// Try to create index if not exist	
			await CreateIndexIfNotExistAsync(indexName);

			// Count any index created 
			var documentCountResponse = await _elasticClient.CountAsync<ElasticBook>(c => c.Index(indexName));
			if(documentCountResponse.Count > 0) // not found
			{
				_logger.Information("Already seed documents for index: {0}", indexName);
				return;
			}
			
			// Try to seed data
			await SeedDataAsync(indexName);
		}
        
        /// <summary>
        /// Try to create index to elastic search 
        /// </summary>
        /// <param name="indexName"></param>
        /// <exception cref="Exception"></exception>
		public async Task<bool> CreateIndexIfNotExistAsync(string indexName)
		{
			try
			{
				// Check whether the specific index name exist or not
				if (!(await _elasticClient.Indices.ExistsAsync(indexName)).Exists)
				{
					// Switch for elastic index
					switch (indexName)
					{
						case ElasticIndexConst.BookIndex:
							// Try to create book index if not exist
							var createResp = await CreateBookIndexAsync();
							// Check for valid resp 
							if (!createResp.IsValid)
							{
								_logger.Error("Error invoke while creating index: {0}", indexName);
								return false;
							}
							break;
					}
				}
				
				// Assign index name
				Index(indexName);
			}	
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when create index to elastic search");
			}

			return true;
		}

		public async Task<bool> DocumentExistsAsync<TDocument>(string documentId)
			where TDocument : class
		{
			try
			{
				// Check whether index existing any document yet
				var response = await _elasticClient.DocumentExistsAsync<TDocument>(documentId, 
					d => d.Index(_indexName));
				return response.Exists;
			}
			catch (Exception ex)
			{
				_logger.Error($"Error checking document existence: {ex.Message}");
				throw new Exception($"Error checking if document with ID {documentId} exists in Elasticsearch");
			}
		}

		public async Task<bool> NestedExistAsync<TDocument>(
			string documentId, string nestedFieldName,
			string nestedKey, string nestedKeyValue
		)
			where TDocument : class
		{
			try
			{
				// Check whether index name is null or empty
				if (string.IsNullOrEmpty(_indexName)) return false;

				// Query to check if the nested object exists
				// GET index_name/_search{"query":{}}
				var queryResponse = await _elasticClient.SearchAsync<TDocument>(s => s
					.Index(_indexName) // Index name
					// Building Query DSL
					.Query(q => q
						.Bool(b => b
							.Must(m => m
									.Term("_id", documentId) // Match document ID
							)
							.Filter(f => f
								.Nested(n => n
									.Path(nestedFieldName)
									.Query(nq => nq
										.Bool(nb => nb
											.Must(
												mq => mq.Term($"{nestedFieldName}.{nestedKey}", int.Parse(nestedKeyValue))
											)
										)
									)
								)
							)
						)
					)
				);

				// Check if the document matches the query
				return queryResponse.Hits.Count > 0;
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception($"Error checking nested object existence in {nestedFieldName} in Elasticsearch");
			}
		}

		public async Task<bool> AddOrUpdateBulkAsync<T>(IEnumerable<T> documents, string? documentKeyName = null) where T : class
		{
			try
			{
				// Check whether index name is null or empty
				if (string.IsNullOrEmpty(_indexName)) return false;

				// Initialize Bulk Descriptor
				var bulkDescriptor = new BulkDescriptor();
				
				// Convert document to list
				var listDocument = documents.ToList();
				// Check whether document key exist
				if (!string.IsNullOrEmpty(documentKeyName))
				{
					var idProperty = typeof(T).GetProperty(documentKeyName);
					if (idProperty == null)
					{
						throw new Exception($"The document does not have a '{documentKeyName}' property");
					}
					
					foreach (var document in listDocument)
					{
						// Override documents key
						var idValue = idProperty.GetValue(document)?.ToString();
						if (string.IsNullOrEmpty(idValue))
						{
							throw new Exception($"{documentKeyName} cannot be null or empty");
						}
						
						bulkDescriptor.Update<T>(op => op
								.Index(_indexName) // Use the default index
								.Id(idValue) // Set the _id using documentKeyName
								.Doc(document) // Set the document to update
								.DocAsUpsert(true) // Use upsert if the document does not exist
						);
					}
					
					var response = await _elasticClient.BulkAsync(bulkDescriptor);
					return response.IsValid;
				}
				
				// Bulk update range data to elastic (POST /index_name/_update_by_query{})
				var indexResponse = await _elasticClient.BulkAsync(b => b
					.Index(_indexName) // index name
					.UpdateMany(listDocument, (ud, d) => ud.Doc(d).DocAsUpsert(true))
				);
				return indexResponse.IsValid;
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when add or update bulk to Elastic Search");
			}
		}
        
		public async Task<bool> AddOrUpdateAsync<T>(T document, string? documentKeyName = null) where T : class
		{
			try
			{
				// Check whether index name is null or empty
				if (string.IsNullOrEmpty(_indexName)) return false;

				if (!string.IsNullOrEmpty(documentKeyName))
				{
					// Try to retrieve document key name
					var idProperty = typeof(T).GetProperty(documentKeyName);
					if (idProperty == null)
					{
						throw new Exception($"The document does not have a '{documentKeyName}' property");
					}
					
					// Try to retrieve key value
					var idValue = idProperty.GetValue(document)?.ToString();
					if (string.IsNullOrEmpty(idValue))
					{
						throw new Exception($"{documentKeyName} cannot be null or empty");
					}
					
					// Index document (PUT /index_name/_doc{})
					var indexWithIdResponse = await _elasticClient.IndexAsync(document, idx => 
						idx.Index(_indexName)
							.OpType(OpType.Index)
							.Id(idValue)); // With specific _id value
					return indexWithIdResponse.IsValid;
				}
				
				// Index document (PUT /index_name/_doc{})
				var indexResponse = await _elasticClient.IndexAsync(document, idx => 
					idx.Index(_indexName).OpType(OpType.Index));
				return indexResponse.IsValid;
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process add or update to Elastic Search");
			}
		}
		
		public async Task<bool> AddOrUpdateNestedAsync<TDocument, TNested>(
			string documentId, // In specific document
			string nestedFieldName, // With specific nested object field name
			TNested nestedObject, // Nested object data
			string nestedKey, // Nested key
			string nestedKeyValue // Nested key value
		)
			where TDocument : class
			where TNested : class
		{
			try
			{
				// Check whether index name is null or empty
				if (string.IsNullOrEmpty(_indexName)) return false;

				// Script to add or update the nested object
				var script = $@"
		            if (ctx._source.{nestedFieldName} == null) {{
		                ctx._source.{nestedFieldName} = [params.newNestedObject];
		            }} else {{
		                int index = -1;
					    for (int i = 0; i < ctx._source.{nestedFieldName}.size(); i++) {{
					        if (ctx._source.{nestedFieldName}[i].{nestedKey} == params.nestedKeyValue) {{
					            index = i;
					            break;
					        }}
					    }}
					    if (index != -1) {{
					        ctx._source.{nestedFieldName}[index] = params.newNestedObject;
					    }} else {{
					        ctx._source.{nestedFieldName}.add(params.newNestedObject);
					    }}
		            }}
		        ";
			
				var response = await _elasticClient.UpdateAsync<TDocument>(documentId, u => u
					.Index(_indexName) // Index name
					.Script(s => s
						// Add script 
						.Source(script) 
						// Add params
						.Params(p => p
							// Add or update nested object
							.Add("newNestedObject", nestedObject)
							// With specific unique value
							.Add("nestedKeyValue", int.Parse(nestedKeyValue))
						)
					)
				);
				
				return response.IsValid;
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception($"Error adding or updating nested object in {nestedFieldName} in Elasticsearch");
			}
		}
		
		public async Task<bool> DeleteNestedAsync<TDocument>(
            string documentId, // In specific document
            string nestedFieldName, // With specific nested object field name
            string nestedKey, // Nested key
            string nestedKeyValue // Nested key value
        )
            where TDocument : class
        {
            try
            {
                // Check whether index name is null or empty
                if (string.IsNullOrEmpty(_indexName)) return false;
        
                // Script to delete the nested object
                var script = $@"
                    if (ctx._source.{nestedFieldName} != null) {{
                        ctx._source.{nestedFieldName}.removeIf(nestedObj -> nestedObj.{nestedKey} == params.nestedKeyValue);
                    }}
                ";
        
                var response = await _elasticClient.UpdateAsync<TDocument>(documentId, u => u
                    .Index(_indexName) // Index name
                    .Script(s => s
                        // Add script
                        .Source(script)
                        // Add params
                        .Params(p => p
                            // Specify the key value of the nested object to delete
                            .Add("nestedKeyValue", int.Parse(nestedKeyValue))
                        )
                    )
                );
        
                return response.IsValid;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw new Exception($"Error deleting nested object from {nestedFieldName} in Elasticsearch");
            }
        }
		
		public async Task<T?> GetAsync<T>(string key) where T : class
		{
			try
			{
				// Check whether index name is null or empty
                if (string.IsNullOrEmpty(_indexName)) return null;
				                
				var response = await _elasticClient.GetAsync<T>(key, g => g.Index(_indexName));
				return response.Source;
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process get data from Elastic Search");
			}
		}
		
		public async Task<List<T>?> GetAllAsync<T>() where T : class
		{
			try
			{
				// Check whether index name is null or empty
				if (string.IsNullOrEmpty(_indexName)) return null;
				
				var searchResponse = await _elasticClient
					// Search descriptor
					.SearchAsync<T>(s => s.Index(_indexName)
						// Building query descriptor
						.Query(q => 
							// Match all conditions
							q.MatchAll()));
				return searchResponse.IsValid ? searchResponse.Documents.ToList() : default;
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process get all data from Elastic Search");
			}
		}
		
		public async Task<bool> DeleteAsync<T>(string key) where T : class
		{
			try
			{
				// Check whether index name is null or empty
				if (string.IsNullOrEmpty(_indexName)) return false;
				
				var response = await _elasticClient.DeleteAsync<T>(
					id: key, // with specific ID 
					selector: d => d.Index(_indexName)); // with index name
				return response.IsValid;
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process remove data from Elastic Search");
			}
		}
		
		public async Task<long> DeleteAllAsync<T>() where T : class
		{
			try
			{
				// Check whether index name is null or empty
				if (string.IsNullOrEmpty(_indexName)) return 0;
				
				// Process delete all
				var response = await _elasticClient.DeleteByQueryAsync<T>(d => 
					d.Index(_indexName) // with specific index name
						// Match all query
						.Query(q => q.MatchAll()));
				return response.Deleted;
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process remove all data from Elastic Search");
			}
		}
		
		private async Task<CreateIndexResponse> CreateBookIndexAsync()
		{
			// PUT request to indices
            return await _elasticClient.Indices.CreateAsync(ElasticIndexConst.BookIndex, c => c
                // Define mappings for index
                .Map<ElasticBook>(m => m
                    // Mapping properties
                    .Properties(ps => ps
                    	.Number(n => n.Name(e => e.BookId).Type(NumberType.Integer))
                    	.Text(t => t.Name(e => e.Title))
                    	.Text(t => t.Name(e => e.SubTitle))
                    	.Text(t => t.Name(e => e.Summary))
                    	.Boolean(b => b.Name(e => e.IsDeleted))
                    	.Date(d => d.Name(e => e.CreatedAt))
                    	.Date(d => d.Name(e => e.UpdatedAt))
                    	.Keyword(k => k.Name(e => e.CreatedBy))
                    	.Keyword(k => k.Name(e => e.UpdatedBy))
                    	// Mapping book category
                    	.Nested<ElasticCategory>(o => o
                    		.Name(e => e.Categories)
                    		.Properties(op => op
                    			.Number(n => n.Name(ec => ec.CategoryId).Type(NumberType.Integer))
                    			.Text(t => t.Name(ec => ec.EnglishName))
                    			.Text(t => t.Name(ec => ec.VietnameseName))
                    			.Text(t => t.Name(ec => ec.Description))
                    		)
                    	)
                    	// Mapping book edition as nested documents
                    	.Nested<ElasticBookEdition>(n => n
                    		.Name(e => e.BookEditions)
                    		.Properties(np => np
                    			.Number(nn => nn.Name(ee => ee.BookEditionId).Type(NumberType.Integer))
                    			.Number(nn => nn.Name(ee => ee.BookId).Type(NumberType.Integer))
                    			.Text(t => t.Name(ee => ee.EditionTitle))
                    			.Text(t => t.Name(ee => ee.EditionSummary))
                    			.Number(k => k.Name(ee => ee.EditionNumber).Type(NumberType.Integer))
                    			.Number(nn => nn.Name(ee => ee.PublicationYear).Type(NumberType.Integer))
                    			.Number(nn => nn.Name(ee => ee.PageCount).Type(NumberType.Integer))
                    			.Keyword(k => k.Name(ee => ee.Language))
                    			.Keyword(k => k.Name(ee => ee.CoverImage))
                    			.Keyword(k => k.Name(ee => ee.Format))
                    			.Keyword(k => k.Name(ee => ee.Status))
                    			.Text(t => t.Name(ee => ee.Publisher))
                    			.Keyword(k => k.Name(ee => ee.Isbn))
                    			.Boolean(b => b.Name(ee => ee.IsDeleted))
                    			.Boolean(b => b.Name(ee => ee.CanBorrow))
                    			.Date(d => d.Name(ee => ee.CreatedAt))
                    			.Date(d => d.Name(ee => ee.UpdatedAt))
                    			.Keyword(k => k.Name(ee => ee.CreatedBy))
                    			.Keyword(k => k.Name(ee => ee.UpdatedBy))
                    			// Mapping author as nested documents
                    			.Nested<ElasticAuthor>(n2 => n2
                    				.Name(e => e.Authors)
                    				.Properties(np2 => np2
                    					.Number(nn => nn.Name(ee => ee.AuthorId).Type(NumberType.Integer))
                    					.Keyword(k => k.Name(ee => ee.AuthorCode))
                    					.Keyword(k => k.Name(ee => ee.AuthorImage))
                    					.Text(t => t.Name(ee => ee.FullName))
                    					.Text(t => t.Name(ee => ee.Biography))
                    					.Date(d => d.Name(ee => ee.Dob))
                    					.Date(d => d.Name(ee => ee.DateOfDeath))
                    					.Keyword(k => k.Name(ee => ee.Nationality))
                    					.Date(d => d.Name(ee => ee.CreateDate))
                    					.Date(d => d.Name(ee => ee.UpdateDate))
                    					.Boolean(b => b.Name(ee => ee.IsDeleted))
                    				)
                    			)
                    		)
                    	)
                    )
                )
            );
		}

		private async Task SeedDataAsync(string indexName)
		{
			// Create book filtering specification
            BaseSpecification<Book> spec = new();
            // Enables split query
            spec.EnableSplitQuery();
            // Add includes 
            spec.ApplyInclude(q => q
            	// Include book categories
            	.Include(b => b.BookCategories)
            		// Then include specific category
            		.ThenInclude(cat => cat.Category)
            	// Include book editions
            	.Include(b => b.BookEditions)
            		// Then include book edition authors
            		.ThenInclude(bea => bea.BookEditionAuthors)
            			// Then include specific author
            			.ThenInclude(bea => bea.Author));
            
            // Get all books with specification
            var getBookResp = await _bookService.GetAllWithSpecAsync(spec);
        
            // Check success result
            if(getBookResp.Data != null && getBookResp.Data is List<BookDto> books)
            {
            	// Map to elastic list object
            	var elasticBooks = books?.Select(b => b.ToElasticBook());
            	var toBookList = elasticBooks?.ToList();
            	// Check exist
            	if (toBookList != null && toBookList.Any())
            	{
            		// Process Bulk API to seeding data to elastic
            		var bulkResp = await _elasticClient.BulkAsync(x => x
            			// Default index name
            			.Index(indexName)
            			// Index with many books
			            // .IndexMany(toBookList));
            			.IndexMany(toBookList, (descriptor, elasticBook) => descriptor
            				// Assign BookId as elastic document key '_id'
            				.Id(elasticBook.BookId)));

            		if (bulkResp.IsValid && bulkResp.Items.Any())
            		{
            			_logger.Information("Bulk {0} records for index: {1}", bulkResp.Items.Count, indexName);
            		}
            		else
            		{
            			_logger.Error("Fail to bulk data for index: {0}", indexName);
            		}
            	}
            }
		}
	}
}
