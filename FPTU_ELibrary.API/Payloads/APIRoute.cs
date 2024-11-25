namespace FPTU_ELibrary.API.Payloads
{
	public class APIRoute
	{
		private const string Base = "api";

		public static class Authentication
		{
			// [GET]
			public const string SignInWithGoogle = Base + "/auth/sign-in-google";
			public const string GoogleCallback = Base + "/auth/google-callback";
			// [POST]
			public const string SignIn = Base + "/auth/sign-in";
			public const string SignUp = Base + "/auth/sign-up";
			public const string RefreshToken = Base + "/auth/refresh-token";
			// [PATCH]
			public const string ConfirmRegistration = Base + "/auth/sign-up/confirm";
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
