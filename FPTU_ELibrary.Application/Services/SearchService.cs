using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Elastic.Mappers;
using FPTU_ELibrary.Application.Elastic.Models;
using FPTU_ELibrary.Application.Elastic.Params;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.Extensions.Options;
using Nest;

namespace FPTU_ELibrary.Application.Services
{
	public class SearchService : ISearchService
	{
		private readonly IElasticClient _elasticClient;
		private readonly AppSettings _appSettings;
		private readonly ISystemMessageService _msgService;

		public SearchService(
			IElasticClient elasticClient,
			ISystemMessageService msgService,
			IOptionsMonitor<AppSettings> monitor)
		{
			_msgService = msgService;
			_elasticClient = elasticClient;
			_appSettings = monitor.CurrentValue;
		}

		public async Task<IServiceResult> SearchItemAsync(SearchItemParameters parameters,
			CancellationToken cancellationToken)
		{
			var mustClauses = new List<Func<QueryContainerDescriptor<ElasticLibraryItem>, QueryContainer>>();

			// Search fields
			var searchText = parameters.SearchText;
			var searchWithKeyword = parameters.SearchWithKeyword;
			var isMatchExact = parameters.IsMatchExact;

			// Determine the suffix based on the searchWithSpecial flag
			var fieldSuffix = parameters.SearchWithSpecial ? "exact" : "non_special";

			// Pagination
			var pageIndex = parameters.PageIndex;
			var pageSize = parameters.PageSize > 0 ? parameters.PageSize : _appSettings.PageSize;
			var maxTotalPage = (int)Math.Ceiling((double) await CountTotalActualItemAsync() / pageSize);
			// Validate pagination 
			if (parameters.PageIndex < 1) pageIndex = 1;
			if (parameters.PageIndex > maxTotalPage) pageIndex = maxTotalPage;
			
			// Apply filters
			if (parameters.F != null && parameters.F.Any()) // when exist at least 1 field
			{
				// Convert to advanced filter list
				var filerList = parameters.FromParamsToListAdvancedFilter();
				if (filerList != null)
				{
					// Iterate each advanced filter field to determine specific logic 
					foreach (var filter in filerList)
					{
						// Is Categories				
						if (filter.FieldName.ToLowerInvariant() ==
						    nameof(ElasticLibraryItem.CategoryId).ToLowerInvariant())
						{
							var categoryIds = filter.Value?.Split(",")
								.Select(x => x.Trim()) // Eliminate white space
								.Select(x => int.TryParse(x, out int i) ? i : 0) // Convert to integer 
								.ToList();
							if (categoryIds != null)
							{
								// Determine filter operator
								switch (filter.Operator)
								{
									case FilterOperator.Includes:
										// Add 'Includes' logic to must clauses
										mustClauses.Add(m => m
											// Query directly on object fields
											.Bool(b => b
													// "should": Query clauses should match
													.Should(categoryIds.Select(categoryId =>
														// From each category found
														(Func<QueryContainerDescriptor<ElasticLibraryItem>,
															QueryContainer>)(sc => sc
															// Term query
															.Term(t => t
																// With specific field
																.Field(f => f.CategoryId)
																// With specific value
																.Value(categoryId)
															)
														)
													).ToArray()) // Convert to array query containers inside single must query
											)
										);
										break;
									case FilterOperator.Equals:
										mustClauses.Add(m => m
											.Bool(b => b
												.Must(categoryIds.Select(categoryId =>
													(Func<QueryContainerDescriptor<ElasticLibraryItem>, QueryContainer>)
													(sc => sc
														.Term(t => t
															.Field(f => f.CategoryId)
															.Value(categoryId)
														)
													)
												).ToArray())
											)
										);
										break;
									case FilterOperator.NotEqualsTo:
										mustClauses.Add(m => m
											.Bool(b => b
												.MustNot(categoryIds.Select(categoryId =>
													(Func<QueryContainerDescriptor<ElasticLibraryItem>, QueryContainer>)
													(sc => sc
														.Term(t => t
															.Field(f => f.CategoryId)
															.Value(categoryId)
														)
													)
												).ToArray())
											)
										);
										break;
								}
							}
						}

						// Is Status
						if (filter.FieldName.ToLowerInvariant() ==
						    nameof(ElasticLibraryItem.Status).ToLowerInvariant())
						{
							// Try to parse filtering status value to typeof(BookEditionStatus)
							if (Enum.Parse(typeof(LibraryItemStatus), filter.Value ?? string.Empty)
							    is LibraryItemStatus status)
							{
								// Determine operator 
								switch (filter.Operator)
								{
									case FilterOperator.Equals:
										// Add must clause
										mustClauses.Add(m => m
											// "bool": Query with boolean logic 
											.Bool(b => b
												// "must": Query clauses must match
												.Must(mm => mm
													// Term query
													.Term(t => t
														// With specific field
														.Field(f => f.Status)
														// With specific value
														.Value(status.ToString())
													)
												)
											)
										);
										break;
									case FilterOperator.NotEqualsTo:
										// Add must clause
										mustClauses.Add(m => m
											// "bool": Query with boolean logic 
											.Bool(b => b
												// "must_not": Query clauses must not match
												.MustNot(mm => mm
													// Term query
													.Term(t => t
														// With specific field
														.Field(f => f.Status)
														// With specific value
														.Value(status.ToString())
													)
												)
											)
										);
										break;
								}
							}
						}
					}
				}
			}

			// Set default search text as empty (if not exist)
			if (string.IsNullOrEmpty(parameters.SearchText)) searchText = string.Empty;

			// TODO: Implement basic filtering
			// TODO: Implement advanced filtering
			// if (parameters.MaxPageCount.HasValue)
			// {
			// 	mustClauses.Add(m => m
			// 		.Nested(nst => nst
			// 			.Path(p => p.BookEditions)
			// 			.Query(q => q
			// 				.Bool(b => b
			// 					.Must(must => must
			// 						.Range(r => r
			// 							.Field(f => f.BookEditions.First().PageCount)
			// 							.LessThanOrEquals(parameters.MaxPageCount)
			// 						)
			// 					)
			// 				)
			// 			)
			// 		)
			// 	);
			// }
			//
			// if (parameters.PublicationYear.HasValue)
			// {
			// 	mustClauses.Add(m => m
			// 		.Nested(nst => nst
			// 			.Path(p => p.BookEditions)
			// 			.Query(q => q
			// 				.Bool(b => b
			// 					.Must(must => must
			// 						.Term(t => t
			// 							// .Field(f => f.BookEditions.First().PublicationYear)
			// 							// .Value(parameters.PublicationYear)
			// 						)
			// 					)
			// 				)
			// 			)
			// 		)
			// 	);
			// }
			//
			// if (!string.IsNullOrEmpty(parameters.Languages))
			// {
			// 	mustClauses.Add(m => m
			// 		.Nested(nst => nst
			// 			.Path(p => p.BookEditions)
			// 			.Query(q => q
			// 				.Bool(b => b
			// 					.Must(must => must
			// 						.Match(mt => mt
			// 							.Field(f => f.BookEditions.First().Language)
			// 							.Query(parameters.Languages)
			// 						)
			// 					)
			// 				)
			// 			)
			// 		)
			// 	);
			// }

			// Initialize search query statement
			Func<QueryContainerDescriptor<ElasticLibraryItem>, QueryContainer>? queryFunc = null;
			// Initialize search response
			ISearchResponse<ElasticLibraryItem>? searchResp = null;

			// Base search
			if (!string.IsNullOrEmpty(searchText)) // Try to build-up elastic query if search text not null or empty
			{
				if (searchWithKeyword == null) // Search without specific keyword
				{
					// Build-up query for search with text (GET index_name/_search)
					queryFunc = q => q
						// "bool": Querying with boolean logic <- Allow to mix multiple conditions (Should, Must, Filter, etc.)	
						.Bool(b => b
							// "filter": query clauses are required to match, but will not contribute to 
							// relevance score. Query clauses may therefore be cached for improve performance
							.Filter(mustClauses)
							// "should": Query clauses should match. Relevance scores of matching documents are boosted
							// for each matching query clause. Behaviour can be adjusted with minimum_should_match
							.Should(s => s
									// Search with multiple fields ("multi_match") - with fuzziness 
									.MultiMatch(m => m
										// "fields": ["title", "sub_title"]
										.Fields(f => f
												// Add field suffix for specific context (search with special or non-special characters) 
												.Field(ff => ff.Title.Suffix(fieldSuffix),
													boost: 2) // Boosting relevance score if match 
												.Field(ff => ff.SubTitle.Suffix(fieldSuffix),
													boost: 1.5) // Boosting relevance score if match
										)
										// "query": "search_text"
										.Query(searchText)
										// "fuzziness": 1 | 0
										.Fuzziness(GetFuzziness(searchText)) // Disable fuzziness for short queries
										// Rewarding documents where multiple fields match with tie_breaker param
										// Each matching field affects the relevance score
										// "tie_breaker": 0.3
										.TieBreaker(0.3)
										// Determine query operator 
										.Operator(isMatchExact ? Operator.And : Operator.Or)
									),
								s => s
									// Search with multiple fields ("multi_match") - without fuzziness  
									.MultiMatch(m => m
										// "fields": ["edition", "summary", "responsibility", "publication_place", etc.]
										.Fields(f => f
												.Field(ff => ff.Edition)
												.Field(ff => ff.Summary)
												.Field(ff => ff.Responsibility)
												.Field(ff => ff.Publisher)
												.Field(ff => ff.PublicationPlace)
												.Field(ff => ff.ClassificationNumber)
												.Field(ff => ff.CutterNumber)
												.Field(ff => ff.GeneralNote)
												.Field(ff => ff.Isbn)
												.Field(ff => ff.Genres,
													boost: 0.5) // Boosting relevance score if match
												.Field(ff => ff.TopicalTerms,
													boost: 0.5) // Boosting relevance score if match
												.Field(ff => ff.AdditionalAuthors,
													boost: 0.5) // Boosting relevance score if match
										)
										// "query": "search_text"
										.Query(searchText)
										// "fuzziness": 0
										.Fuzziness(Fuzziness.EditDistance(0)) // Disable fuzziness 
										// Determine query operator 
										.Operator(isMatchExact ? Operator.And : Operator.Or)
									),
								s => s
									// "nested": Query from nested object of "library_item"
									.Nested(nst2 => nst2
										// "book_editions.authors": path
										.Path(p => p.Authors)
										// "query": Query command
										.Query(qqq => qqq
											// "bool": Querying with boolean logic
											.Bool(bbb => bbb
												// "should": Query clauses should match
												.Should(sss => sss
													// Search with multiple fields ("multi_match")
													.MultiMatch(m => m
														.Fields(f => f
															// "fields":
															// ["library_item.authors.author_code","library_item.authors.fullname",
															// "library_item.authors.biography"]
															.Field(ff => ff.Authors.First().AuthorCode)
															.Field(ff => ff.Authors.First().FullName)
															.Field(ff => ff.Authors.First().Biography)
														)
														// "query": "search_text"
														.Query(searchText)
														// Determine query operator 
														.Operator(isMatchExact ? Operator.And : Operator.Or)
													)
												)
												// "must_not": Query clauses must not match and do not affect relevance score
												.MustNot(mn => mn
													// "term": { "library_item.authors.is_deleted" : "false" }
													.Term(t => t
														.Field(f => f.Authors.First().IsDeleted)
														.Value(true)
													)
												)
											)
										)
										// "score_mode": avg
										.ScoreMode(NestedScoreMode.Average)
									)
							)
							// Required at least one descriptor in "should" matched
							.MinimumShouldMatch(1)
							// "must_not": Query clauses must not match and do not affect relevance score
							.MustNot(mn => mn
								// "match": { "status" : "draft" }
								.Match(t => t
									.Field(f => f.Status)
									.Query(nameof(LibraryItemStatus.Draft))
								),
								mm => mm
								// "term": { "is_deleted" : true }
								.Term(t => t.IsDeleted, true)
							)
						);
					
					// Build-up query for search with text (GET index_name/_search)
					searchResp = await _elasticClient.SearchAsync<ElasticLibraryItem>(x => x
						// index_name
						.Index(ElasticIndexConst.LibraryItemIndex)
						// "query": Query command
						.Query(queryFunc)
						.Sort(s => s.Descending(SortSpecialField.Score))
						.Skip((pageIndex - 1) * pageSize) // Offset
						.Take(pageSize // Limit number of items
						), cancellationToken);
				}
				else
				{
					switch (searchWithKeyword)
					{
						case SearchKeyword.Title:
							// Build-up and assign query func
							queryFunc = q => q
								.Bool(b => b
									.Must(m => m
										.Match(mm => mm
											.Field(f => f.Title.Suffix(fieldSuffix))
											.Query(searchText)
											.Operator(isMatchExact ? Operator.And : Operator.Or)
											.Fuzziness(GetFuzziness(searchText))
										)
									)
									.MustNot(GetMustNotClauses())
								);
							
							// Progress search with specific elastic query 
							searchResp = await BuildSearchQuery(
								// Cancellation token
								cancellationToken,
								// Pagination fields
								pageIndex,
								pageSize,
								// With specific index_name
								ElasticIndexConst.LibraryItemIndex,
								// Apply query func
								queryFunc
							);
							break;
						case SearchKeyword.Author:
							// Build-up and assign query func
							queryFunc = q => q.Nested(n => n
								.Path(p => p.Authors)
								.Query(nq => nq.Bool(nb => nb
									.Must(s => s.Match(m => m
										.Field(f => f.Authors.First().FullName)
										.Query(searchText)
										.Operator(isMatchExact ? Operator.And : Operator.Or)
									))
									.MustNot(mn => mn.Term(t => t
										.Field(f => f.Authors.First().IsDeleted)
										.Value(true)
									))
								))
								.ScoreMode(NestedScoreMode.Average)
							);
							
							// Progress search with specific elastic query 
							searchResp = await BuildSearchQuery(
								// Cancellation token
								cancellationToken,
								// Pagination fields
								pageIndex,
								pageSize,
								// With specific index_name
								ElasticIndexConst.LibraryItemIndex,
								// Apply query func
								queryFunc
							);
							break;
						case SearchKeyword.TopicalTerms:
							// Build-up and assign query func
							queryFunc = q => q.Bool(b => b
								.Must(m => m.Match(mm => mm
									.Field(f => f.TopicalTerms)
									.Query(searchText)
									.Operator(isMatchExact ? Operator.And : Operator.Or)
									.Fuzziness(GetFuzziness(searchText))
								))
								.MustNot(GetMustNotClauses())
							);
							
							// Progress search with specific elastic query 
							searchResp = await BuildSearchQuery(
								// Cancellation token
								cancellationToken,
								// Pagination fields
								pageIndex,
								pageSize,
								// With specific index_name
								ElasticIndexConst.LibraryItemIndex,
								// Apply query func
								queryFunc
							);
							break;
						case SearchKeyword.Isbn:
							// Build-up and assign query func
							queryFunc = q => q.Bool(b => b
								.Must(m => m.Match(mm => mm
									.Field(f => f.Isbn)
									.Query(searchText)
									.Operator(isMatchExact ? Operator.And : Operator.Or)
									.Fuzziness(GetFuzziness(searchText))
								))
								.MustNot(GetMustNotClauses())
							);
							
							// Progress search with specific elastic query 
							searchResp = await BuildSearchQuery(
								// Cancellation token
								cancellationToken,
								// Pagination fields
								pageIndex,
								pageSize,
								// With specific index_name
								ElasticIndexConst.LibraryItemIndex,
								// Apply query func
								queryFunc
							);
							break;
						case SearchKeyword.ClassificationNumber:
							// Build-up and assign query func
							queryFunc = q => q.Bool(b => b
								.Must(m => m.Match(mm => mm
									.Field(f => f.ClassificationNumber)
									.Query(searchText)
									.Operator(isMatchExact ? Operator.And : Operator.Or)
									.Fuzziness(GetFuzziness(searchText))
								))
								.MustNot(GetMustNotClauses())
							);
							
							// Progress search with specific elastic query
							searchResp = await BuildSearchQuery(
								// Cancellation token
								cancellationToken,
								// Pagination fields
								pageIndex,
								pageSize,
								// With specific index_name
								ElasticIndexConst.LibraryItemIndex,
								// Apply query func
								queryFunc
							);
							break;
						case SearchKeyword.Genres:
							// Build-up and assign query func
							queryFunc = q => q.Bool(b => b
								.Must(m => m.Match(mm => mm
									.Field(f => f.Genres)
									.Query(searchText)
									.Operator(isMatchExact ? Operator.And : Operator.Or)
									.Fuzziness(GetFuzziness(searchText))
								))
								.MustNot(GetMustNotClauses())
							);
							
							// Progress search with specific elastic query
							searchResp = await BuildSearchQuery(
								// Cancellation token
								cancellationToken,
								// Pagination fields
								pageIndex,
								pageSize,
								// With specific index_name
								ElasticIndexConst.LibraryItemIndex,
								// Apply query func
								queryFunc
							);
							break;
						case SearchKeyword.Publisher:
							// Build-up and assign query func
							queryFunc = q => q.Bool(b => b
								.Must(m => m.Match(mm => mm
									.Field(f => f.Publisher)
									.Query(searchText)
									.Operator(isMatchExact ? Operator.And : Operator.Or)
									.Fuzziness(GetFuzziness(searchText))
								))
								.MustNot(GetMustNotClauses())
							);
							
							// Progress search with specific elastic query
							searchResp = await BuildSearchQuery(
								// Cancellation token
								cancellationToken,
								// Pagination fields
								pageIndex,
								pageSize,
								// With specific index_name
								ElasticIndexConst.LibraryItemIndex,
								// Apply query func
								queryFunc
							);
							break;
					}
				}
			}
			else // Try to search all 
			{
				// Build-up and assign query func
				queryFunc = q => q
					.Bool(b => b
						.Must(m => m
								.MatchAll() // Match all documents
						)
						// "must_not": Query clauses must not match and do not affect relevance score
						.MustNot(mn => mn
								// "match": { "status" : "draft" }
								.Match(t => t
									.Field(f => f.Status)
									.Query(nameof(LibraryItemStatus.Draft))
								),
							mm => mm
								// "term": { "is_deleted" : true }
								.Term(t => t.IsDeleted, true)
						)
					);
				
				// Progress search with specific elastic query 
				searchResp = await _elasticClient.SearchAsync<ElasticLibraryItem>(x => x
					// index_name
					.Index(ElasticIndexConst.LibraryItemIndex)
					// "query": Query command
					.Query(queryFunc)
					// TODO: Add total reviewed to elastic
					// Default sort by total reviewed
					.Sort(s => s.Descending(SortSpecialField.Score))
					.Skip((pageIndex - 1) * pageSize) // Offset
					.Take(pageSize // Limit number of items
					), cancellationToken);
			}

			if (searchResp?.Total > 0)
			{
				// Count total actual item 
				var totalActualItem = queryFunc != null ? await CountTotalActualItemWithQueryAsync(queryFunc) : 0;
				// Total page
				var totalPage = (int)Math.Ceiling((double)totalActualItem / pageSize);

				return new ServiceResult(ResultCodeConst.SYS_Success0002,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
					searchResp.ToSearchLibraryItemResponse(pageIndex, pageSize, totalPage, totalActualItem));
			}

			return new ServiceResult(ResultCodeConst.SYS_Warning0004,
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
				searchResp?.ToSearchLibraryItemResponse(pageIndex: 0, pageSize: 0, totalPage: 0, totalActualItems: 0));
		}

		/// <summary>
		/// Build elastic query helper method
		/// </summary>
		/// <param name="cancellationToken"></param>
		/// <param name="pageIndex"></param>
		/// <param name="pageSize"></param>
		/// <param name="indexName"></param>
		/// <param name="queryFunc"></param>
		/// <returns></returns>
		private async Task<ISearchResponse<ElasticLibraryItem>> BuildSearchQuery(
			CancellationToken cancellationToken,
			int pageIndex,
			int pageSize,
			string indexName,
			Func<QueryContainerDescriptor<ElasticLibraryItem>, QueryContainer> queryFunc
		)
		{
			return await _elasticClient.SearchAsync<ElasticLibraryItem>(x => x
					.Index(indexName)
					.Query(queryFunc)
					.Sort(s => s.Descending(SortSpecialField.Score))
					.Skip((pageIndex - 1) * pageSize)
					.Take(pageSize),
				cancellationToken
			);
		}

		/// <summary>
		/// Get fuzziness for elastic query helper method
		/// </summary>
		/// <param name="searchText"></param>
		/// <returns></returns>
		private Fuzziness GetFuzziness(string searchText)
		{
			return searchText.Length > 4 ? Fuzziness.EditDistance(1) : Fuzziness.EditDistance(0);
		}

		/// <summary>
		/// Must not clauses helper method
		/// </summary>
		/// <returns></returns>
		private Func<QueryContainerDescriptor<ElasticLibraryItem>, QueryContainer>[] GetMustNotClauses()
		{
			return new Func<QueryContainerDescriptor<ElasticLibraryItem>, QueryContainer>[]
			{
				mn => mn.Match(t => t
					.Field(f => f.Status)
					.Query(nameof(LibraryItemStatus.Draft))
				),
				mn => mn.Term(t => t.IsDeleted, true)
			};
		}

		/// <summary>
		/// Count total actual item based on specific query statement
		/// </summary>
		/// <param name="queryFunc"></param>
		/// <returns></returns>
		private async Task<int> CountTotalActualItemWithQueryAsync(
			Func<QueryContainerDescriptor<ElasticLibraryItem>, QueryContainer> queryFunc)
		{
			// Try to count total item based on specific query logic
			var countResponse = await _elasticClient.CountAsync<ElasticLibraryItem>(x => x
					.Index(ElasticIndexConst.LibraryItemIndex) // Specify the index
					.Query(queryFunc) // Apply the custom query logic
			);

			// Return the count
			return (int)countResponse.Count;
		}

		/// <summary>
		/// Count total actual item within a single index
		/// </summary>
		/// <returns></returns>
		private async Task<int> CountTotalActualItemAsync()
		{
			// Cet the total number of documents in the index
			var countResponse = await _elasticClient.CountAsync<ElasticLibraryItem>(x => x
				.Index(ElasticIndexConst.LibraryItemIndex) // Specify the index
				.Query(q => q
					.Bool(b => b
						// Exclude documents with "status: draft" or "is_deleted: true"
						.MustNot(mn => mn
								.Match(t => t
									.Field(f => f.Status)
									.Query(nameof(LibraryItemStatus.Draft))
								),
							mm => mm
								.Term(t => t.IsDeleted, true)
						)
					)
				)
			);

			// Return the count
			return (int)countResponse.Count;
		}
	}

	public static class SearchServiceExtensions
	{
		public static List<AdvancedFilter>? FromParamsToListAdvancedFilter(this SearchItemParameters searchReq)
		{
			if (searchReq.F == null || !searchReq.F.Any()) return null;
        
			// Initialize list of advanced filters
			var advancedFilters = new List<AdvancedFilter>();
			for (int i = 0; i < searchReq.F.Length; i++)
			{
				var fieldName = searchReq.F[i];
				var filterOperator = searchReq.O?.ElementAtOrDefault(i) != null ? searchReq.O[i] : (FilterOperator?) null;
				var value = searchReq.V?.ElementAtOrDefault(i) != null ? searchReq.V[i] : null;

				if (filterOperator != null && value != null)
				{
					advancedFilters.Add(new()
					{
						FieldName = fieldName,
						Operator = filterOperator,
						Value = value,
					});
				}
			}

			return advancedFilters;
		}
	}
}
