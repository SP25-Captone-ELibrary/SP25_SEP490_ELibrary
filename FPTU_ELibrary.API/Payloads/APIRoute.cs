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
			public const string GetMfaBackupAsync = Base + "/auth/mfa-backup";
			// [POST]
			public const string SignIn = Base + "/auth/sign-in";
			public const string SignInAsEmployee = Base + "/auth/employee/sign-in";
			public const string SignInAsAdmin = Base + "/auth/admin/sign-in";
			public const string SignInWithPassword = Base + "/auth/sign-in/password-method";
			public const string SignInWithOtp = Base + "/auth/sign-in/otp-method";
			public const string SignInWithGoogle = Base + "/auth/sign-in-google";
			public const string SignInWithFacebook = Base + "/auth/sign-in-facebook";
			public const string SignUp = Base + "/auth/sign-up";
			public const string RefreshToken = Base + "/auth/refresh-token";
			public const string ResendOtp = Base + "/auth/resend-otp";
			public const string ChangePasswordOtpVerification = Base + "/auth/change-password/verify-otp";
			public const string EnableMfa = Base + "/auth/enable-mfa";
			public const string ValidateMfa = Base + "/auth/validate-mfa";
			public const string ValidateBackupCode = Base + "/auth/validate-mfa-backup";
			public const string RegenerateBackupCode = Base + "/auth/regenerate-mfa-backup";
			public const string RegenerateBackupCodeConfirm = Base + "/auth/regenerate-mfa-backup/confirm";
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
			#region Management
			// [GET]
			public const string GetAll = Base + "/management/users";
			public const string Search = Base + "/management/users/q";
			// [POST]
			public const string Create = Base + "/management/users";
			public const string CreateManyWithSendEmail = Base + "/management/users/create-many-with-send-mails";
			public const string CreateMany = Base + "/management/createMany";
			// [PUT]
			public const string ChangeAccountStatus = Base + "/management/users/{id}/status";
			// [PATCH]
			// [DELETE]
			public const string HardDelete = Base + "/management/users/{id}";
			
			// public const string CreateMany = Base + "/admin/createMany";
			//admin update role from general user(GU) to Student or Teacher role
			// public const string UpdateRole = Base + "/users/{id}/role";
			#endregion
			
			// [GET]
			public const string GetById = Base + "/profile/{id}";
			// [POST]
			// [PATCH]
			public const string Update = Base + "/profile/{id}"; //users update their own account
			// [PUT]
			// [PATCH]
			// [DELETE]
		}

		/// <summary>
		/// Employee endpoints
		/// </summary>
		public static class Employee
		{
			#region Management
			// [GET]
			public const string GetAll = Base + "/management/employees";
			public const string Export = Base + "/management/employees/export";
			// [POST]
			public const string Create = Base + "/management/employees";
			public const string Import = Base + "/management/employees/import";
			// [PUT]
			public const string Update = Base + "/management/employees/{id}";
			public const string UpdateProfile = Base + "/management/employees/{id}/profile";
			// [PATCH]
			public const string ChangeActiveStatus = Base + "/management/employees/{id}/status";
			// [DELETE]
			public const string SoftDelete = Base + "/management/employees/{id}/soft-delete";
			public const string UndoDelete = Base + "/management/employees/{id}/undo-delete";
			public const string Delete = Base + "/management/employees/{id}";
			#endregion
		}

		/// <summary>
		/// Author endpoints
		/// </summary>
		public static class Author
		{
			public const string GetAll = Base + "/management/authors";
		}
		
		/// <summary>
		/// Resource endpoints
		/// </summary>
		public static class Resource
		{
			// [GET]
			public const string GetAllType = Base + "/management/resources/types";
			// [POST]
			public const string UploadImage = Base + "/management/resources/images/upload";
			public const string UploadVideo = Base + "/management/resources/videos/upload";
			// [PUT]
			public const string UpdateImage = Base + "/management/resources/images/update";
			public const string UpdateVideo = Base + "/management/resources/videos/update";
			// [DELETE]
			public const string DeleteImage = Base + "/management/resources/images";
			public const string DeleteVideo = Base + "/management/resources/videos";
		}
		
		/// <summary>
		/// SystemMessage endpoints
		/// </summary>
		public static class SystemMessage
		{
			// [POST]
			public const string ImportToExcel = Base + "/management/system-messages/import-excel";
			public const string ExportToExcel = Base + "/management/system-messages/export-excel";
		}

		/// <summary>
		/// Role management endpoints
		/// </summary>
		public static class Role
		{
			#region Management
			//	[GET]
			public const string GetAllRoleType = Base + "/management/roles/types";
			public const string GetAllRole = Base + "/management/roles";
			public const string GetById = Base + "/management/roles/{id}";
			public const string GetAllUserRole = Base + "/management/roles/users";
			public const string GetAllEmployeeRole = Base + "/management/roles/employees";
			public const string GetAllPermission = Base + "/management/roles/permissions";
			public const string GetAllFeature = Base + "/management/roles/features";
			public const string GetRolePermissionTable = Base + "/management/roles/user-permissions";
			//	[POST]
			public const string CreateRole = Base + "/management/roles";
			//	[PUT]
			public const string UpdateRole = Base + "/management/roles/{id}";
			//	[PATCH]
			public const string UpdateRolePermission = Base + "/management/roles/user-permissions";
			public const string UpdateUserRole = Base + "/management/roles/users";
			public const string UpdateEmployeeRole = Base + "/management/roles/employees";
			//	[DELETE]
			public const string DeleteRole = Base + "/management/roles/{id}";
			#endregion
		}

		/// <summary>
		/// Feature endpoints
		/// </summary>
		public static class Feature
		{
			//	[GET]
			public const string GetAuthorizedUserFeatures = Base + "/features/authorized";
			public const string GetFeaturePermission = Base + "/features/{id}/authorized-permission";
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
			#region Management
			//	[GET]	
			public const string Create = Base + "/management/notifications";
			public const string GetTypes = Base + "/management/notifications/types";
			public const string GetNotificationByAdmin = Base + "/management/notifications";
			//	[POST]
			//	[PUT]
			//	[PATCH]
			//	[DELETE]
			public const string DeleteNotification = Base + "/management/notifications/{notiId}";
			#endregion
			
			//	[GET]
			//	[POST]
			public const string GetNotificationNotByAdmin = Base + "/privacy/notifivations";
			public const string GetNumberOfUnreadNotifications = Base + "/privacy/unread-noti"; //filter unread notification
			//	[PUT]
			public const string UpdateReadStatus = Base + "/privacy/notifications";
			//	[PATCH]
			//	[DELETE]
		}

		/// <summary>
		/// BookCategory endpoints
		/// </summary>
		public static class BookCategory
		{
			public const string Create = "/book-categories";
			public const string Update = "/book-categories/{id}";
			public const string Delete = "/book-categories/{id}";
		}
	}
}
