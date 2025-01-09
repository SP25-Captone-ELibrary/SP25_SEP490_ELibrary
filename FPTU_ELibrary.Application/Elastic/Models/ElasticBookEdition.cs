using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPTU_ELibrary.Application.Elastic.Models
{
    public class ElasticBookEdition
    {
        [Number(NumberType.Integer, Name = "book_edition_id")]
        public int BookEditionId { get; set; }

        [Number(NumberType.Integer, Name = "book_id")]
        public int BookId { get; set; }

        [Text(Name = "edition_title")]
        public string? EditionTitle { get; set; } 
       
        [Text(Name = "edition_summary")]
        public string? EditionSummary { get; set; }

        [Number(NumberType.Integer, Name = "edition_number")]
        public int EditionNumber { get; set; }

        [Number(NumberType.Integer, Name = "publication_year")]
        public int PublicationYear { get; set; }

        [Number(NumberType.Integer, Name = "page_count")]
        public int PageCount { get; set; }

        [Keyword(Name = "language")]
        public string Language { get; set; } = null!;

        [Keyword(Name = "cover_image")]
        public string? CoverImage { get; set; }

        [Keyword(Name = "format")]
        public string? Format { get; set; }

        [Text(Name = "publisher")]
        public string? Publisher { get; set; }

        [Keyword(Name = "isbn")]
        public string Isbn { get; set; } = null!;

        [Boolean(Name = "is_deleted")]
        public bool IsDeleted { get; set; }

        [Boolean(Name = "can_borrow")]
        public bool CanBorrow { get; set; }
        
        [Keyword(Name = "status")] 
        public string Status { get; set; } = null!;
        
        [Date(Name = "created_at")]
        public DateTime CreatedAt { get; set; }

        [Date(Name = "updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Keyword(Name = "created_by")] 
        public string CreatedBy { get; set; } = null!;

        [Keyword(Name = "updated_by")]
        public string? UpdatedBy { get; set; }
        
        [Nested(Name = "authors")]
        public List<ElasticAuthor> Authors { get; set; } = null!;
    }
}
