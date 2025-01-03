using Nest;

namespace FPTU_ELibrary.Application.Elastic.Models
{
    public class ElasticBook
    {
        [Number(NumberType.Integer, Name = "book_id")]
        public int BookId { get; set; }

        [Text(Name = "title")]
        public string Title { get; set; } = null!;

        [Text(Name = "sub_title")] 
        public string SubTitle { get; set; } = null!; 

        [Text(Name = "summary")]
        public string? Summary { get; set; }

        [Boolean(Name = "is_deleted")]
        public bool IsDeleted { get; set; }

        [Date(Name = "created_at")]
        public DateTime CreatedAt { get; set; }

        [Date(Name = "updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Keyword(Name = "created_by")] 
        public string CreatedBy { get; set; } = null!;

        [Keyword(Name = "updated_by")]
        public string? UpdatedBy { get; set; }

        [Nested(Name = "categories")]
        public List<ElasticCategory> Categories { get; set; } = null!;

        [Nested(Name = "book_editions")]
        public List<ElasticBookEdition> BookEditions { get; set; } = null!;
    }
}
