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
			#region LibraryManagement
			public const string GetAllByEntityIdAndName = Base + "/management/library-items/audits";
			public const string GetDetailByDateUtc = Base + "/management/library-items/audits/detail";
			#endregion

			#region RoleManagement

			// 

			#endregion
		}

		/// <summary>
		/// Borrow request endpoints
		/// </summary>
		public static class BorrowRequest
		{
			#region Management
			// [GET]
			public const string GetAllManagement = Base + "/management/borrows/requests";
			public const string GetByIdManagement = Base + "/management/borrows/requests/{id}";
			public const string CheckExistBarcode = Base + "/management/borrows/requests/{id}/exist-barcode";
			#endregion
			
			// [GET]
			public const string GetAll = Base + "/borrows/requests";
			public const string GetById = Base + "/borrows/requests/{id}";
			// [POST]
			public const string Create = Base + "/borrows/requests";
			// [PATCH]
			public const string Cancel = Base + "/borrows/requests/{id}/cancel";
		}

		/// <summary>
		/// Borrow
		/// </summary>
		public static class BorrowRecord
		{
			#region Management
			// [GET]
			public const string GetAll = Base + "/management/borrows/records";
			public const string GetById = Base + "/management/borrows/records/{id}";
			// [POST]
			public const string ProcessRequest = Base + "/management/borrows/records/process-request";
			public const string Create = Base + "/management/borrows/records";
			#endregion
			
			public const string SelfCheckout = Base + "/management/borrows/records/self-checkout";
		}
		
		/// <summary>
		/// Library item endpoints
		/// </summary>
		public static class LibraryItem
		{
			#region management
			// [GET]
			public const string GetEnums = Base + "/management/library-items/enums";
			public const string GetAll = Base + "/management/library-items";
			public const string GetById = Base + "/management/library-items/{id}";
			public const string CountTotalInstance = Base + "/management/library-items/{id}/total-copy";
			public const string CountRangeTotalInstance = Base + "/management/library-items/total-copy";
			public const string Export = Base + "/management/library-items/export";
			// [POST]
			public const string Create = Base + "/management/library-items";
			public const string AddAuthor = Base + "/management/library-items/add-author";
			public const string AddRangeAuthor = Base + "/management/library-items/add-range-author";
			public const string DeleteAuthor = Base + "/management/library-items/delete-author";
			// public const string Training = Base + "/management/books/editions/{id}/ai/train";
			public const string CheckImagesForTraining = Base + "/management/library-items/ai/check-images-for-training";
			public const string DeleteRangeAuthor = Base + "/management/library-items/delete-range-author";
			public const string Import = Base + "/management/library-items/import";
			// [PUT] / [PATCH]
			public const string Update = Base + "/management/library-items/{id}";
			public const string UpdateStatus = Base + "/management/library-items/{id}/status";
			public const string UpdateShelfLocation = Base + "/management/library-items/{id}/shelf-location";
			public const string SoftDelete = Base + "/management/library-items/{id}/soft-delete";
			public const string SoftDeleteRange = Base + "/management/library-items/soft-delete-range";
			public const string UndoDelete = Base + "/management/library-items/{id}/undo-delete";
			public const string UndoDeleteRange = Base + "/management/library-items/undo-delete-range";
			// [DELETE]
			public const string Delete = Base + "/management/library-items/{id}";
			public const string DeleteRange = Base + "/management/library-items";
			#endregion

			public const string GetNewArrivals = Base + "/library-items/new-arrivals";
			public const string GetRecentReadByIds = Base + "/library-items/recent-read";
			public const string GetTrending = Base + "/library-items/trending";
			public const string GetByCategory = Base + "/library-items/category/{categoryId}";
			public const string GetDetail = Base + "/library-items/{id}";
			public const string GetByBarcode = Base + "/library-items/get-by-barcode";
			public const string GetByIsbn = Base + "/library-items/get-by-isbn";
			public const string GetDetailEditions = Base + "/library-items/{id}/editions";
			public const string GetDetailReviews = Base + "/library-items/{id}/reviews";
			public const string GetRelatedItems = Base + "/library-items/{id}/related-items";
			public const string GetRelatedAuthorItems = Base + "/library-items/author-related-items";
			public const string Search = Base + "/library-items/q";
		}

		/// <summary>
		/// Library resource endpoints
		/// </summary>
		public static class LibraryItemResource
		{
			#region management
			// [GET]
			public const string GetAll = Base + "/management/library-items/resources";
			public const string GetById = Base + "/management/library-items/resources/{id}";
			// [POST]
			public const string AddToBook = Base + "/management/library-items/{libraryItemId}/resources";
			// [PUT] / [PATCH]
			public const string Update = Base + "/management/library-items/resources/{id}";
			public const string SoftDelete = Base + "/management/library-items/resources/{id}/soft-delete";
			public const string SoftDeleteRange = Base + "/management/library-items/resources/soft-delete-range";
			public const string UndoDelete = Base + "/management/library-items/resources/{id}/undo-delete";
			public const string UndoDeleteRange = Base + "/management/library-items/resources/undo-delete-range";
			// [DELETE]
			public const string Delete = Base + "/management/library-items/resources/{id}";
			public const string DeleteRange = Base + "/management/library-items/resources";
			#endregion
		}

		/// <summary>
		/// Library item instance endpoints
		/// </summary>
		public static class LibraryItemInstance
		{
			#region Management
			// [GET]
			public const string GetById = Base + "/management/library-items/instances/{id}";
			public const string GetByBarcode = Base + "/management/library-items/instances/code";
			// [POST]
			public const string AddRange = Base + "/management/library-items/{id}/instances";
			// [PUT] / [PATCH]
			public const string Update = Base + "/management/library-items/instances/{id}";
			public const string UpdateRange = Base + "/management/library-items/{libraryItemId}/instances";
			public const string SoftDelete = Base + "/management/library-items/instances/{id}/soft-delete";
			public const string SoftDeleteRange = Base + "/management/library-items/{libraryItemId}/instances/soft-delete-range";
			public const string UndoDelete = Base + "/management/library-items/instances/{id}/undo-delete";
			public const string UndoDeleteRange = Base + "/management/library-items/{libraryItemId}/instances/undo-delete-range";
			// [DELETE]
			public const string Delete = Base + "/management/library-items/instances/{id}";
			public const string DeleteRange = Base + "/management/library-items/{libraryItemId}/instances";
			#endregion
		}

		/// <summary>
		/// Library card endpoints
		/// </summary>
		public static class LibraryCard
		{
			#region Library Card Holder Management
			// [POST]
			public const string Create = Base + "/management/library-card-holders";
			// [GET]
			public const string GetAllCardHolders = Base + "/management/library-card-holders";
			public const string GetCardHolderById = Base + "/management/library-card-holders/{userId}";
			public const string GetAllCardHolderBorrowRequest = Base + "/management/library-card-holders/{userId}/borrows/requests";
			public const string GetAllCardHolderBorrowRecord = Base + "/management/library-card-holders/{userId}/borrows/records";
			public const string GetAllCardHolderDigitalBorrow = Base + "/management/library-card-holders/{userId}/borrows/digital";
			public const string GetAllCardHolderReservation = Base + "/management/library-card-holders/{userId}/reservations";
			public const string GetAllCardHolderInvoice = Base + "/management/library-card-holders/{userId}/invoices";
			public const string GetAllCardHolderTransaction = Base + "/management/library-card-holders/{userId}/transactions";
			public const string GetAllCardHolderNotification = Base + "/management/library-card-holders/{userId}/notifications";
			// [PUT]
			public const string UpdateHolder = Base + "/management/library-card-holders/{userId}";
			#endregion

			#region Library Card Management
			// [GET]
			public const string GetAllCard = Base + "/management/library-cards";
			public const string GetCardById = Base + "/management/library-cards/{id}";
			// [POST]
			public const string AddCard = Base + "/management/library-cards";
			// [PUT]
			public const string UpdateCard = Base + "/management/library-cards/{id}";
			// [PATCH]
			public const string SuspendCard = Base + "/management/library-cards/{id}/suspend";
			public const string UnsuspendCard = Base + "/management/library-cards/{id}/un-suspend";
			public const string ArchiveCard = Base + "/management/library-cards/{userId}/archive-card"; 
			// [DELETE]
			public const string DeleteCard = Base + "/management/library-cards/{id}";
			#endregion
			
			// [GET]
			public const string CheckCardExtension = Base + "/library-cards/{id}/check-extension";
			public const string GetByBarcode = Base + "/library-card-holders/get-by-barcode";
			public const string GetCardHolderDetailByEmail = Base + "/library-card-holders/detail";
			public const string GetAllCardHolderBorrowRequestByEmail = Base + "/library-card-holders/borrows/requests";
			public const string GetAllCardHolderBorrowRecordByEmail = Base + "/library-card-holders/borrows/records";
			public const string GetAllCardHolderDigitalBorrowByEmail = Base + "/library-card-holders/borrows/digital";
			public const string GetAllCardHolderReservationByEmail = Base + "/library-card-holders/reservations";
			public const string GetAllCardHolderInvoiceByEmail = Base + "/library-card-holders/invoices";
			public const string GetAllCardHolderTransactionByEmail = Base + "/library-card-holders/transactions";
			public const string GetAllCardHolderNotificationByEmail = Base + "/library-card-holders/notifications";
			// [POST]
			public const string Register = Base + "/library-cards/register";
			public const string ConfirmRegister = Base + "/library-cards/confirm-register";
			public const string ConfirmExtend = Base + "/library-cards/confirm-extend";
		}
		
		/// <summary>
		/// Library card package endpoints
		/// </summary>
		public static class LibraryCardPackage
		{
			#region Management
			// [GET]
			public const string GetAll = Base + "/management/packages";
			public const string GetById = Base + "/management/packages/{id}";
			// [POST]
			public const string Create = Base + "/management/packages";
			// [PUT]
			public const string Update = Base + "/management/packages/{id}";
			// [DELETE]
			public const string Delete = Base + "/management/packages/{id}";
			#endregion
		}
		
		/// <summary>
		/// Library item condition endpoints
		/// </summary>
		public static class LibraryItemCondition
		{
			#region Management
			// [GET]
			public const string GetAll = Base + "/management/conditions";
			
			#endregion
		}
		
		/// <summary>
		/// Group endpoints
		/// </summary>
		public static class Group
		{
			// [POST]
			public const string CheckAvailableGroup = Base + "/management/groups/check";
			public const string CheckItemToTrain = Base + "/management/groups/check-item-to-train"; 
			public const string DefineGroup = Base + "/management/groups/define-group"; 
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
		/// Role endpoints
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
		/// Library shelf endpoints
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
		/// Notification endpoints
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
		/// Category endpoints
		/// </summary>
		public static class Category
		{
			#region Management
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
            public const string Import = Base + "/management/categories/import";
			#endregion

			public const string GetAllPublic = Base + "/categories";
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
			public const string GetAvailableLanguages = Base + "/library-items/available-languages";	
			//[POST]
			public const string CheckBookEdition = Base + "/management/library-items/ai/check-book-edition";
			public const string TrainingAfterCreate = Base + "/management/library-items/ai/train";
			public const string Predict = Base + "/library-items/ai/predict";
			public const string Recommendation = Base + "/library-items/ai/recommendation";
			public const string RecommendationWithId = Base + "/library-items/ai/recommendation/{id}";
			public const string VoiceSearching = Base + "/library-items/voice";
			public const string RawDetect = Base + "/library-items/{id}/ai/raw-detect";
			public const string OCR = Base + "/ocr";
			public const string OCRDetail = Base + "/library-items/{id}/ocr-detail";
			//[PUT] | [PATCH]
			//[DELETE]
		}

		/// <summary>
		/// Warehouse tracking endpoints
		/// </summary>
		public static class WarehouseTracking
		{
			#region management
			// [GET]
            public const string GetAll = Base + "/management/warehouse-trackings";
            public const string GetById = Base + "/management/warehouse-trackings/{id}";
            // [POST]
            public const string Create = Base + "/management/warehouse-trackings";
            // [PUT]
            public const string Update = Base + "/management/warehouse-trackings/{id}";
            // [DELETE]
            public const string Delete = Base + "/management/warehouse-trackings/{id}";
			#endregion

			#region HeadLibrian only
			// [PATCH]
			public const string UpdateStatus = Base + "/management/warehouse-trackings/{id}/status";
			#endregion
		}

		/// <summary>
		/// Warehouse tracking detail endpoints
		/// </summary>
		public static class WarehouseTrackingDetail
		{
			// [GET]
			public const string GetById = Base + "/management/warehouse-trackings/details/{id}";
			public const string GetAllByTrackingId = Base + "/management/warehouse-trackings/{trackingId}/details";
			public const string GetAllNotExistItemByTrackingId = Base + "/management/warehouse-trackings/{trackingId}/details/no-item";
			// [POST]
			public const string Import = Base + "/management/warehouse-trackings/{trackingId}/details/import";
			public const string AddToTracking = Base + "/management/warehouse-trackings/{trackingId}/details";
			// [PUT]
			public const string Update = Base + "/management/warehouse-trackings/details/{id}";
			public const string UpdateItem = Base + "/management/warehouse-trackings/details/{id}/item";
			// [DELETE]
			public const string DeleteItem = Base + "/management/warehouse-trackings/details/{id}/item";
			public const string Delete = Base + "/management/warehouse-trackings/details/{id}";
		}
		
		/// <summary>
		/// Supplier endpoints
		/// </summary>
		public static class Supplier
		{
			// [GET]
			public const string GetAll = Base + "/management/suppliers";
			public const string GetById = Base + "/management/suppliers/{id}";
			public const string Export = Base + "/management/suppliers/export";
			// [POST]
			public const string Create = Base + "/management/suppliers";
			public const string Import = Base + "/management/suppliers/import";
			// [PUT] & [PATCH]
			public const string Update = Base + "/management/suppliers/{id}";
			// [DELETE]
			public const string Delete = Base + "/management/suppliers/{id}";
		}

		public static class Payment
		{
			//[Get]
			public const string GetPayOsPaymentLinkInformation = Base + "/payment/create-payment/{paymentLinkId}";
			//[Post]
			public const string CreatePayment = Base + "/payment/create-payment";
			public const string CancelPayment = Base + "/payment/cancel/{paymentLinkId}";
		}

		public static class Fine
		{
			public const string Create = Base + "/fine/create";
		}
	}
}
