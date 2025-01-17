using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers
{
	[ApiController]
	public class BookController : ControllerBase
	{
		// private readonly ISearchService _searchService;
		// private readonly AppSettings _appSettings;
		// private readonly ILibraryItemService<LibraryItemDto> _libraryItemService;
  //
		// public BookController(
		// 	ILibraryItemService<LibraryItemDto> libraryItemService,
		// 	ISearchService searchService,
		// 	IOptionsMonitor<AppSettings> monitor)
  //       {
		// 	_searchService = searchService;
		// 	_libraryItemService = libraryItemService;
		// 	_appSettings = monitor.CurrentValue;
  //       }

		// [Authorize]
		// [HttpGet(APIRoute.Book.Search, Name = nameof(SearchBookAsync))]
		// public async Task<IActionResult> SearchBookAsync([FromQuery] SearchBookRequest req, CancellationToken cancellationToken)
		// {
		// 	// Map request to search book params
		// 	var searchBookParams = req.ToSearchBookParams();
		// 	// Process search book
		// 	return Ok(await _searchService.SearchBookAsync(searchBookParams, cancellationToken));
		// }

		// [Authorize]
		// [HttpGet(APIRoute.Book.GetEnums, Name = nameof(GetBookEnumsAsync))]
		// public async Task<IActionResult> GetBookEnumsAsync()
		// {
		// 	return Ok(await _bookService.GetBookEnumsAsync());
		// }
  //
		// [Authorize]
		// [HttpGet(APIRoute.Book.GetById, Name = nameof(GetBookByIdAsync))]
		// public async Task<IActionResult> GetBookByIdAsync([FromRoute] int id)
		// {
		// 	return Ok(await _bookService.GetByIdAsync(id));
		// }
		//
		// [Authorize]
		// [HttpPost(APIRoute.Book.Create, Name = nameof(CreateBookAsync))]
		// public async Task<IActionResult> CreateBookAsync([FromBody] CreateBookRequest req)
		// {
		// 	// Retrieve user email from token
		// 	var email = User.FindFirst(ClaimTypes.Email)?.Value;
		// 	return Ok(await _bookService.CreateAsync(req.ToBookDto(), email ?? string.Empty));
		// }
		//
		// [Authorize]
		// [HttpPut(APIRoute.Book.Update, Name = nameof(UpdateBookAsync))]
		// public async Task<IActionResult> UpdateBookAsync([FromRoute] int id, [FromBody] UpdateBookRequest dto)
		// {
		// 	// Retrieve user email from token
  //           var email = User.FindFirst(ClaimTypes.Email)?.Value;
		// 	return Ok(await _bookService.UpdateAsync(id, dto.ToBookDto(), email ?? string.Empty));
		// }
  //
		// [Authorize]
		// [HttpDelete(APIRoute.Book.Delete, Name = nameof(DeleteBookAsync))]
		// public async Task<IActionResult> DeleteBookAsync([FromRoute] int id)
		// {
		// 	return Ok(await _bookService.DeleteAsync(id));
		// }
	} 
}
