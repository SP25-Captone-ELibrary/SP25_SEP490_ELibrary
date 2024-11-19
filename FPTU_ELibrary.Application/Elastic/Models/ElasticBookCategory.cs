using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPTU_ELibrary.Application.Elastic.Models
{
	public class ElasticBookCategory
	{
        [Number(NumberType.Integer)]
        public int CategoryId { get; set; }

        [Keyword]
        public string EnglishName { get; set; } = null!;

		[Keyword]
		public string VietnameseName { get; set; } = null!;

		[Keyword]
        public string Description { get; set; } = null!;
    }
}
