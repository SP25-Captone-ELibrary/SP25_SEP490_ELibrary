using System.Security.Claims;
using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Filters;
using FPTU_ELibrary.API.Payloads.Requests.Book;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.BookEditions;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Services;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FPTU_ELibrary.API.Controllers
{
	[ApiController]
	public class BookController : ControllerBase
	{
		private readonly IBookService<BookDto> _bookService;
		private readonly ISearchService _searchService;
		private readonly AppSettings _appSettings;
		private readonly IBookEditionService<BookEditionDto> _bookEditionService;

		public BookController(
			IBookService<BookDto> bookService, 
			IBookEditionService<BookEditionDto> bookEditionService,
			ISearchService searchService,
			IOptionsMonitor<AppSettings> monitor)
        {
            _bookService = bookService;
			_searchService = searchService;
			_bookEditionService = bookEditionService;
			_appSettings = monitor.CurrentValue;
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

		[Authorize]
		[HttpGet(APIRoute.Book.GetById, Name = nameof(GetBookByIdAsync))]
		public async Task<IActionResult> GetBookByIdAsync([FromRoute] int id)
		{
			return Ok(await _bookService.GetByIdAsync(id));
		}
		
		[Authorize]
		[HttpPost(APIRoute.Book.Create, Name = nameof(CreateBookAsync))]
		public async Task<IActionResult> CreateBookAsync([FromBody] CreateBookRequest req)
		{
			// Retrieve user email from token
			var email = User.FindFirst(ClaimTypes.Email)?.Value;
			return Ok(await _bookService.CreateAsync(req.ToBookDto(), email ?? string.Empty));
		}
		
		[Authorize]
		[HttpPut(APIRoute.Book.Update, Name = nameof(UpdateBookAsync))]
		public async Task<IActionResult> UpdateBookAsync([FromRoute] int id, [FromBody] UpdateBookRequest dto)
		{
			// Retrieve user email from token
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
			return Ok(await _bookService.UpdateAsync(id, dto.ToBookDto(), email ?? string.Empty));
		}
	} 
}
