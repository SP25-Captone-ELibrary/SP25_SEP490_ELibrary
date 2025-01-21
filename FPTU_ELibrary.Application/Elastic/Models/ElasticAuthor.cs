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
        [Number(NumberType.Integer, Name = "author_id")]
        public int AuthorId { get; set; }

        [Text(Name = "author_code")] 
        public string AuthorCode { get; set; } = null!;
        
        [Keyword(Name = "author_image")]
        public string? AuthorImage { get; set; }

        [Text(Name = "full_name")]
        public string FullName { get; set; } = null!;

        [Text(Name = "biography")]
        public string? Biography { get; set; } 

        [Date(Name = "dob")]
        public DateTime? Dob { get; set; }

        [Date(Name = "date_of_death")]
        public DateTime? DateOfDeath { get; set; }

        [Keyword(Name = "nationality")]
        public string? Nationality { get; set; }

        [Boolean(Name = "is_deleted")] 
        public bool IsDeleted { get; set; }
    }
}
