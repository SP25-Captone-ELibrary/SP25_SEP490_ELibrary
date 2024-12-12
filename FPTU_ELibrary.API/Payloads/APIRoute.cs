namespace FPTU_ELibrary.API.Payloads
{
	public class APIRoute
	{
		private const string Base = "api";

		/// <summary>
		/// Authentication endpoints
		/// </summary>
		public static class Authentication
		{
			// [GET]
			public const string ForgotPassword = Base + "/auth/forgot-password";
			public const string CurrentUser = Base + "/auth/current-user";
			// [POST]
			public const string SignIn = Base + "/auth/sign-in";
			public const string SignInAsEmployee = Base + "/employee/auth/sign-in";
			public const string SignInWithPassword = Base + "/auth/sign-in/password-method";
			public const string SignInWithOtp = Base + "/auth/sign-in/otp-method";
			public const string SignInWithGoogle = Base + "/auth/sign-in-google";
			public const string SignInWithFacebook = Base + "/auth/sign-in-facebook";
			public const string SignUp = Base + "/auth/sign-up";
			public const string RefreshToken = Base + "/auth/refresh-token";
			public const string ResendOtp = Base + "/auth/resend-otp";
			public const string ChangePasswordOtpVerification = Base + "/auth/change-password/verify-otp";
			// [PATCH]
			public const string ConfirmRegistration = Base + "/auth/sign-up/confirm";
			public const string ChangePassword = Base + "/auth/change-password";
            public const string ChangePasswordAsEmployee = Base + "/employee/auth/change-password";
		}

		/// <summary>
		/// Book endpoints
		/// </summary>
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

		/// <summary>
		/// User endpoints
		/// </summary>
		public static class User
		{
			// [Get]
			public const string GetAll = Base + "/users";
			public const string Search = Base + "/users/q";

			public const string GetById = Base + "/profile/{id}";
			// [POST]
			public const string Create = Base + "/users";
			public const string CreateManyWithSendEmail = Base + "/users/create-many-with-send-mails";
			public const string CreateMany = Base + "/createMany";
			// public const string CreateMany = Base + "/admin/createMany";
			// [PATCH]
			//users update their own account
			public const string Update = Base + "/profile/{id}";
			//admin update role from general user(GU) to Student or Teacher role
			public const string UpdateRole = Base + "/users/{id}/role";
			//[Put]
			public const string ChangeAccountStatus = Base + "/users/{id}/status";
			//[Delete]
			public const string HardDelete = Base + "/users/{id}";
		}

		/// <summary>
		/// SystemMessage endpoints
		/// </summary>
		public static class SystemMessage
		{
			// [POST]
			public const string ImportToExcel = Base + "/system-messages/import-excel";
			public const string ExportToExcel = Base + "/system-messages/export-excel";
		}

		/// <summary>
		/// System service healthcheck endpoints
		/// </summary>
		public static class HealthCheck
		{
			//	[GET]
			public const string BaseUrl = Base;
			public const string Check = Base + "/health-check";
		}

		/// <summary>
		/// Role management endpoints
		/// </summary>
		public static class Role
		{
			public const string GetAll = Base + "/roles";
		}

		/// <summary>
		/// Role management endpoints
		/// </summary>
		public static class Notification
		{
			//Create
			public const string Create = "/notifications";
			//Get	
			public const string GetTypes = "/notifications/types";
			public const string GetNotificationByAdmin = "/notifications";
			//private noti
			public const string GetNotificationNotByAdmin = "/privacy/{userId}/notifivations";

			public const string GetNumberOfUnreadNotifications = "/privacy/unread-noti";//filter unread notification
			//Put
			public const string UpdateReadStatus = "/privacy/notifications";
			//Delete
			public const string DeleteNotification = "/notifications/{notiId}";

		}
	}
}
