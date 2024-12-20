using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Dtos.Locations;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using MapsterMapper;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using BookCategory = FPTU_ELibrary.Domain.Common.Enums.BookCategory;

namespace FPTU_ELibrary.Application.Services
{
	public class BookService : GenericService<Book, BookDto, int>, IBookService<BookDto>
	{
		private readonly IAuthorService<AuthorDto> _authorService;
		private readonly ILibraryShelfService<LibraryShelfDto> _libShelfService;

		public BookService(
			ILibraryShelfService<LibraryShelfDto> libShelfService,
			IAuthorService<AuthorDto> authorService,
			ISystemMessageService msgService,
			IUnitOfWork unitOfWork,
			IMapper mapper,
			ILogger logger) 
			: base(msgService,unitOfWork, mapper, logger)
		{
			_authorService = authorService;
			_libShelfService = libShelfService;
		}

		public async Task<IServiceResult> GetCreateInformationAsync()
		{
			try
			{
				// Determine current system language
				var lang = (SystemLanguage?)EnumExtensions.GetValueFromDescription<SystemLanguage>(
					LanguageContext.CurrentLanguage);
				// Check whether current lang is English
				var isEng = lang == SystemLanguage.English;
				
				// TODO: Change load from enum to call BookCategoryService
				// Get all book categories
				var bookCategories = new List<string>()
				{
					isEng ? nameof(BookCategory.Biography) : BookCategory.Biography.GetDescription(),
					isEng ? nameof(BookCategory.BusinessAndInvesting) : BookCategory.BusinessAndInvesting.GetDescription(),
					isEng ? nameof(BookCategory.CookBooks) : BookCategory.CookBooks.GetDescription(),
					isEng ? nameof(BookCategory.Children) : BookCategory.Children.GetDescription(),
					isEng ? nameof(BookCategory.Essays) : BookCategory.Essays.GetDescription(),
					isEng ? nameof(BookCategory.Education) : BookCategory.Education.GetDescription(),
					isEng ? nameof(BookCategory.FantasyAndScienceFiction) : BookCategory.FantasyAndScienceFiction.GetDescription(),
					isEng ? nameof(BookCategory.Humor) : BookCategory.Humor.GetDescription(),
					isEng ? nameof(BookCategory.History) : BookCategory.History.GetDescription(),
					isEng ? nameof(BookCategory.HealthAndWellness) : BookCategory.HealthAndWellness.GetDescription(),
					isEng ? nameof(BookCategory.LifeSkills) : BookCategory.LifeSkills.GetDescription(),
					isEng ? nameof(BookCategory.Mystery) : BookCategory.Mystery.GetDescription(),
					isEng ? nameof(BookCategory.Novels) : BookCategory.Novels.GetDescription(),
					isEng ? nameof(BookCategory.Politics) : BookCategory.Politics.GetDescription(),
					isEng ? nameof(BookCategory.Poetry) : BookCategory.Politics.GetDescription(),
					isEng ? nameof(BookCategory.Romance) : BookCategory.Romance.GetDescription(),
					isEng ? nameof(BookCategory.ReligionAndSpirituality) : BookCategory.ReligionAndSpirituality.GetDescription(),
					isEng ? nameof(BookCategory.SelfHelp) : BookCategory.SelfHelp.GetDescription(),
					isEng ? nameof(BookCategory.ShortStories) : BookCategory.ShortStories.GetDescription(),
					isEng ? nameof(BookCategory.ScienceAndTechnology) : BookCategory.ScienceAndTechnology.GetDescription(),
					isEng ? nameof(BookCategory.ThrillerAndHorror) : BookCategory.ThrillerAndHorror.GetDescription(),
					isEng ? nameof(BookCategory.TravelAndGeography) : BookCategory.TravelAndGeography.GetDescription(),
				};
				
				// Book formats
				var bookFormats = new List<string>()
				{
					isEng ? nameof(BookFormat.Paperback) : BookFormat.Paperback.GetDescription(),
					isEng ? nameof(BookFormat.HardCover) : BookFormat.HardCover.GetDescription()
				};
				
				// Book resource types
				var bookResourceTypes = new List<string>()
				{
					isEng ? nameof(BookResourceType.Ebook) : BookResourceType.Ebook.GetDescription(),
					isEng ? nameof(BookResourceType.Audio) : BookResourceType.Audio.GetDescription()
				};
				
				// Book authors
				var authors = (await _authorService.GetAllAsync()).Data as List<AuthorDto>;
				if (authors == null || !authors.Any())
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0005);
					errMsg = StringUtils.Format(errMsg, isEng
						? "not found any authors to process create"
						: "không tìm thấy tác giả");
					return new ServiceResult(ResultCodeConst.SYS_Fail0005, errMsg);
				}
				
				// Get all shelves
				var bookShelves = (await _libShelfService.GetAllAsync()).Data as List<LibraryShelfDto>;

				return new ServiceResult(ResultCodeConst.SYS_Success0002,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
					new
					{
						BookCategories = bookCategories,
						BookFormats = bookFormats,
						BookResourceTypes = bookResourceTypes,
						Authors = authors,
						BookShelves = bookShelves
					});
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when process get create information");
			}
		}
	}
}
