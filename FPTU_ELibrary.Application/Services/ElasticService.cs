using Elasticsearch.Net;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Elastic.Mappers;
using FPTU_ELibrary.Application.Elastic.Models;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Nest;
using Serilog;

namespace FPTU_ELibrary.Application.Services
{
	public class ElasticService : IElasticService
	{
		private string _indexName = string.Empty;
		
		private readonly ILogger _logger;
		private readonly IElasticClient _elasticClient;
		private readonly IMapper _mapper;
		private readonly ILibraryItemService<LibraryItemDto> _libItemService;

		public ElasticService(
			ILibraryItemService<LibraryItemDto> libItemService,
			IElasticClient elasticClient,
			IConfiguration configuration,
			IOptionsMonitor<ElasticSettings> monitor,
			IMapper mapper,
			ILogger logger)
		{
			_libItemService = libItemService;
			_elasticClient = elasticClient;
			_mapper = mapper;
			_logger = logger;
        }

		private ElasticService Index(string indexName)
		{
			_indexName = indexName;
			return this;
		}
		
        public async Task InitializeAsync()
        {
	        var indexName = ElasticIndexConst.LibraryItemIndex;
	        
			// Try to create index if not exist	
			await CreateIndexIfNotExistAsync(indexName);

			// Count any index created 
			var documentCountResponse = await _elasticClient.CountAsync<ElasticLibraryItem>(c => c.Index(indexName));
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
						case ElasticIndexConst.LibraryItemIndex:
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
            return await _elasticClient.Indices.CreateAsync(ElasticIndexConst.LibraryItemIndex, c => c
	            .Settings(s => s
		            .Analysis(a => a
			            .Analyzers(an => an
				            // Analyzer for exact matching (keeps special characters)
				            .Custom("exact_match_analyzer", ea => ea
					            .Tokenizer("standard") // Use standard tokenizer
					            .Filters("lowercase") // Case-insensitive
				            )
				            // Analyzer for non-special matching
				            .Custom("non_special_match_analyzer", nsa => nsa
					            .Tokenizer("standard") // Use standard tokenizer
					            .Filters("lowercase", "asciifolding") // Case-insensitive and removes accents
				            )
			            )
		            )
	            )
	            // Define mappings for index
                .Map<ElasticLibraryItem>(m => m
                    // Mapping properties
                    .Properties(ps => ps
                    	.Number(n => n.Name(e => e.LibraryItemId).Type(NumberType.Integer))
                    	.Number(n => n.Name(e => e.EditionNumber).Type(NumberType.Integer))
                    	.Number(n => n.Name(e => e.PublicationYear).Type(NumberType.Integer))
                    	.Number(n => n.Name(e => e.PageCount).Type(NumberType.Integer))
                    	.Number(n => n.Name(e => e.CategoryId).Type(NumberType.Integer))
                    	.Number(n => n.Name(e => e.ShelfId).Type(NumberType.Integer))
                    	.Number(n => n.Name(e => e.GroupId).Type(NumberType.Integer))
                    	.Number(n => n.Name(e => e.EstimatedPrice).Type(NumberType.Double))
                    	.Text(t => t
		                    .Name(e => e.Title)
		                    .Fields(ff => ff
			                    .Text(tt => tt
				                    .Name("exact") // Exact match field
				                    .Analyzer("exact_match_analyzer")
			                    )
			                    .Text(tt => tt
				                    .Name("non_special") // Non-special match field
				                    .Analyzer("non_special_match_analyzer")
			                    )
		                    )
		                )
                    	.Text(t => t
		                    .Name(e => e.SubTitle)
		                    .Fields(ff => ff
			                    .Text(tt => tt
				                    .Name("exact") // Exact match field
				                    .Analyzer("exact_match_analyzer")
			                    )
			                    .Text(tt => tt
				                    .Name("non_special") // Non-special match field
				                    .Analyzer("non_special_match_analyzer")
			                    )
		                    )
	                    )
                    	.Text(t => t
							.Name(e => e.Genres)
							.Fields(ff => ff
								.Text(tt => tt
									.Name("exact") // Exact match field
									.Analyzer("exact_match_analyzer")
								)
								.Text(tt => tt
									.Name("non_special") // Non-special match field
									.Analyzer("non_special_match_analyzer")
								)
							)
	                    )
                    	.Text(t => t
							.Name(e => e.TopicalTerms)
							.Fields(ff => ff
								.Text(tt => tt
									.Name("exact") // Exact match field
									.Analyzer("exact_match_analyzer")
								)
								.Text(tt => tt
									.Name("non_special") // Non-special match field
									.Analyzer("non_special_match_analyzer")
								)
							)
	                    )
                    	.Text(t => t.Name(e => e.Responsibility))
                    	.Text(t => t.Name(e => e.Edition))
                    	.Text(t => t.Name(e => e.Summary))
                    	.Keyword(t => t.Name(e => e.Language))
                    	.Keyword(t => t.Name(e => e.OriginLanguage))
                    	.Keyword(t => t.Name(e => e.CoverImage))
                    	.Keyword(t => t.Name(e => e.Status))
                    	.Text(t => t.Name(e => e.ClassificationNumber))
                    	.Text(t => t.Name(e => e.CutterNumber))
                    	.Text(t => t.Name(e => e.Isbn))
                    	.Text(t => t.Name(e => e.Ean))
                    	.Text(t => t.Name(e => e.PhysicalDetails))
                    	.Text(t => t.Name(e => e.Dimensions))
                    	.Boolean(b => b.Name(e => e.IsDeleted))
                    	.Boolean(b => b.Name(e => e.CanBorrow))
                    	.Boolean(b => b.Name(e => e.IsTrained))
	                    // Mapping book edition inventory as object
	                    .Object<ElasticLibraryItemInventory>(ob => ob
		                    .Name(e => e.LibraryItemInventory)
		                    .Properties(np4 => np4
			                    .Number(nn => nn.Name(ee => ee.LibraryItemId).Type(NumberType.Integer))
			                    .Number(nn => nn.Name(ee => ee.TotalUnits).Type(NumberType.Integer))
			                    .Number(nn => nn.Name(ee => ee.AvailableUnits).Type(NumberType.Integer))
			                    .Number(nn => nn.Name(ee => ee.RequestUnits).Type(NumberType.Integer))
			                    .Number(nn => nn.Name(ee => ee.BorrowedUnits).Type(NumberType.Integer))
			                    .Number(nn => nn.Name(ee => ee.ReservedUnits).Type(NumberType.Integer))
		                    )
	                    )
	                    // Mapping edition copy as nested documents
	                    .Nested<ElasticLibraryItemInstance>(n1 => n1
		                    .Name(e => e.LibraryItemInstances)
		                    .Properties(np1 => np1
			                    .Number(nn => nn.Name(ee => ee.LibraryItemInstanceId).Type(NumberType.Integer))
			                    .Number(nn => nn.Name(ee => ee.LibraryItemId).Type(NumberType.Integer))
			                    .Text(t => t.Name(ee => ee.Barcode))
			                    .Keyword(k => k.Name(ee => ee.Status))
			                    .Boolean(b => b.Name(ee => ee.IsDeleted))
		                    )
	                    )
	                    // Mapping author as nested documents
	                    .Nested<ElasticAuthor>(n2 => n2
		                    .Name(e => e.Authors)
		                    .Properties(np2 => np2
			                    .Number(nn => nn.Name(ee => ee.AuthorId).Type(NumberType.Integer))
			                    .Keyword(k => k.Name(ee => ee.AuthorImage))
			                    .Text(t => t.Name(ee => ee.AuthorCode))
			                    .Text(t => t
				                    .Name(ee => ee.FullName)
				                    .Fields(ff => ff
					                    .Text(tt => tt
						                    .Name("exact") // Exact match field
						                    .Analyzer("exact_match_analyzer")
					                    )
					                    .Text(tt => tt
						                    .Name("non_special") // Non-special match field
						                    .Analyzer("non_special_match_analyzer")
					                    )
				                    )
			                    )
			                    .Text(t => t.Name(ee => ee.Biography))
			                    .Date(d => d.Name(ee => ee.Dob))
			                    .Date(d => d.Name(ee => ee.DateOfDeath))
			                    .Keyword(k => k.Name(ee => ee.Nationality))
			                    .Boolean(b => b.Name(ee => ee.IsDeleted))
		                    )
	                    )
                    )
                )
            );
		}

		private async Task SeedDataAsync(string indexName)
		{
		   // Build specification 
		   BaseSpecification<LibraryItem> spec = new();
		   // Enables split query
		   spec.EnableSplitQuery();
		   
		   var getLibraryItemsRes = await _libItemService
                .GetAllWithSpecAndSelectorAsync(spec, be => new LibraryItem()
                {
                    LibraryItemId = be.LibraryItemId,
                    Title = be.Title,
                    SubTitle = be.SubTitle,
                    Responsibility = be.Responsibility,
                    Edition = be.Edition,
                    EditionNumber = be.EditionNumber,
                    Language = be.Language,
                    OriginLanguage = be.OriginLanguage,
                    Summary = be.Summary,
                    CoverImage = be.CoverImage,
                    PublicationYear = be.PublicationYear,
                    Publisher = be.Publisher,
                    PublicationPlace = be.PublicationPlace,
                    ClassificationNumber = be.ClassificationNumber,
                    CutterNumber = be.CutterNumber,
                    Isbn = be.Isbn,
                    Ean = be.Ean,
                    EstimatedPrice = be.EstimatedPrice,
                    PageCount = be.PageCount,
                    PhysicalDetails = be.PhysicalDetails,
                    Dimensions = be.Dimensions,
                    AccompanyingMaterial = be.AccompanyingMaterial,
                    Genres = be.Genres,
                    GeneralNote = be.GeneralNote,
                    BibliographicalNote = be.BibliographicalNote,
                    TopicalTerms = be.TopicalTerms,
                    AdditionalAuthors = be.AdditionalAuthors,
                    CategoryId = be.CategoryId,
                    ShelfId = be.ShelfId,
                    GroupId = be.GroupId,
                    Status = be.Status,
                    IsDeleted = be.IsDeleted,
                    IsTrained = be.IsTrained,
                    CanBorrow = be.CanBorrow,
                    TrainedAt = be.TrainedAt,
                    CreatedAt = be.CreatedAt,
                    UpdatedAt = be.UpdatedAt,
                    UpdatedBy = be.UpdatedBy,
                    CreatedBy = be.CreatedBy,
                    // References
                    Category = be.Category,
                    Shelf = be.Shelf,
                    LibraryItemGroup = be.LibraryItemGroup,
                    LibraryItemInventory = be.LibraryItemInventory,
                    LibraryItemInstances = be.LibraryItemInstances,
                    LibraryItemReviews = be.LibraryItemReviews,
                    LibraryItemAuthors = be.LibraryItemAuthors.Select(ba => new LibraryItemAuthor()
                    {
                        LibraryItemAuthorId = ba.LibraryItemAuthorId,
                        LibraryItemId = ba.LibraryItemId,
                        AuthorId = ba.AuthorId,
                        Author = ba.Author
                    }).ToList()
                });
		   
		   // Check success result
		   if (getLibraryItemsRes.Data != null && getLibraryItemsRes.Data is List<LibraryItem> libraryItems) {
			   // Convert to dtos (exclude all draft item)
			   var libraryItemDtos = _mapper.Map<List<LibraryItemDto>>(libraryItems.Where(li => 
				   !Equals(li.Status.ToString(), LibraryItemStatus.Draft.ToString())).ToList());
			   // Map to elastic list object
			   var elasticItems = libraryItemDtos.Select(b => b.ToElasticLibraryItem());
			   var toItemList = elasticItems?.ToList();
			   // Check exist
			   if (toItemList != null && toItemList.Any()) {
				   // Process Bulk API to seeding data to elastic
				   var bulkResp = await _elasticClient.BulkAsync(x => x
					   // Default index name
					   .Index(indexName)
					   // Index with many items
					   // .IndexMany(toItemList));
					   .IndexMany(toItemList, (descriptor, elasticBook) => descriptor
						   // Assign BookId as elastic document key '_id'
						   .Id(elasticBook.LibraryItemId)));

				   if (bulkResp.IsValid && bulkResp.Items.Any()) {
					   _logger.Information("Bulk {0} records for index: {1}", bulkResp.Items.Count, indexName);
				   } else {
					   _logger.Error("Fail to bulk data for index: {0}", indexName);
				   }
			   }
		   }
		}
	}
}
