using FPTU_ELibrary.Application.Dtos;
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
				CategoryId = booKDto.CategoryId,
				IsDeleted = booKDto.IsDeleted,
				IsDraft = booKDto.IsDraft,
				CanBorrow = booKDto.CanBorrow,
				CreateDate = booKDto.CreateDate,
				CreateBy = booKDto.CreateBy,
				UpdatedDate = booKDto.UpdatedDate,
				UpdatedBy = booKDto.UpdatedBy,
				BookCategory = new ElasticBookCategory
				{
					CategoryId = booKDto.CategoryId,
					EnglishName = booKDto.Category.EnglishName,
					VietnameseName = booKDto.Category.VietnameseName,
					Description = booKDto.Category.Description ?? string.Empty,
				},
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
					CreateDate = be.CreateDate,
					UpdatedDate = be.UpdatedDate,
					CreateBy = be.CreateBy,
					Authors = booKDto.BookAuthors.Select(ba => new ElasticAuthor
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
