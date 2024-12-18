using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

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

        [Number(NumberType.Integer)]
        public int CategoryId { get; set; }

        [Date]
        public DateTime CreateDate { get; set; }

        [Date]
        public DateTime? UpdatedDate { get; set; }

        [Keyword]
        public Guid CreateBy { get; set; }

        [Keyword]
        public Guid? UpdatedBy { get; set; }

        [Object]
        public ElasticBookCategory BookCategory { get; set; } = null!;

        [Nested]
        public List<ElasticBookEdition> BookEditions { get; set; } = null!;
    }
}
