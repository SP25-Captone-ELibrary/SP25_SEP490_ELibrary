using FPTU_ELibrary.API.Mappings;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.Roles;
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
			var mustClauses = new List<Func<QueryContainerDescriptor<ElasticBook>, QueryContainer>>();

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
						    nameof(ElasticBook.Categories).ToLowerInvariant())
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
											// From nested object
											.Nested(nst => nst
												// "categories": path 
												.Path(p => p.Categories)
												// "query": Add query logic 
												.Query(qq => qq
													// "bool": Querying with boolean logic
													.Bool(b => b
															// "should": Query clauses should match
															.Should(categoryIds.Select(categoryId => // From each category found
																// Return query container 
																(Func<QueryContainerDescriptor<ElasticBook>, QueryContainer>)(sc => sc
																	// Term query
																	.Term(t => t
																		// With specific field
																		.Field(f => f.Categories.First().CategoryId)
																		// With specific value
																		.Value(categoryId)
																	)
																)
															).ToArray()) // Convert to array query containers inside single must query
													)
												)
											)
										);
										break;
									case FilterOperator.Equals:
										// Add 'Equals' logic to must clauses
										mustClauses.Add(m => m
											// From nested object
											.Nested(nst => nst
												// "categories": path 
												.Path(p => p.Categories)
												// "query": Add query logic 
												.Query(qq => qq
													// "bool": Querying with boolean logic
													.Bool(b => b
														// "must": Query clauses must match
														.Must(categoryIds.Select(categoryId => // From each category found
															// Return query container 
															(Func<QueryContainerDescriptor<ElasticBook>, QueryContainer>)(sc => sc
																// Term query
																.Term(t => t
																	// With specific field
																	.Field(f => f.Categories.First().CategoryId)
																	// With specific value
																	.Value(categoryId)
																)
															)
														).ToArray()) // Convert to array query containers inside single must query
													)
												)
											)
										);
										break;
									case FilterOperator.NotEqualsTo:
										// Add 'Not Equals' logic to must clauses
										mustClauses.Add(m => m
											// From nested object
											.Nested(nst => nst
												// "categories": path 
												.Path(p => p.Categories)
												// "query": Add query logic 
												.Query(qq => qq
													// "bool": Querying with boolean logic
													.Bool(b => b
															// "must_not": Query clauses must not match
															.MustNot(categoryIds.Select(categoryId => // From each category found
																// Return query container 
																(Func<QueryContainerDescriptor<ElasticBook>, QueryContainer>)(sc => sc
																	// Term query
																	.Term(t => t
																		// With specific field
																		.Field(f => f.Categories.First().CategoryId)
																		// With specific value
																		.Value(categoryId)
																	)
																)
															).ToArray()) // Convert to array query containers inside single must query
													)
												)
											)
										);
										break;
								}
							}
						}
						// Is Status
						if (filter.FieldName.ToLowerInvariant() ==
						    nameof(ElasticBookEdition.Status).ToLowerInvariant())
						{
							// Try to parse filtering status value to typeof(BookEditionStatus)
							if (Enum.Parse(typeof(BookEditionStatus), filter.Value ?? string.Empty) 
							    is BookEditionStatus status)
							{
								// Determine operator 
								switch (filter.Operator)
								{
									case FilterOperator.Equals:
										// Add must clause
										mustClauses.Add(m => m
											// From nested object
											.Nested(nst => nst
												// "book_editions": path
												.Path(p => p.BookEditions)
												// "query": Add query logic  
												.Query(q => q
													// "bool": Query with boolean logic 
													.Bool(b => b
														// "must": Query clauses must match
														.Must(mm => mm
															// Term query
															.Term(t => t
																// With specific field
																.Field(f => f.BookEditions.First().Status)
																// With specific value
																.Value(status.ToString())
															)
														)
													)
												)
											)
										);
										break;
									case FilterOperator.NotEqualsTo:
										// Add must clause
										mustClauses.Add(m => m
											// From nested object
											.Nested(nst => nst
												// "book_editions": path
												.Path(p => p.BookEditions)
												// "query": Add query logic
												.Query(q => q
													// "bool": Query with boolean logic 
													.Bool(b => b
														// "must_not": Query clauses must not match
														.MustNot(mm => mm
															// Term query
															.Term(t => t
																// With specific field
																.Field(f => f.BookEditions.First().Status)
																// With specific value
																.Value(status.ToString())
															)
														)
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
			var result = await _elasticClient.SearchAsync<ElasticBook>(x => x
				// index_name
				.Index(ElasticIndexConst.BookIndex)
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
								// "fields": ["title", "sub_title", "summary", "book_code"]
								.Fields(f => f
									.Field(ff => ff.Title, boost: 2) // Boosting relevance score if match 
									.Field(ff => ff.Summary)
									.Field(ff => ff.SubTitle)
									.Fields(ff => ff.BookCode)
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
								// "nested": Query from nested object 
								.Nested(nst => nst
									// "inner_hits": {"name": "book_editions"}
									.InnerHits(ih => 
										ih.Name(ElasticUtils.GetElasticFieldName<ElasticBook>(nameof(ElasticBook.BookEditions))))
									// "book_editions": path
									.Path(p => p.BookEditions)
									// "query": Query command
									.Query(qq => qq
										// "bool": Querying with boolean logic
										.Bool(bb => bb
											// "should": Query clauses should match
											.Should(ss => ss
												// Search with multiple fields ("multi_match")
												.MultiMatch(m => m
													// "fields": ["book_editions.edition_title", "book_editions.edition_summary"]
													.Fields(f => f
														.Field(ff => ff.BookEditions.First().EditionTitle, boost: 2) // Boosting relevance score if match  
														.Field(ff => ff.BookEditions.First().EditionSummary)
													)
													// "query": "search_text"
													.Query(parameters.SearchText)
													// "fuzziness": 1
													.Fuzziness(Fuzziness.AutoLength(1, 1))
												), ss => ss
												// "nested": Query from nested object of "book_editions"
												.Nested(nst2 => nst2
													// "book_editions.authors": path
													.Path(p => p.BookEditions.First().Authors)
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
																		// ["book_editions.authors.fullname", "book_editions.authors.biography",
																		// "book_editions.authors.nationality"]
																		.Field(ff => ff.BookEditions.First().Authors.First().FullName)
																		.Field(ff => ff.BookEditions.First().Authors.First().Biography)
																		.Field(ff => ff.BookEditions.First().Authors.First().Nationality)
																	)
																	// "query": "search_text"
																	.Query(parameters.SearchText)
																	// "fuzziness": 1
																	.Fuzziness(Fuzziness.AutoLength(1, 1))
																)
															)
														)
													)
													// "score_mode": avg
													.ScoreMode(NestedScoreMode.Average)
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
				// .Skip(parameters.Skip)
				// .Take(parameters.Take
				// ),cancellationToken
			, cancellationToken);

			// Initialize a collection to store all innerHits
			var allInnerHits = new List<IHit<ILazyDocument>>();
			if(result.Total > 0)
			{
				foreach (var hit in result.Hits)
				{
					// Collect all inner hits from the dictionary
					foreach (var innerHitsEntry in hit.InnerHits)
					{
						// Aggregate all innerHits from the dictionary values
						allInnerHits.AddRange(innerHitsEntry.Value.Hits.Hits);
					}
				}

				// Sort all collected innerHits by relevance score in descending order
				var sortedInnerHits = allInnerHits.OrderByDescending(h => h.Score ?? 0);

				// Map sorted innerHits to ElasticBookEdition
				var mappedEditions = sortedInnerHits
					.Select(innerHit => innerHit.Source.As<ElasticBookEdition>())
					.Where(edition => edition != null) // Filter out null mappings
					.ToList();
				
				// Pagination
				var pageIndex = parameters.PageIndex;
				var pageSize = parameters.PageSize > 0 ? parameters.PageSize : _appSettings.PageSize;
				// Total actual item
				var totalActualItem = mappedEditions.Count;
				// Total page
				var totalPage = (int) Math.Ceiling((double) totalActualItem/ pageSize) + 1;
				// Validate pagination fields base on counted total page
				if (parameters.PageIndex < 1 || parameters.PageIndex > totalPage) pageIndex = 1;
				
				// Apply paging offset 
				mappedEditions = mappedEditions
					.Skip((pageIndex - 1) * pageSize) // Offset
					.Take(pageSize)  // Limit number of items
					.ToList(); // Convert to collection
				
				// Success response
				return new ServiceResult(ResultCodeConst.SYS_Success0002, 
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
					mappedEditions.ToSearchBookEditionResponse(
						pageIndex: pageIndex,
						pageSize: pageSize,
						totalPage: totalPage,
						totalActualItems: totalActualItem));
			}

			// Data null or empty response
			return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
					new List<ElasticBookEdition>().ToSearchBookEditionResponse(
					pageIndex: parameters.PageIndex,
					pageSize: parameters.PageSize,
					totalPage: 0,
					totalActualItems: 0));
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
