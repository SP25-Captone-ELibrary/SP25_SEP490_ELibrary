using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Elastic.Models;
using FPTU_ELibrary.Application.Elastic.Responses;
using Nest;

namespace FPTU_ELibrary.API.Mappings
{
	public static class ElasticBookMappers
	{
		public static ElasticBook ToElasticBook(this BookDto booKDto)
			=> new()
			{
				BookId = booKDto.BookId,
				Title = booKDto.Title,
				Summary = booKDto.Summary,
				IsDeleted = booKDto.IsDeleted,
				IsDraft = booKDto.IsDraft,
				CreatedAt = booKDto.CreatedAt,
				CreatedBy = booKDto.CreatedBy,
				UpdatedAt = booKDto.UpdatedAt,
				UpdatedBy = booKDto.UpdatedBy,
				Categories = booKDto.BookCategories.Select(bc => new ElasticCategory()
				{
					CategoryId = bc.CategoryId,
					EnglishName = bc.Category.EnglishName,
					VietnameseName = bc.Category.VietnameseName,
					Description = bc.Category.Description ?? string.Empty
				}).ToList(),
				BookEditions = booKDto.BookEditions.Select(be => new ElasticBookEdition
				{
					BookEditionId = be.BookEditionId,
					BookId = be.BookId,
					EditionTitle = be.EditionTitle,
					EditionNumber = be.EditionNumber,
					PublicationYear = be.PublicationYear,
					PageCount = be.PageCount,
					Language = be.Language,
					CoverImage = be.CoverImage,
					Format = be.Format,
					Publisher = be.Publisher,
					Isbn = be.Isbn,
					IsDeleted = be.IsDeleted,
					CanBorrow = be.CanBorrow,
					CreatedAt = be.CreatedAt,
					UpdatedAt = be.UpdatedAt,
					CreatedBy = be.CreatedBy,
					Authors = be.BookEditionAuthors.Select(ba => new ElasticAuthor
					{
						AuthorId = ba.Author.AuthorId,
						AuthorCode = ba.Author.AuthorCode,
						AuthorImage = ba.Author.AuthorImage,
						FullName = ba.Author.FullName,
						Biography = ba.Author.Biography ?? string.Empty,
						Dob = ba.Author.Dob,
						DateOfDeath = ba.Author.DateOfDeath,
						Nationality = ba.Author.Nationality,
						CreateDate = ba.Author.CreateDate,
						UpdateDate = ba.Author.UpdateDate
					}).ToList()
				}).ToList(),
			};

		public static SearchBookResponse ToSearchBookResponse(this ISearchResponse<ElasticBook> searchResp,
			int pageIndex, int pageSize, long totalPage)
			=> new(searchResp.Documents.ToList(), pageIndex, pageSize, totalPage);
	}
}
