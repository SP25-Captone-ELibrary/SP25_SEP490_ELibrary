using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPTU_ELibrary.Application.Elastic.Models
{
    public class ElasticAuthor
    {
        [Number(NumberType.Integer)]
        public int AuthorId { get; set; }

        [Keyword]
        public string? AuthorCode { get; set; }

        [Keyword]
        public string? AuthorImage { get; set; }

        [Text]
        public string FullName { get; set; } = null!;

        [Text]
        public string Biography { get; set; } = null!;

        [Date]
        public DateTime? Dob { get; set; }

        [Date]
        public DateTime? DateOfDeath { get; set; }

        [Keyword]
        public string? Nationality { get; set; }

        [Date]
        public DateTime CreateDate { get; set; }

        [Date]
        public DateTime? UpdateDate { get; set; }
    }
}
