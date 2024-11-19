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
        [Number(NumberType.Integer)]
        public int BookEditionId { get; set; }

        [Number(NumberType.Integer)]
        public int BookId { get; set; }

        [Text]
        public string EditionTitle { get; set; } = null!;

        [Number(NumberType.Integer)]
        public int EditionNumber { get; set; }

        [Number(NumberType.Integer)]
        public int PublicationYear { get; set; }

        [Number(NumberType.Integer)]
        public int PageCount { get; set; }

        [Keyword]
        public string Language { get; set; } = null!;

        [Keyword]
        public string? CoverImage { get; set; }

        [Keyword]
        public string? Format { get; set; }

        [Text]
        public string? Publisher { get; set; }

        [Keyword]
        public string Isbn { get; set; } = null!;

        [Boolean]
        public bool IsDeleted { get; set; }

        [Date]
        public DateTime CreateDate { get; set; }

        [Date]
        public DateTime? UpdatedDate { get; set; }

        [Keyword]
        public Guid CreateBy { get; set; }
    }
}
