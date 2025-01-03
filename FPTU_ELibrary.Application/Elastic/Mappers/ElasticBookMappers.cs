using System.Text.RegularExpressions;
using FPTU_ELibrary.Application.Dtos.BookEditions;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Elastic.Models;
using FPTU_ELibrary.Application.Elastic.Responses;
using Nest;

namespace FPTU_ELibrary.API.Mappings
{
	public static class ElasticBookMappers
	{
		public static ElasticBook ToElasticBook(this BookDto bookDto)
			=> new()
			{
				BookId = bookDto.BookId,
				Title = bookDto.Title,
				SubTitle = bookDto.SubTitle ?? string.Empty,
				Summary = bookDto.Summary,
				IsDeleted = bookDto.IsDeleted,
				CreatedAt = bookDto.CreatedAt,
				CreatedBy = bookDto.CreatedBy,
				UpdatedAt = bookDto.UpdatedAt,
				UpdatedBy = bookDto.UpdatedBy,
				Categories = bookDto.BookCategories.Select(bc => new ElasticCategory()
				{
					CategoryId = bc.CategoryId,
					EnglishName = bc.Category.EnglishName,
					VietnameseName = bc.Category.VietnameseName,
					Description = bc.Category.Description ?? string.Empty
				}).ToList(),
				BookEditions = bookDto.BookEditions.Select(be => new ElasticBookEdition
				{
					BookEditionId = be.BookEditionId,
					BookId = be.BookId,
					EditionTitle = be.EditionTitle,
					EditionNumber = be.EditionNumber,
					EditionSummary = be.EditionSummary,
					PublicationYear = be.PublicationYear,
					PageCount = be.PageCount,
					Language = be.Language,
					CoverImage = be.CoverImage,
					Format = be.Format,
					Publisher = be.Publisher,
					Isbn = be.Isbn,
					IsDeleted = be.IsDeleted,
					CanBorrow = be.CanBorrow,
					Status = be.Status.ToString(),
					CreatedAt = be.CreatedAt,
					UpdatedAt = be.UpdatedAt,
					CreatedBy = be.CreatedBy,
					UpdatedBy = be.UpdatedBy,
					Authors = be.BookEditionAuthors.Select(ba => new ElasticAuthor
					{
						AuthorId = ba.Author.AuthorId,
						AuthorCode = ba.Author.AuthorCode,
						AuthorImage = ba.Author.AuthorImage,
						FullName = ba.Author.FullName,
						Biography = Regex.Replace(ba.Author.Biography ?? string.Empty, "<.*?>", string.Empty),
						Dob = ba.Author.Dob,
						DateOfDeath = ba.Author.DateOfDeath,
						Nationality = ba.Author.Nationality,
						CreateDate = ba.Author.CreateDate,
						UpdateDate = ba.Author.UpdateDate,
						IsDeleted = ba.Author.IsDeleted
					}).ToList()
				}).ToList(),
			};

		public static ElasticBook ToElasticBook(this BookEditionDto bookEditionDto)
			=> new()
			{
				BookId = bookEditionDto.Book.BookId,
				Title = bookEditionDto.Book.Title,
				SubTitle = bookEditionDto.Book.SubTitle ?? string.Empty,
				Summary = bookEditionDto.Book.Summary,
				IsDeleted = bookEditionDto.Book.IsDeleted,
				CreatedAt = bookEditionDto.Book.CreatedAt,
				CreatedBy = bookEditionDto.Book.CreatedBy,
				UpdatedAt = bookEditionDto.Book.UpdatedAt,
				UpdatedBy = bookEditionDto.Book.UpdatedBy,
				Categories = bookEditionDto.Book.BookCategories.Select(bc => new ElasticCategory()
				{
					CategoryId = bc.CategoryId,
					EnglishName = bc.Category.EnglishName,
					VietnameseName = bc.Category.VietnameseName,
					Description = bc.Category.Description ?? string.Empty
				}).ToList(),
				BookEditions = new()
				{
					new ()
					{
						BookEditionId = bookEditionDto.BookEditionId,
						BookId = bookEditionDto.BookId,
						EditionTitle = bookEditionDto.EditionTitle,
						EditionNumber = bookEditionDto.EditionNumber,
						EditionSummary = bookEditionDto.EditionSummary,
						PublicationYear = bookEditionDto.PublicationYear,
						PageCount = bookEditionDto.PageCount,
						Language = bookEditionDto.Language,
						CoverImage = bookEditionDto.CoverImage,
						Format = bookEditionDto.Format,
						Publisher = bookEditionDto.Publisher,
						Isbn = bookEditionDto.Isbn,
						IsDeleted = bookEditionDto.IsDeleted,
						CanBorrow = bookEditionDto.CanBorrow,
						Status = bookEditionDto.Status.ToString(),
						CreatedAt = bookEditionDto.CreatedAt,
						UpdatedAt = bookEditionDto.UpdatedAt,
						CreatedBy = bookEditionDto.CreatedBy,
						UpdatedBy = bookEditionDto.UpdatedBy,
						Authors = bookEditionDto.BookEditionAuthors.Select(ba => new ElasticAuthor
						{
							AuthorId = ba.Author.AuthorId,
							AuthorCode = ba.Author.AuthorCode,
							AuthorImage = ba.Author.AuthorImage,
							FullName = ba.Author.FullName,
							Biography = Regex.Replace(ba.Author.Biography ?? string.Empty, "<.*?>", string.Empty),
							Dob = ba.Author.Dob,
							DateOfDeath = ba.Author.DateOfDeath,
							Nationality = ba.Author.Nationality,
							CreateDate = ba.Author.CreateDate,
							UpdateDate = ba.Author.UpdateDate,
							IsDeleted = ba.Author.IsDeleted
						}).ToList()
					}
				},
			};
		
		public static ElasticBookEdition ToElasticBookEdition(this BookEditionDto bookEditionDto)
			=> new ElasticBookEdition()
			{
				BookEditionId = bookEditionDto.BookEditionId,
				BookId = bookEditionDto.BookId,
				EditionTitle = bookEditionDto.EditionTitle,
				EditionNumber = bookEditionDto.EditionNumber,
				EditionSummary = bookEditionDto.EditionSummary,
				PublicationYear = bookEditionDto.PublicationYear,
				PageCount = bookEditionDto.PageCount,
				Language = bookEditionDto.Language,
				CoverImage = bookEditionDto.CoverImage,
				Format = bookEditionDto.Format,
				Publisher = bookEditionDto.Publisher,
				Isbn = bookEditionDto.Isbn,
				IsDeleted = bookEditionDto.IsDeleted,
				CanBorrow = bookEditionDto.CanBorrow,
				Status = bookEditionDto.Status.ToString(),
				CreatedAt = bookEditionDto.CreatedAt,
				UpdatedAt = bookEditionDto.UpdatedAt,
				CreatedBy = bookEditionDto.CreatedBy,
				UpdatedBy = bookEditionDto.UpdatedBy,
				Authors = bookEditionDto.BookEditionAuthors.Select(ba => new ElasticAuthor
				{
					AuthorId = ba.Author.AuthorId,
					AuthorCode = ba.Author.AuthorCode,
					AuthorImage = ba.Author.AuthorImage,
					FullName = ba.Author.FullName,
					Biography = Regex.Replace(ba.Author.Biography ?? string.Empty, "<.*?>", string.Empty),
					Dob = ba.Author.Dob,
					DateOfDeath = ba.Author.DateOfDeath,
					Nationality = ba.Author.Nationality,
					CreateDate = ba.Author.CreateDate,
					UpdateDate = ba.Author.UpdateDate,
					IsDeleted = ba.Author.IsDeleted
				}).ToList()
			};
		
		public static SearchBookResponse ToSearchBookResponse(this ISearchResponse<ElasticBook> searchResp,
			int pageIndex, int pageSize, long totalPage)
			=> new(searchResp.Documents.ToList(), pageIndex, pageSize, totalPage);
	}
}
