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
            // [PUT]
            public const string UpdateProfile = Base + "/auth/profile";
		}

		/// <summary>
		/// Audit trail endpoints
		/// </summary>
		public static class AuditTrail
		{
			#region BookManagement
			public const string GetAllByEntityIdAndName = Base + "/management/books/audit-trails";
			public const string GetDetailByDateUtc = Base + "/management/books/audit-trails/detail";
			#endregion

			#region RoleManagement

			// 

			#endregion
		}
		
		/// <summary>
		/// Book endpoints
		/// </summary>
		public static class Book
		{
			#region management
			// [GET]
			public const string GetEnums = Base + "/management/books/enums";
			public const string GetById = Base + "/management/books/{id}";
			// [POST]
			public const string Create = Base + "/management/books";
			// [PUT] / [PATCH]
			public const string Update = Base + "/management/books/{id}";
			// [DELETE]
			public const string Delete = Base + "/management/books/{id}";
			#endregion
			
			public const string Search = Base + "/books/q";
		}

		/// <summary>
		/// Book edition endpoints
		/// </summary>
		public static class LibraryItem
		{
			#region management
			// [GET]
			public const string GetAll = Base + "/management/library-items";
			public const string GetDetail = Base + "/management/library-items/{id}";
			public const string CountTotalInstance = Base + "/management/books/editions/{id}/total-copy";
			public const string CountRangeTotalInstance = Base + "/management/books/editions/total-copy";
			public const string Export = Base + "/management/books/editions/export";
			// [POST]
			public const string Create = Base + "/management/library-items";
			public const string AddAuthor = Base + "/management/library-items/add-author";
			public const string AddRangeAuthor = Base + "/management/library-items/add-range-author";
			public const string DeleteAuthor = Base + "/management/library-items/delete-author";
			public const string Training = Base + "/management/books/editions/{id}/ai/train";
			public const string CheckImagesForTraining = Base + "/management/books/editions/{id}/ai/check-images-for-training";
			public const string DeleteRangeAuthor = Base + "/management/library-items/delete-range-author";
			public const string Import = Base + "/management/books/editions/import";
			// [PUT] / [PATCH]
			public const string Update = Base + "/management/books/editions/{id}";
			public const string UpdateStatus = Base + "/management/books/editions/{id}/status";
			public const string UpdateShelfLocation = Base + "/management/library-items/{id}/shelf-location";
			public const string SoftDelete = Base + "/management/books/editions/{id}/soft-delete";
			public const string SoftDeleteRange = Base + "/management/books/editions/soft-delete-range";
			public const string UndoDelete = Base + "/management/books/editions/{id}/undo-delete";
			public const string UndoDeleteRange = Base + "/management/books/editions/undo-delete-range";
			// [DELETE]
			public const string Delete = Base + "/management/books/editions/{id}";
			public const string DeleteRange = Base + "/management/books/editions";
			#endregion
		}

		/// <summary>
		/// Book resource endpoints
		/// </summary>
		public static class BookResource
		{
			#region management
			// [GET]
			public const string GetAll = Base + "/management/books/resources";
			public const string GetById = Base + "/management/books/resources/{id}";
			// [POST]
			public const string AddToBook = Base + "/management/books/{bookId}/resources";
			// [PUT] / [PATCH]
			public const string Update = Base + "/management/books/resources/{id}";
			public const string SoftDelete = Base + "/management/books/resources/{id}/soft-delete";
			public const string SoftDeleteRange = Base + "/management/books/resources/soft-delete-range";
			public const string UndoDelete = Base + "/management/books/resources/{id}/undo-delete";
			public const string UndoDeleteRange = Base + "/management/books/resources/undo-delete-range";
			// [DELETE]
			public const string Delete = Base + "/management/books/resources/{id}";
			public const string DeleteRange = Base + "/management/books/resources";

			#endregion
		}

		public static class BookEditionCopy
		{
			#region Management
			// [GET]
			public const string GetById = Base + "/management/books/editions/copies/{id}";
			// [POST]
			public const string AddRange = Base + "/management/books/editions/{id}/copies";
			// [PUT] / [PATCH]
			public const string Update = Base + "/management/books/editions/copies/{id}";
			public const string UpdateRange = Base + "/management/books/editions/{bookEditionId}/copies";
			public const string SoftDelete = Base + "/management/books/editions/copies/{id}/soft-delete";
			public const string SoftDeleteRange = Base + "/management/books/editions/{bookEditionId}/copies/soft-delete-range";
			public const string UndoDelete = Base + "/management/books/editions/copies/{id}/undo-delete";
			public const string UndoDeleteRange = Base + "/management/books/editions/{bookEditionId}/copies/undo-delete-range";
			// [DELETE]
			public const string Delete = Base + "/management/books/editions/copies/{id}";
			public const string DeleteRange = Base + "/management/books/editions/{bookEditionId}/copies";
			#endregion
		}
		
		/// <summary>
		/// User endpoints
		/// </summary>
		public static class User
		{
			#region Management
			// [GET]
			public const string GetById = Base + "/management/users/{id}";
			public const string GetAll = Base + "/management/users";
			public const string Export = Base + "/management/users/export";
			// [POST]
			public const string Create = Base + "/management/users";
			public const string Import = Base + "/management/users/import";
			// [PUT]
			public const string Update = Base + "/management/users/{id}";
			// [PATCH]
			public const string ChangeAccountStatus = Base + "/management/users/{id}/status";
			public const string SoftDelete = Base + "/management/users/{id}/soft-delete";
			public const string SoftDeleteRange = Base + "/management/users/soft-delete-range";
			public const string UndoDelete = Base + "/management/users/{id}/undo-delete";
			public const string UndoDeleteRange = Base + "/management/users/undo-delete-range";
			// [DELETE]
			public const string HardDelete = Base + "/management/users/{id}";
			public const string HardDeleteRange = Base + "/management/users";
			
			// public const string CreateMany = Base + "/admin/createMany";
			//admin update role from general user(GU) to Student or Teacher role
			// public const string UpdateRole = Base + "/users/{id}/role";
			#endregion
			
			// [GET]
			// [POST]
			// [PATCH]
			// public const string Update = Base + "/profile/{id}"; //users update their own account
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
			public const string GetById = Base + "/management/employees/{id}";
			public const string Export = Base + "/management/employees/export";
			// [POST]
			public const string Create = Base + "/management/employees";
			public const string Import = Base + "/management/employees/import";
			// [PUT]
			public const string Update = Base + "/management/employees/{id}";
			// [PATCH]
			public const string ChangeActiveStatus = Base + "/management/employees/{id}/status";
			public const string SoftDelete = Base + "/management/employees/{id}/soft-delete";
			public const string SoftDeleteRange = Base + "/management/employees/soft-delete-range";
			public const string UndoDelete = Base + "/management/employees/{id}/undo-delete";
			public const string UndoDeleteRange = Base + "/management/employees/undo-delete-range";
			// [DELETE]
			public const string Delete = Base + "/management/employees/{id}";
			public const string DeleteRange = Base + "/management/employees";
			#endregion
		}

		/// <summary>
		/// Author endpoints
		/// </summary>
		public static class Author
		{
			#region Management
			//	[GET]
			public const string GetAll = Base + "/management/authors";
			public const string GetById = Base + "/management/authors/{id}";
			public const string Export = Base + "/management/authors/export";
			//	[POST]
			public const string Create = Base + "/management/authors";
			public const string Import = Base + "/management/authors/import";
			//	[PUT] | [PATCH]
			public const string Update = Base + "/management/authors/{id}";
			public const string UndoDelete = Base + "/management/authors/{id}/undo-delete";
			public const string UndoDeleteRange = Base + "/management/authors/undo-delete-range";
			public const string SoftDelete = Base + "/management/authors/{id}/soft-delete";
			public const string SoftDeleteRange = Base + "/management/authors/soft-delete-range";
			//	[DELETE]
			public const string Delete = Base + "/management/authors/{id}";
			public const string DeleteRange = Base + "/management/authors";
			
			#endregion
			
			//	[GET]
			public const string GetAuthorDetail = Base + "/authors/{id}";
			//	[POST]
			//	[PUT] | [PATCH]
			//	[DELETE]
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
		/// Library shelf
		/// </summary>
		public static class LibraryLocation
		{
			#region Management
			//	[GET]
			public const string GetFloors = Base + "/management/location/floors";
			public const string GetShelvesForFilter = Base + "/management/location/shelves/filter";
			public const string GetZonesByFloorId = Base + "/management/location/zones";
			public const string GetSectionsByZoneId = Base + "/management/location/sections";
			public const string GetShelvesBySectionId = Base + "/management/location/shelves";
			#endregion
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
			public const string GetById = Base + "/privacy/notifications/{id}";
			//	[POST]
			public const string GetNotificationNotByAdmin = Base + "/privacy/notifications";
			public const string GetNumberOfUnreadNotifications = Base + "/privacy/unread-noti"; //filter unread notification
			//	[PUT]
			public const string UpdateReadStatus = Base + "/privacy/notifications";
			//	[PATCH]
			//	[DELETE]
		}

		/// <summary>
		/// BookCategory endpoints
		/// </summary>
		public static class Category
		{
			// [CREATE]
			public const string Create = Base + "/management/categories";
			//	[PUT] | [PATCH]
			public const string Update = Base + "/management/categories/{id}";
			// [DELETE]
			public const string HardDelete = Base + "/management/categories/{id}";
			public const string HardDeleteRange = Base + "/management/categories";
			// public const string Delete = Base + "/management/categories/{id}";
			// [GET]
			public const string GetAll = Base + "/management/categories";
			public const string GetById = Base + "/management/categories/{id}";
			public const string Import = Base +"management/categories/import";
		}

		/// <summary>
		/// FinePolicy endpoints
		/// </summary>
		public static class FinePolicy
		{
			// [CREATE]
			public const string Create = Base + "/management/fines/policy";
			//	[PUT] | [PATCH]
			public const string Update = Base + "/management/fines/policy/{id}";
			// [DELETE]
			public const string HardDelete = Base + "/management/fines/policy/{id}";
			public const string HardDeleteRange = Base + "/management/fines/policy";
			// public const string Delete = Base + "/management/fines/policy/{id}";
			// [GET]
			public const string GetAll = Base + "/fines/policy";
			public const string GetById = Base + "/fines/policy/{id}";
			public const string Import = Base + "/management/fines/policy/import";
		}

		/// <summary>
		/// AI endpoints
		/// </summary>
		public static class AIServices
		{
			// [GET]
			public const string GetAvailableLanguages = Base + "/books/available-languages";	
			//[POST]
			public const string CheckBookEdition = Base + "/management/books/ai/check-book-edition";
			public const string TrainingAfterCreate = Base + "/management/books/ai/train-after-create";
			public const string Predict = Base + "/books/ai/predict";
			public const string Recommendation = Base + "/books/ai/recommendation";
			public const string VoiceSearching = Base + "/books/voice";
			//[PUT] | [PATCH]
			//[DELETE]
		}
		
	}
}
