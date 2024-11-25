using FPTU_ELibrary.API.Mappings;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Elastic.Models;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;

namespace FPTU_ELibrary.Application.Services
{
	public class ElasticInitializeService : IElasticInitializeService
	{
		private readonly IBookService<BookDto> _bookService;
		private readonly IElasticClient _elasticClient;
		private readonly IConfiguration _configuration;
		private readonly ILogger<ElasticInitializeService> _logger;

		public ElasticInitializeService(IBookService<BookDto> bookService, 
			IElasticClient elasticClient,
			IConfiguration configuration,
			ILogger<ElasticInitializeService> logger)
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
				_logger.LogError("Index {0} not exist", index);
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
							.Date(d => d.Name(e => e.CreateDate))
							.Date(d => d.Name(e => e.UpdatedDate))
							.Keyword(k => k.Name(e => e.CreateBy))
							.Keyword(k => k.Name(e => e.UpdatedBy))
							.Number(n => n.Name(e => e.CategoryId).Type(NumberType.Integer))
							// Mapping book category
							.Object<ElasticBookCategory>(o => o
								.Name(e => e.BookCategory)
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
									.Date(d => d.Name(ee => ee.CreateDate))
									.Date(d => d.Name(ee => ee.UpdatedDate))
									.Keyword(k => k.Name(ee => ee.CreateBy))
								)
							)
							// Mapping author as nested documents
							.Nested<ElasticAuthor>(n => n
								.Name(e => e.Authors)
								.Properties(np => np
									.Number(nn => nn.Name(ee => ee.AuthorId).Type(NumberType.Integer))
									.Keyword(k => k.Name(ee => ee.AuthorCode))
									.Keyword(k => k.Name(ee => ee.AuthorImage))
									.Text(t => t.Name(ee => ee.FirstName))
									.Text(t => t.Name(ee => ee.LastName))
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
				);

				// Check for valid resp 
				if (!createIndexResponse.IsValid)
				{
					_logger.LogError("Error invoke while creating index: {0}", createIndexResponse.ServerError);
					return;
				}
			}

			var documentCountResponse = await _elasticClient.CountAsync<ElasticBook>(c => c.Index(index));
			if(documentCountResponse.Count > 0)
			{
				_logger.LogInformation("Already seed documents for index: {0}", index);
				return;
			}

			// Create book filtering specification
			BaseSpecification<Book> spec = new();
			// Add includes 
			spec.AddInclude(b => b.Category);
			spec.AddInclude(b => b.BookEditions);
			spec.AddInclude(b => b.BookAuthors);

			// Get all books with specification
			var getBookResp = await _bookService.GetAllWithEditionsAndAuthorsAsync(spec);
		
			// Check success result
			if(getBookResp.Status == ResultConst.SUCCESS_READ_CODE)
			{
				// Convert result data to List
				var books = getBookResp.Data as List<BookDto>;

				// Map to elastic list object
				var elasticBooks = books?.Select(b => b.ToElasticBook());

				// Check exist
				if (elasticBooks != null && elasticBooks.Any())
				{
					// Process Bulk API to seeding data to elastic
					await _elasticClient.BulkAsync(x => x
						// Default index name
						.Index(index)
						// Index with many books
						.IndexMany(elasticBooks, (descriptor, elasticBook) => descriptor
							// Assign BookId as elastic document key '_id'
							.Id(elasticBook.BookId)));
				}
			}
		}
	}
}
