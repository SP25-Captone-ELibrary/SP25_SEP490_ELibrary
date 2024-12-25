using Nest;

namespace FPTU_ELibrary.Application.Elastic.Models
{
	public class ElasticCategory
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
