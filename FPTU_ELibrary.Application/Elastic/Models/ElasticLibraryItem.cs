using Nest;

namespace FPTU_ELibrary.Application.Elastic.Models
{
    public class ElasticLibraryItem
    {
        [Number(NumberType.Integer, Name = "library_item_id")]
        public int LibraryItemId { get; set; }

        [Text(Name = "title")] 
        public string Title { get; set; } = null!;
        
        [Text(Name = "sub_title")] 
        public string? SubTitle { get; set; } 
       
        [Text(Name = "responsibility")] 
        public string? Responsibility { get; set; } 
        
        [Text(Name = "edition")] 
        public string? Edition { get; set; } 
        
        [Number(NumberType.Integer, Name = "edition_number")]
        public int? EditionNumber { get; set; }
        
        [Text(Name = "summary")]
        public string? Summary { get; set; }

        [Keyword(Name = "language")] 
        public string Language { get; set; } = null!;
        
        [Keyword(Name = "origin_language")] 
        public string? OriginLanguage { get; set; } 
        
        [Keyword(Name = "cover_image")]
        public string? CoverImage { get; set; } 
        
        [Number(NumberType.Integer, Name = "publication_year")]
        public int PublicationYear { get; set; }
        
        [Text(Name = "publisher")] 
        public string? Publisher { get; set; } 
        
        [Text(Name = "publication_place")] 
        public string? PublicationPlace { get; set; } 
        
        [Text(Name = "classification_number")] 
        public string ClassificationNumber { get; set; } = null!;
        
        [Text(Name = "cutter_number")] 
        public string CutterNumber { get; set; } = null!;
        
        [Text(Name = "isbn")] 
        public string? Isbn { get; set; } 
        
        [Keyword(Name = "ean")] 
        public string? Ean { get; set; } 
        
        [Number(NumberType.Double, Name = "estimated_price")]
        public decimal? EstimatedPrice { get; set; }
        
        [Number(NumberType.Integer, Name = "page_count")]
        public int PageCount { get; set; }
        
        [Text(Name = "physical_details")] 
        public string? PhysicalDetails { get; set; } 
        
        [Text(Name = "dimensions")] 
        public string? Dimensions { get; set; } 
        
        [Text(Name = "genres")] 
        public string? Genres { get; set; } 
        
        [Text(Name = "topical_terms")] 
        public string? TopicalTerms { get; set; } 
        
        [Text(Name = "general_note")] 
        public string? GeneralNote { get; set; }
        
        [Text(Name = "additional_authors")] 
        public string? AdditionalAuthors { get; set; }
        
        [Number(NumberType.Integer, Name = "category_id")]
        public int? CategoryId { get; set; }
        
        [Number(NumberType.Integer, Name = "shelf_id")]
        public int? ShelfId { get; set; }
        
        [Number(NumberType.Integer, Name = "group_id")]
        public int? GroupId { get; set; }

        [Keyword(Name = "status")] 
        public string Status { get; set; } = null!; 
        
        [Boolean(Name = "is_deleted")]
        public bool IsDeleted { get; set; }

        [Boolean(Name = "can_borrow")]
        public bool CanBorrow { get; set; }
        
        [Boolean(Name = "is_trained")]
        public bool IsTrained { get; set; }
        
        [Object(Name = "library_item_inventory")]
        public ElasticLibraryItemInventory? LibraryItemInventory { get; set; } 
            
        [Nested(Name = "library_item_instances")] 
        public List<ElasticLibraryItemInstance> LibraryItemInstances { get; set; } = new();
        
        [Nested(Name = "authors")]
        public List<ElasticAuthor> Authors { get; set; } = null!;
    }
}
