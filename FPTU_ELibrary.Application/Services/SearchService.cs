using FPTU_ELibrary.API.Mappings;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Elastic.Models;
using FPTU_ELibrary.Application.Elastic.Params;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using Nest;

namespace FPTU_ELibrary.Application.Services
{
	public class SearchService : ISearchService
	{
		private readonly IElasticClient _elasticClient;

		public SearchService(IElasticClient elasticClient)
        {
			_elasticClient = elasticClient;
		}

        public async Task<IServiceResult> SearchBookAsync(SearchBookParameters parameters, 
			CancellationToken cancellationToken)
		{
			var mustClauses = new List<Func<QueryContainerDescriptor<ElasticBook>, QueryContainer>>();

			if (parameters.MaxPageCount.HasValue)
			{
				mustClauses.Add(m => m
					.Nested(nst => nst
						.Path(p => p.BookEditions)
						.Query(q => q
							.Bool(b => b
								.Must(must => must
									.Range(r => r
										.Field(f => f.BookEditions.First().PageCount)
										.LessThanOrEquals(parameters.MaxPageCount)
									)
								)
							)
						)
					)
				);
			}

			if (parameters.PublicationYear.HasValue)
			{
				mustClauses.Add(m => m
					.Nested(nst => nst
						.Path(p => p.BookEditions)
						.Query(q => q
							.Bool(b => b
								.Must(must => must
									.Term(t => t
										.Field(f => f.BookEditions.First().PublicationYear)
										.Value(parameters.PublicationYear)
									)
								)
							)
						)
					)
				);
			}

			if (!string.IsNullOrEmpty(parameters.Languages))
			{
				mustClauses.Add(m => m
					.Nested(nst => nst
						.Path(p => p.BookEditions)
						.Query(q => q
							.Bool(b => b
								.Must(must => must
									.Match(mt => mt
										.Field(f => f.BookEditions.First().Language)
										.Query(parameters.Languages)
									)
								)
							)
						)
					)
				);
			}

			var result = await _elasticClient.SearchAsync<ElasticBook>(x => x
				.Query(q => q
					.Bool(b => b
						.Filter(mustClauses)
						.Should(s => s
							.MultiMatch(m => m
								.Fields(f => f
									.Field(ff => ff.Title, boost: 2)
									.Field(ff => ff.Summary)
									)
								.Query(parameters.SearchText)
								//.Fuzziness(Fuzziness.Auto)
							)
						)
						.MustNot(s => s
							.Term(t => t.IsDeleted, true)
						)
						//.MustNot(s => s
						//	.Term(t => t.IsDraft, true)
						//)
						// .Should(s => s
						// 	.Nested(nst => nst
						// 		.Path(p => p.Authors)
						// 		.Query(q => q
						// 			.Bool(b => b
						// 				.Should(s => s
						// 					.MultiMatch(m => m
						// 						.Fields(f => f
						// 							.Field(ff => ff.Authors.First().FirstName)
						// 							.Field(ff => ff.Authors.First().LastName)
						// 							.Field(ff => ff.Authors.First().Biography)
						// 							.Field(ff => ff.Authors.First().Nationality)
						// 						)
						// 						.Query(parameters.SearchText)
						// 						//.Fuzziness(Fuzziness.AutoLength(1, 1))
						// 					)
						// 				)
						// 			)
						// 		)
						// 		.ScoreMode(NestedScoreMode.Average)
						// 	)
						// )
						.Should(s => s
							.Nested(nst => nst
								.Path(p => p.BookEditions)
								.Query(q => q
									.Bool(b => b
										.Should(s => s
											.MultiMatch(m => m
												.Fields(f => f
													.Field(ff => ff.BookEditions.First().EditionTitle)
													.Field(ff => ff.BookEditions.First().EditionNumber)
												)
												.Query(parameters.SearchText)
												//.Fuzziness(Fuzziness.AutoLength(1, 1))
											)
										)
									)
								)
								.ScoreMode(NestedScoreMode.Average)
							)
						)
						.MinimumShouldMatch(3)
					)
				)
				.Sort(s => s.Descending(SortSpecialField.Score))
				.Skip(parameters.Skip)
				.Take(parameters.Take
				),cancellationToken
			);

			if(result.Total > 0)
			{
				var totalPage = result.Total;
				var pageIndex = (int) Math.Ceiling((double) parameters.Skip/ parameters.Take) + 1;
				var pageSize = parameters.Take;

				return new ServiceResult(ResultCodeConst.SYS_Success0002, "Get data successfully",
					result.ToSearchBookResponse(pageIndex, pageSize, totalPage));
			}

			return new ServiceResult(ResultCodeConst.SYS_Warning0004, "Data not found or empty",
				result.ToSearchBookResponse(pageIndex: 0, pageSize: 0, totalPage: 0));
		}
	}
}
