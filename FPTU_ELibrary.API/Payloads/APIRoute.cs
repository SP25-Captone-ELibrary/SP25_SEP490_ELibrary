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
			public const string SignInWithPassword = Base + "/auth/sign-in/password-method";
			public const string SignInWithOtp = Base + "/auth/sign-in/otp-method";
			public const string SignInAsEmployee = Base + "/auth/employee/sign-in";
			public const string SignInAsAdmin = Base + "/auth/admin/sign-in";
			public const string SignInWithPasswordAsEmployee = Base + "/auth/employee/sign-in/password-method";
			public const string SignInWithGoogle = Base + "/auth/sign-in-google";
			public const string SignInWithFacebook = Base + "/auth/sign-in-facebook";
			public const string SignUp = Base + "/auth/sign-up";
			public const string RefreshToken = Base + "/auth/refresh-token";
			public const string ResendOtp = Base + "/auth/resend-otp";
			public const string ChangePasswordOtpVerification = Base + "/auth/change-password/verify-otp";
			// [PATCH]
			public const string ConfirmRegistration = Base + "/auth/sign-up/confirm";
			public const string ChangePassword = Base + "/auth/change-password";
            public const string ChangePasswordAsEmployee = Base + "/auth/employee/change-password";
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
			// [GET]
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
		/// Employee endpoints
		/// </summary>
		public static class Employee
		{
			// [GET]
			public const string GetAll = Base + "/employees";
			// [POST]
			public const string Create = Base + "/employees";
			public const string Import = Base + "/employees/import";
			// [PUT]
			public const string Update = Base + "/employees/{id}";
			// [PATCH]
			public const string ChangeActiveStatus = Base + "/employees/{id}/status";
			// [DELETE]
			public const string SoftDelete = Base + "/employees/{id}/soft-delete";
			public const string Delete = Base + "/employees/{id}";
		}

		/// <summary>
		/// Resource endpoints
		/// </summary>
		public static class Resource
		{
			// [GET]
			public const string GetAllType = "/resources/types";
			// [POST]
			public const string UploadImage = Base + "/resources/images/upload";
			public const string UploadVideo = Base + "/resources/videos/upload";
			// [PUT]
			public const string UpdateImage = Base + "/resources/images/update";
			public const string UpdateVideo = Base + "/resources/videos/update";
			// [DELETE]
			public const string DeleteImage = Base + "/resources/images";
			public const string DeleteVideo = Base + "/resources/videos";
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
		/// Role management endpoints
		/// </summary>
		public static class Role
		{
			//	[GET]
			public const string GetAllRoleType = Base + "/roles/types";
			public const string GetAllRole = Base + "/roles";
			public const string GetById = Base + "/roles/{id}";
			public const string GetAllUserRole = Base + "/roles/users";
			public const string GetAllEmployeeRole = Base + "/roles/employees";
			public const string GetAllPermission = Base + "/roles/permissions";
			public const string GetAllFeature = Base + "/roles/features";
			public const string GetRolePermissionTable = Base + "/roles/user-permissions";
			//	[POST]
			public const string CreateRole = Base + "/roles";
			//	[PUT]
			public const string UpdateRole = Base + "/roles/{id}";
			//	[PATCH]
			public const string UpdateRolePermission = Base + "/roles/user-permissions";
			public const string UpdateUserRole = Base + "/roles/users";
			public const string UpdateEmployeeRole = Base + "/roles/employees";
			//	[DELETE]
			public const string DeleteRole = Base + "/roles";
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
		public static class Notification
		{
			//Create
			public const string Create =Base + "/notifications";
			//Get	
			public const string GetTypes =Base + "/notifications/types";
			public const string GetNotificationByAdmin =Base + "/notifications";
			//private noti
			public const string GetNotificationNotByAdmin =Base + "/privacy/notifivations";

			public const string GetNumberOfUnreadNotifications =Base + "/privacy/unread-noti";//filter unread notification
			//Put
			public const string UpdateReadStatus =Base + "/privacy/notifications";
			//Delete
			public const string DeleteNotification =Base + "/notifications/{notiId}";

		}
	}
}
