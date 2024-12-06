using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Filters;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FPTU_ELibrary.API.Controllers
{
	[ApiController]
	public class BookController : ControllerBase
	{
		private readonly IBookService<BookDto> _bookService;
		private readonly ISearchService _searchService;

		public BookController(IBookService<BookDto> bookService, 
			ISearchService searchService)
        {
            _bookService = bookService;
			_searchService = searchService;
        }

		//	Summary:
		//		Get all book
		[Authorize]
		[HttpGet(APIRoute.Book.GetAll, Name = nameof(GetAllBookAsync))]
		public async Task<IActionResult> GetAllBookAsync()
		{
			// Create book filtering specification
			BaseSpecification<Book> spec = new();
			// Enables split query
			spec.EnableSplitQuery();
			// Add includes 
			spec.ApplyInclude(q => q
					.Include(b => b.Category)
					.Include(b => b.BookEditions)
					.Include(b => b.BookAuthors));

			// Get all books with specification
			var getBookResp = await _bookService.GetAllWithEditionsAndAuthorsAsync(spec);
			
			return Ok(getBookResp);
		}
		
		[HttpGet(APIRoute.Book.Search, Name = nameof(SearchBookAsync))]
		public async Task<IActionResult> SearchBookAsync([FromQuery] SearchBookRequest req, CancellationToken cancellationToken)
		{
			// Map request to search book params
			var searchBookParams = req.ToSearchBookParams();
			// Process search book
			return Ok(await _searchService.SearchBookAsync(searchBookParams, cancellationToken));
		}

		[HttpPost(APIRoute.Book.Create, Name = nameof(CreateBookAsync))]
		public async Task<IActionResult> CreateBookAsync([FromBody] BookDto dto)
		{
			return Ok(await _bookService.CreateAsync(dto));
		}

		[HttpPut(APIRoute.Book.Update, Name = nameof(UpdateBookAsync))]
		public async Task<IActionResult> UpdateBookAsync([FromBody] BookDto dto)
		{
			return Ok(await _bookService.UpdateAsync(dto.BookId, dto));
		}
	} 
}
