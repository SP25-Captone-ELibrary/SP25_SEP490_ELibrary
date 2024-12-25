using FPTU_ELibrary.API.Mappings;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Elastic.Models;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Nest;
using Serilog;

namespace FPTU_ELibrary.Application.Services
{
	public class ElasticInitializeService : IElasticInitializeService
	{
		private readonly IBookService<BookDto> _bookService;
		private readonly IElasticClient _elasticClient;
		private readonly IConfiguration _configuration;
		private readonly ILogger _logger;

		public ElasticInitializeService(IBookService<BookDto> bookService, 
			IElasticClient elasticClient,
			IConfiguration configuration,
			ILogger logger)
        {
			_bookService = bookService;
			_elasticClient = elasticClient;
			_configuration = configuration;
			_logger = logger;
        }

        public async Task RunAsync()
		{
			// Get DefaultIndex str from configuration 
			var index = _configuration["ElasticSettings:DefaultIndex"] ?? string.Empty;

			if (string.IsNullOrEmpty(index)) 
			{
				_logger.Error("Index {0} not exist", index);
				return;
			}

			var indexExistResp = await _elasticClient.Indices.ExistsAsync(index);
			if (!indexExistResp.Exists)
			{
				// PUT request to indices
				var createIndexResponse = _elasticClient.Indices.Create("books", c => c
					// Define mappings for index
					.Map<ElasticBook>(m => m
						// Mapping properties
						.Properties(ps => ps
							.Number(n => n.Name(e => e.BookId).Type(NumberType.Integer))
							.Text(t => t.Name(e => e.Title))
							.Text(t => t.Name(e => e.Summary))
							.Boolean(b => b.Name(e => e.IsDeleted))
							.Boolean(b => b.Name(e => e.IsDraft))
							.Boolean(b => b.Name(e => e.CanBorrow))
							.Date(d => d.Name(e => e.CreatedAt))
							.Date(d => d.Name(e => e.UpdatedAt))
							.Keyword(k => k.Name(e => e.CreatedBy))
							.Keyword(k => k.Name(e => e.UpdatedBy))
							// Mapping book category
							.Nested<ElasticCategory>(o => o
								.Name(e => e.Categories)
								.Properties(op => op
									.Number(n => n.Name(c => c.CategoryId).Type(NumberType.Integer))
									.Text(t => t.Name(c => c.EnglishName))
									.Text(t => t.Name(c => c.VietnameseName))
									.Text(t => t.Name(c => c.Description))
								)
							)
							// Mapping book edition as nested documents
							.Nested<ElasticBookEdition>(n => n
								.Name(e => e.BookEditions)
								.Properties(np => np
									.Number(nn => nn.Name(ee => ee.BookEditionId).Type(NumberType.Integer))
									.Number(nn => nn.Name(ee => ee.BookId).Type(NumberType.Integer))
									.Text(t => t.Name(ee => ee.EditionTitle))
									.Number(k => k.Name(ee => ee.EditionNumber).Type(NumberType.Integer))
									.Number(nn => nn.Name(ee => ee.PublicationYear).Type(NumberType.Integer))
									.Number(nn => nn.Name(ee => ee.PageCount).Type(NumberType.Integer))
									.Keyword(k => k.Name(ee => ee.Language))
									.Keyword(k => k.Name(ee => ee.CoverImage))
									.Keyword(k => k.Name(ee => ee.Format))
									.Text(t => t.Name(ee => ee.Publisher))
									.Keyword(k => k.Name(ee => ee.Isbn))
									.Boolean(b => b.Name(ee => ee.IsDeleted))
									.Date(d => d.Name(ee => ee.CreatedAt))
									.Date(d => d.Name(ee => ee.UpdatedAt))
									.Keyword(k => k.Name(ee => ee.CreatedBy))
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
										)
									)
								)
							)
						)
					)
				);

				// Check for valid resp 
				if (!createIndexResponse.IsValid)
				{
					_logger.Error("Error invoke while creating index: {0}", createIndexResponse.ServerError);
					return;
				}
			}

			var documentCountResponse = await _elasticClient.CountAsync<ElasticBook>(c => c.Index(index));
			if(documentCountResponse.Count > 0)
			{
				_logger.Information("Already seed documents for index: {0}", index);
				return;
			}

			// Create book filtering specification
			BaseSpecification<Book> spec = new();
			// Enables split query
			spec.EnableSplitQuery();
			// Add includes 
			spec.ApplyInclude(q => q
				.Include(b => b.BookCategories)
					.ThenInclude(cat => cat.Category)
				.Include(b => b.BookEditions)
				.Include(b => b.BookEditions));

			// Get all books with specification
			var getBookResp = await _bookService.GetAllWithSpecAsync(spec);
		
			// Check success result
			if(getBookResp.ResultCode == ResultCodeConst.SYS_Success0002)
			{
				// Convert result data to List
				var books = getBookResp.Data as List<BookDto>;

				// Map to elastic list object
				var elasticBooks = books?.Select(b => b.ToElasticBook());
				var toBookList = elasticBooks?.ToList();
				// Check exist
				if (toBookList != null && toBookList.Any())
				{
					// Process Bulk API to seeding data to elastic
					await _elasticClient.BulkAsync(x => x
						// Default index name
						.Index(index)
						// Index with many books
						.IndexMany(toBookList, (descriptor, elasticBook) => descriptor
							// Assign BookId as elastic document key '_id'
							.Id(elasticBook.BookId)));
				}
			}
		}
	}
}
