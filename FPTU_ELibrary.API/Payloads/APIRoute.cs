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

		public static class User
		{
			// [Get]
			public const string GetAll = Base + "/users";
			public const string Search = Base + "/users/q";
			// [POST]
			public const string Create = Base + "/admin";

			public const string CreateMany = Base + "/createMany";
			// public const string CreateMany = Base + "/admin/createMany";
			// [PATCH]
			//users update their own account
			public const string Update = Base + "/users/{id}";
			//admin update role from general user(GU) to Student or Teacher role
			public const string UpdateRole = Base + "/admin/{id}/role";
			//[Put]
			public const string ChangeAccountStatus = Base + "/admin/{id}/status";
			//[Delete]
			public const string HardDelete = Base + "/admin/{id}";
		}
	}
}
