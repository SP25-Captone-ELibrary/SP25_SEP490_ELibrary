using FPTU_ELibrary.API.Mappings;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Elastic.Models;
using FPTU_ELibrary.Application.Elastic.Params;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
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

        public async Task<IServiceResult> SearchBookAsync(SearchBookParameters parameters, 
			CancellationToken cancellationToken)
		{
			var mustClauses = new List<Func<QueryContainerDescriptor<ElasticLibraryItem>, QueryContainer>>();

			// Pagination
			var pageIndex = parameters.PageIndex;
			var pageSize = parameters.PageSize > 0 ? parameters.PageSize : _appSettings.PageSize;
			// Validate pagination 
			if (parameters.PageIndex < 1) pageIndex = 1;
			
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
														(Func<QueryContainerDescriptor<ElasticLibraryItem>, QueryContainer>)(sc => sc
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
													(Func<QueryContainerDescriptor<ElasticLibraryItem>, QueryContainer>)(sc => sc
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
													(Func<QueryContainerDescriptor<ElasticLibraryItem>, QueryContainer>)(sc => sc
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

			// Build-up query for elastic search (GET index_name/_search)
			var result = await _elasticClient.SearchAsync<ElasticLibraryItem>(x => x
				// index_name
				.Index(ElasticIndexConst.LibraryItemIndex)
				// "query": Query command
				.Query(q => q
					// "bool": Querying with boolean logic <- Allow to mix multiple conditions (Should, Must, Filter, etc.)	
					.Bool(b => b
						// "filter": query clauses are required to match, but will not contribute to 
						// relevance score. Query clauses may therefore be cached for improve performance
						.Filter(mustClauses)
						// "should": Query clauses should match. Relevance scores of matching documents are boosted
						// for each matching query clause. Behaviour can be adjusted with minimum_should_match
						.Should(s => s
							// Search with multiple fields ("multi_match")
							.MultiMatch(m => m
								// "fields": ["title", "sub_title", "summary", etc.]
								.Fields(f => f
									.Field(ff => ff.Title, boost: 2) // Boosting relevance score if match 
									.Field(ff => ff.SubTitle, boost: 2) // Boosting relevance score if match
									.Field(ff => ff.Edition)
									.Field(ff => ff.Summary)
									.Field(ff => ff.Language)
									.Field(ff => ff.OriginLanguage)
									.Field(ff => ff.Responsibility)
									.Field(ff => ff.PublicationPlace)
									.Field(ff => ff.ClassificationNumber)
									.Field(ff => ff.CutterNumber)
									.Field(ff => ff.Isbn)
									.Field(ff => ff.Genres, boost: 2) // Boosting relevance score if match
									.Field(ff => ff.TopicalTerms, boost: 2) // Boosting relevance score if match
									.Field(ff => ff.AdditionalAuthors, boost: 2) // Boosting relevance score if match
								)
								// "query": "search_text"
								.Query(parameters.SearchText)
								// "fuzziness": "auto"
								.Fuzziness(Fuzziness.Auto)
								// Rewarding documents where multiple fields match with tie_breaker param
								// Each matching field affects the relevance score
								// "tie_breaker": 0.3
								.TieBreaker(0.3)
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
												.Query(parameters.SearchText)
												// "fuzziness": 1
												.Fuzziness(Fuzziness.AutoLength(1, 1))
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
										// "must_not": Query clauses must not match and do not affect relevance score
										.MustNot(mn => mn
											// "term": { "library_item.status" : "draft" }
											.Term(t => t
												.Field(f => f.Status)
												// TODO: Change this into Draft when completed
												.Value(nameof(LibraryItemStatus.Published))
											)
										)
										// "must_not": Query clauses must not match and do not affect relevance score
										.MustNot(mn => mn
											// "term": { "library_item.is_deleted" : "false" }
											.Term(t => t
												.Field(f => f.IsDeleted)
												.Value(true)
											)
										)
									)
								)
								// "score_mode": avg
								.ScoreMode(NestedScoreMode.Average)
							)
						)
						// "must_not": Query clauses must not match and do not affect relevance score
						.MustNot(s => s
							// "term": { "is_deleted": "true" }
							.Term(t => t.IsDeleted, true)
						)
						// Required at least one descriptor in "should" matched
						.MinimumShouldMatch(1)
					)
				)
				.Sort(s => s.Descending(SortSpecialField.Score))
				.Skip((pageIndex - 1) * pageSize) // Offset
				.Take(pageSize // Limit number of items
				),cancellationToken);

			if(result.Total > 0)
			{
				// Total actual item
				var totalActualItem = result.Hits.Count;
				// Total page
				var totalPage = (int) Math.Ceiling((double) totalActualItem/ pageSize) + 1;
				
				return new ServiceResult(ResultCodeConst.SYS_Success0002, 
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
					result.ToSearchLibraryItemResponse(pageIndex, pageSize, totalPage, totalActualItem));
			}

			return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
				result.ToSearchLibraryItemResponse(pageIndex: 0, pageSize: 0, totalPage: 0, totalActualItems: 0));
		}
	}

	public static class SearchServiceExtensions
	{
		public static List<AdvancedFilter>? FromParamsToListAdvancedFilter(this SearchBookParameters searchReq)
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
