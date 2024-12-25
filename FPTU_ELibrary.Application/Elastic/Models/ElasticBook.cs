using Nest;

namespace FPTU_ELibrary.Application.Elastic.Models
{
    public class ElasticBook
    {
        [Number(NumberType.Integer)]
        public int BookId { get; set; }

        [Text]
        public string Title { get; set; } = null!;

        [Text]
        public string? Summary { get; set; }

        [Boolean]
        public bool IsDeleted { get; set; }

        [Boolean]
        public bool IsDraft { get; set; }

        [Boolean]
        public bool CanBorrow { get; set; }

        [Date]
        public DateTime CreatedAt { get; set; }

        [Date]
        public DateTime? UpdatedAt { get; set; }

        [Keyword] 
        public string CreatedBy { get; set; } = null!;

        [Keyword]
        public string? UpdatedBy { get; set; }

        [Nested]
        public List<ElasticCategory> Categories { get; set; } = null!;

        [Nested]
        public List<ElasticBookEdition> BookEditions { get; set; } = null!;
    }
}
