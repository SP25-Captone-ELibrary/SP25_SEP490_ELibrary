using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums
{
	public enum BookCategory
	{
		[Description("Tình cảm")]
		Romance,

		[Description("Bí ẩn")]
		Mystery,
	
		[Description("Khoa học viễn tưởng và giả tưởng")]
		FantasyAndScienceFiction,

		[Description("Kinh dị và giật gân")]
		ThrillerAndHorror,

		[Description("Truyện ngắn")]
		ShortStories,

		[Description("Tiểu sử")]
		Biography,

		[Description("Sách nấu ăn")]
		CookBooks,

		[Description("Tiểu luận")]
		Essays,

		[Description("Tiểu thuyết")]
		Novels,

		[Description("Tự lực")]
		SelfHelp,

		[Description("Lịch sử")]
		History,

		[Description("Thơ ca")]
		Poetry,

		[Description("Sách thiếu nhi")]
		Children,

		[Description("Kinh doanh và đầu tư")]
		BusinessAndInvesting,

		[Description("Giáo dục")]
		Education,

		[Description("Chính trị")]
		Politics,

		[Description("Tôn giáo và tâm linh")]
		ReligionAndSpirituality,

		[Description("Kỹ năng sống")]
		LifeSkills,

		[Description("Chăm sóc sức khỏe")]
		HealthAndWellness,

		[Description("Khoa học và công nghệ")]
		ScienceAndTechnology,

		[Description("Du lịch và địa lý")]
		TravelAndGeography,

		[Description("Hài hước")]
		Humor
	}
}
