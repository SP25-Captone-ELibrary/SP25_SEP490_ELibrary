namespace FPTU_ELibrary.API.Payloads
{
	public class APIRoute
	{
		private const string Base = "api";

		public static class Authentication
		{

		}

		public static class Book
		{
			// [GET]
			public const string GetAll = Base + "/books";
			public const string Search = Base + "/books/q";
			// [POST]
			public const string Create = Base + "/books";
			// [PUT]
			public const string Update = Base + "/books";
			// [PATCH]
			// [DELETE]
		}
	}
}
