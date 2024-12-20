using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Filters;
using FPTU_ELibrary.API.Payloads.Requests.Book;
using FPTU_ELibrary.Application.Dtos.Books;
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

		[Authorize]
		[HttpGet(APIRoute.Book.Search, Name = nameof(SearchBookAsync))]
		public async Task<IActionResult> SearchBookAsync([FromQuery] SearchBookRequest req, CancellationToken cancellationToken)
		{
			// Map request to search book params
			var searchBookParams = req.ToSearchBookParams();
			// Process search book
			return Ok(await _searchService.SearchBookAsync(searchBookParams, cancellationToken));
		}

		[Authorize]
		[HttpGet(APIRoute.Book.GetCreateInformation, Name = nameof(GetBookCreateInformationAsync))]
		public async Task<IActionResult> GetBookCreateInformationAsync()
		{
			return Ok(await _bookService.GetCreateInformationAsync());
		}
		
		[HttpPost(APIRoute.Book.Create, Name = nameof(CreateBookAsync))]
		public async Task<IActionResult> CreateBookAsync([FromBody] CreateBookRequest req)
		{
			return Ok();
		}

		[HttpPut(APIRoute.Book.Update, Name = nameof(UpdateBookAsync))]
		public async Task<IActionResult> UpdateBookAsync([FromBody] BookDto dto)
		{
			return Ok(await _bookService.UpdateAsync(dto.BookId, dto));
		}
	} 
}
