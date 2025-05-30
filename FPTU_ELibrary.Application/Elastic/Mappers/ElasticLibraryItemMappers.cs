﻿using System.Net;
using System.Text.RegularExpressions;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Elastic.Models;
using FPTU_ELibrary.Application.Elastic.Responses;
using HtmlAgilityPack;
using Nest;

namespace FPTU_ELibrary.Application.Elastic.Mappers
{
	public static class ElasticLibraryItemMappers
	{
		public static ElasticLibraryItem ToElasticLibraryItem(this LibraryItemDto itemDto)
			=> new()
			{
				LibraryItemId = itemDto.LibraryItemId,
				Title = itemDto.Title,
				SubTitle = itemDto.SubTitle ?? string.Empty,
				Responsibility = itemDto.Responsibility,
				Edition = itemDto.Edition,
				EditionNumber = itemDto.EditionNumber,
				Summary = itemDto.Summary,
				Language = itemDto.Language,
				OriginLanguage = itemDto.OriginLanguage,
				CoverImage = itemDto.CoverImage,
				Publisher = itemDto.Publisher,
				PublicationYear = itemDto.PublicationYear,
				PublicationPlace = itemDto.PublicationPlace,
				ClassificationNumber = itemDto.ClassificationNumber,
				CutterNumber = itemDto.CutterNumber,
				Isbn = itemDto.Isbn,
				Ean = itemDto.Ean,
				EstimatedPrice = itemDto.EstimatedPrice,
				PageCount = itemDto.PageCount ?? 0,
				PhysicalDetails = itemDto.PhysicalDetails,
				Dimensions = itemDto.Dimensions,
				Genres = itemDto.Genres,
				TopicalTerms = itemDto.TopicalTerms,
				AdditionalAuthors = itemDto.AdditionalAuthors,
				GeneralNote = itemDto.GeneralNote,
				CategoryId = itemDto.CategoryId,
				ShelfId = itemDto.ShelfId,
				GroupId = itemDto.GroupId,
				Status = itemDto.Status.ToString(),
				IsDeleted = itemDto.IsDeleted,
				CanBorrow = itemDto.CanBorrow,
				IsTrained = itemDto.IsTrained,
				LibraryItemInventory = itemDto.LibraryItemInventory != null 
					? new ElasticLibraryItemInventory()
					{
						LibraryItemId = itemDto.LibraryItemInventory.LibraryItemId,
						TotalUnits = itemDto.LibraryItemInventory.TotalUnits,
						AvailableUnits = itemDto.LibraryItemInventory.AvailableUnits,
						RequestUnits = itemDto.LibraryItemInventory.RequestUnits,
						BorrowedUnits = itemDto.LibraryItemInventory.BorrowedUnits,
						ReservedUnits = itemDto.LibraryItemInventory.ReservedUnits,
					}
					: null,
				LibraryItemInstances = itemDto.LibraryItemInstances.Select(bec => new ElasticLibraryItemInstance()
				{
					LibraryItemInstanceId = bec.LibraryItemInstanceId,
					LibraryItemId = bec.LibraryItemId,
					Barcode = bec.Barcode,
					Status = bec.Status,
					IsDeleted = bec.IsDeleted
				}).ToList(),
				Authors = itemDto.LibraryItemAuthors.Select(ba => new ElasticAuthor
				{
					AuthorId = ba.Author.AuthorId,
					AuthorCode = ba.Author.AuthorCode,
					AuthorImage = ba.Author.AuthorImage,
					FullName = ba.Author.FullName,
					Biography = ProcessBiography(ba.Author.Biography),
					Dob = ba.Author.Dob,
					DateOfDeath = ba.Author.DateOfDeath,
					Nationality = ba.Author.Nationality,
					IsDeleted = ba.Author.IsDeleted
				}).ToList()
			};

		public static SearchBookResponse ToSearchBookResponse(this ISearchResponse<ElasticLibraryItem> searchResp,
			int pageIndex, int pageSize, long totalPage)
			=> new(searchResp.Documents.ToList(), pageIndex, pageSize, totalPage);

		public static SearchBookEditionResponse ToSearchLibraryItemResponse(
			this ISearchResponse<ElasticLibraryItem> searchResp,
			int pageIndex, int pageSize, int totalPage, int totalActualItems)
			=> new(searchResp.Documents.ToList(), pageIndex, pageSize, totalPage, totalActualItems);

		private static string ProcessBiography(string biographyHtml)
		{
			if (string.IsNullOrEmpty(biographyHtml))
				return string.Empty;

			// Decode HTML entities first
			string decodedHtml = WebUtility.HtmlDecode(biographyHtml);
			
			// parse and extract plain text
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(decodedHtml);

			return htmlDoc.DocumentNode.InnerText.Trim(); 
		}
	}
}
