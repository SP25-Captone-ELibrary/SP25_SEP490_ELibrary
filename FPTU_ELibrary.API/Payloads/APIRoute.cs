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
			// [PATCH]
			public const string CancelManagement = Base + "/management/borrows/requests/{id}/cancel";
			public const string CancelSpecificItemManagement = Base + "/management/borrows/requests/{id}/details/{libraryItemId}/cancel";
			#endregion
			
			// [GET]
			public const string ConfirmCreateTransaction = Base + "/borrows/requests/{id}/confirm-transaction";
			// [POST]
			public const string Create = Base + "/borrows/requests";
			public const string AddItemToRequest = Base + "/borrows/requests/{id}/details/add-item";
			// [PATCH]
			public const string Cancel = Base + "/borrows/requests/{id}/cancel";
			public const string CancelSpecificItem = Base + "/borrows/requests/{id}/details/{libraryItemId}/cancel";
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
			public const string GetAllUserPendingActivity = Base + "/management/borrows/records/user-pending-activity";
			// [POST]
			public const string ProcessRequest = Base + "/management/borrows/records/process-request";
			public const string Create = Base + "/management/borrows/records";
			// [PUT]
			public const string ProcessReturn = Base + "/mangement/borrows/records/process-return";
			#endregion
			
			public const string SelfCheckout = Base + "/borrows/records/self-checkout";
			public const string Extend = Base + "/borrows/records/{id}/extend";
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
			public const string GetShelf = Base + "/management/library-items/{id}/get-shelf";
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

			public const string GetPartOfAudioResource = Base + "/library-items/{itemId}/resource/{resourceId}/{part}";
			public const string CountPartToUpload = Base +"/library-items/{itemId}/resource/{resourceId}/count-part";
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
			public const string CheckUnavailableItems = Base + "/library-items/unavailable";
			public const string GetOwnResource = Base + "/library-items/{itemId}/resource/{resourceId}";
			public const string GetPdfPreview = Base + "/library-items/resource/{resourceId}/preview";
			public const string CheckEmgu = "emgu/test";
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
			public const string AddAudioBook = Base + "/management/library-items/{libraryItemId}/resources/audio";			// [PUT] / [PATCH]
			public const string Update = Base + "/management/library-items/resources/{id}";
			public const string SoftDelete = Base + "/management/library-items/resources/{id}/soft-delete";
			public const string SoftDeleteRange = Base + "/management/library-items/resources/soft-delete-range";
			public const string UndoDelete = Base + "/management/library-items/resources/{id}/undo-delete";
			public const string UndoDeleteRange = Base + "/management/library-items/resources/undo-delete-range";
			// [DELETE]
			public const string Delete = Base + "/management/library-items/resources/{id}";
			public const string DeleteRange = Base + "/management/library-items/resources";
			#endregion
			
			// [GET]
			public const string GetByIdPublic = Base + "/library-items/resources/{id}";
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
			public const string GetByBarcodeToConfirmUpdateShelf = Base + "/management/library-items/instances/{barcode}/shelf-update-confirmation";
			public const string CheckExistBarcode = Base + "/management/library-items/instances/check-exist-barcode";
			public const string GenerateBarcodeRange = Base + "/management/library-items/instances/generate-barcode-range";
			// [POST]
			public const string AddRange = Base + "/management/library-items/{id}/instances";
			// [PUT] / [PATCH]
			public const string Update = Base + "/management/library-items/instances/{id}";
			public const string UpdateRange = Base + "/management/library-items/{libraryItemId}/instances";
			public const string UpdateRangeInShelf = Base + "/management/library-items/instances/update-in-shelf";
			public const string UpdateRangeOutOfShelf = Base + "/management/library-items/instances/update-out-of-shelf";
			public const string UpdateInShelf = Base + "/management/library-items/instances/{barcode}/update-in-shelf"; 
			public const string UpdateOutOfShelf = Base + "/management/library-items/instances/{barcode}/update-out-of-shelf"; 
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

			#region Library Card Management
			// [GET]
			public const string GetAllCard = Base + "/management/library-cards";
			public const string GetCardById = Base + "/management/library-cards/{id}";
			// [PUT]
			public const string UpdateCard = Base + "/management/library-cards/{id}";
			// [PATCH]
			public const string Confirm = Base + "/management/library-cards/{id}/confirm";
			public const string Reject = Base + "/management/library-cards/{id}/reject";
			public const string ExtendBorrowAmount = Base + "/management/library-cards/{id}/extend-borrow-amount";
			public const string ExtendCard = Base + "/management/library-cards/{id}/extend";
			public const string SuspendCard = Base + "/management/library-cards/{id}/suspend";
			public const string UnsuspendCard = Base + "/management/library-cards/{id}/un-suspend";
			public const string ArchiveCard = Base + "/management/library-cards/{userId}/archive-card"; 
			// [DELETE]
			public const string DeleteCard = Base + "/management/library-cards/{id}";
			#endregion
			
			// [GET]
			public const string CheckCardExtension = Base + "/library-cards/{id}/check-extension";
			// [POST]
			public const string Register = Base + "/library-cards/register";
			// [PATCH]
			public const string SendReConfirm = Base + "/library-cards/re-confirm";
		}
		
		/// <summary>
		/// Library cardholder endpoints
		/// </summary>
		public static class LibraryCardHolder
		{
			#region Management
			// [POST]
			public const string Create = Base + "/management/library-card-holders";
			public const string AddCard = Base + "/management/library-card-holders/add-card";
			public const string Import = Base + "/management/library-card-holders/import";
			// [GET]
			public const string Export = Base + "/management/library-card-holders/export";
			public const string GetCardHolderById = Base + "/management/library-card-holders/{userId}";
			public const string GetCardHolderBorrowRequestById = Base + "/management/library-card-holders/{userId}/borrows/requests/{requestId}";
			public const string GetCardHolderBorrowRecordById = Base + "/management/library-card-holders/{userId}/borrows/records/{borrowRecordId}";
			public const string GetCardHolderDigitalBorrowById = Base + "/management/library-card-holders/{userId}/borrows/digital/{digitalBorrowId}";
			public const string GetCardHolderTransactionById = Base + "/management/library-card-holders/{userId}/borrows/transactions/{transactionId}";
			public const string GetAllCardHolders = Base + "/management/library-card-holders";
			public const string GetAllCardHolderBorrowRequest = Base + "/management/library-card-holders/{userId}/borrows/requests";
			public const string GetAllCardHolderBorrowRecord = Base + "/management/library-card-holders/{userId}/borrows/records";
			public const string GetAllCardHolderDigitalBorrow = Base + "/management/library-card-holders/{userId}/borrows/digital";
			public const string GetAllCardHolderReservation = Base + "/management/library-card-holders/{userId}/reservations";
			public const string GetAllCardHolderTransaction = Base + "/management/library-card-holders/{userId}/transactions";
			public const string GetAllCardHolderNotification = Base + "/management/library-card-holders/{userId}/notifications";
			// [PUT] OR [PATCH]
			public const string UpdateCardHolder = Base + "/management/library-card-holders/{userId}";
			public const string SoftDeleteCardHolder = Base + "/management/library-card-holders/{userId}/soft-delete";
			public const string SoftDeleteRangeCardHolder = Base + "/management/library-card-holders/soft-delete-range";
			public const string UndoDeleteCardHolder = Base + "/management/library-card-holders/{userId}/undo-delete";
			public const string UndoDeleteRangeCardHolder = Base + "/management/library-card-holders/undo-delete-range";
			// [DELETE]
			public const string DeleteCardHolder = Base + "/management/library-card-holders/{userId}";
			public const string DeleteRangeCardHolder = Base + "/management/library-card-holders";
			

			#endregion
			
			// [GET]
			public const string GetByBarcode = Base + "/library-card-holders/get-by-barcode";
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
			
			// [GET]
			public const string GetAllPublic = Base + "/packages";
			public const string GetByIdPublic = Base + "/packages/{id}";
		}
		
		/// <summary>
		/// Library item condition endpoints
		/// </summary>
		public static class LibraryItemCondition
		{
			#region Management
			// [GET]
			public const string GetAll = Base + "/management/conditions";
			public const string GetAllForStockInWarehouse = Base + "/management/conditions/stock-in-warehouse";
			public const string GetById = Base + "/management/conditions/{id}";
			// [POST]
			public const string Create = Base + "/management/conditions";
			// [PUT]
			public const string Update = Base + "/management/conditions/{id}";
			// [DELETE]
			public const string Delete = Base + "/management/conditions/{id}";
			#endregion
		}
		
		/// <summary>
		/// Group endpoints
		/// </summary>
		public static class Group
		{
			//[GET]
			public const string GetSuitableItemsForGrouping = Base + "/management/groups/suitable-items/{rootItemId}";
			public const string GroupedItems = Base + "/management/groups/grouped-items";

			public const string AvailableTrainingGroupPerTime = "/management/groups/available-groups-to-train";
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
			public const string CalculateBorrowReturnSummary = Base + "/users/borrows/calculate-summary";
			public const string GetAllPendingActivity = Base + "/users/borrows/records/user-pending-activity";
			public const string GetAllUserBorrowRequest = Base + "/users/borrows/requests";
			public const string GetAllUserBorrowRecord = Base + "/users/borrows/records";
			public const string GetAllUserDigitalBorrow = Base + "/users/borrows/digital";
			public const string GetAllUserReservation = Base + "/users/reservations";
			public const string GetAllUserTransaction = Base + "/users/transactions";
			public const string GetAllUserNotification = Base + "/users/notifications";
			public const string GetBorrowRequestById = Base + "/users/borrows/requests/{id}";
			public const string GetBorrowRecordById = Base + "/users/borrows/records/{id}";
			public const string GetDigitalBorrowById = Base + "/users/borrows/digital/{id}";
			public const string GetTransactionById = Base + "/users/transactions/{id}";
			// [POST]
			// [PATCH]
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
			#region Management
			// [GET]
			public const string GetAllType = Base + "/management/resources/types";
			// [POST]
			public const string UploadImage = Base + "/management/resources/images/upload";
			public const string UploadVideo = Base + "/management/resources/videos/upload";
			public const string UploadLargeVideo = Base + "/management/resources/large-videos/upload";
			// [PUT]
			public const string UpdateImage = Base + "/management/resources/images/update";
			public const string UpdateVideo = Base + "/management/resources/videos/update";
			// [DELETE]
			public const string DeleteImage = Base + "/management/resources/images";
			public const string DeleteVideo = Base + "/management/resources/videos";
			#endregion
			
			public const string PublicUploadImage = Base + "/resources/images/upload";
		}

		/// <summary>
		/// Return endpoints
		/// </summary>
		public static class Return
		{
			#region Management
			public const string InLibraryReturn = Base + "/management/returns/in-library";
			public const string SelfCheckoutReturn = Base + "/managmenet/returns/self-checkout";
			#endregion
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
			public const string GetMapByFloorId = Base + "/management/location/map/floors/{floorId}";
			public const string GetMapShelfDetailById = Base + "/management/location/map/shelves/{shelfId}";

			public const string GetShelvesForFilter = Base + "/management/location/shelves/filter";
			public const string GetZonesByFloorId = Base + "/management/location/zones";
			public const string GetSectionsByZoneId = Base + "/management/location/sections";
			public const string GetShelvesBySectionId = Base + "/management/location/shelves";
			#endregion
			
			public const string GetShelfWithFloorZoneSectionById = Base + "/location/shelves/{shelfId}";
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
			public const string GetAll = Base + "/management/notifications";
			public const string GetById = Base + "/management/notifications/{id}";
			//	[POST]
			public const string Create = Base + "/management/notifications";
			//	[PUT]
			//	[PATCH]
			//	[DELETE]
			#endregion
			
			//	[GET]
			public const string GetPrivacyById = Base + "/privacy/notifications/{id}";
			//	[POST]
			public const string GetAllPrivacy = Base + "/privacy/notifications";
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
			public const string TextToVoice = Base + "/library-items/text-to-voice";
			public const string GetStatusToTrain = Base+ "/library-items/train-status";
			//[POST]
			public const string CheckBookEdition = Base + "/management/library-items/ai/check-book-edition";
			public const string TrainingAfterCreate = Base + "/management/library-items/ai/train";
			public const string Training = Base + "/management/library-items/ai/extend-train";
			public const string TrainingLatestVersion = Base+"/management/library-items/ai/extend-train/v2";
			public const string Predict = Base + "/library-items/ai/predict";
			public const string PredictWithEmgu = Base + "/library-items/ai/predict/v2";
			public const string Recommendation = Base + "/library-items/ai/recommendation";
			public const string RecommendationWithId = Base + "/library-items/ai/recommendation/{id}";
			public const string VoiceSearching = Base + "/library-items/voice";
			public const string RawDetect = Base + "/library-items/{id}/ai/raw-detect";
			public const string OCR = Base + "/ocr";
			public const string OCRDetail = Base + "/library-items/{id}/ocr-detail";
			//[PUT] | [PATCH]
			//[DELETE]
		}

		public static class FaceDetection
		{
			public const string Detect = Base + "/face-detection/detect";
		}
		
		/// <summary>
		/// Warehouse tracking endpoints
		/// </summary>
		public static class WarehouseTracking
		{
			#region management
			// [GET]
            public const string GetAll = Base + "/management/warehouse-trackings";
            public const string GetAllStockTransactionTypeByTrackingType = Base + "/management/warehouse-trackings/stock-transasction-types";
            public const string GetById = Base + "/management/warehouse-trackings/{id}";
            // [POST]
            public const string Create = Base + "/management/warehouse-trackings";
            public const string StockIn = Base + "/management/warehouse-trackings/stock-in";
            // [PUT]
            public const string Update = Base + "/management/warehouse-trackings/{id}";
            public const string UpdateRangeUniqueBarcodeRegistration = Base + "/management/warehouse-trackings/{id}/unique-barcode-registration";
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
			public const string GetRangeBarcodeById = Base + "/management/warehouse-trackings/details/{id}/range-barcode";
			// [POST]
			public const string Import = Base + "/management/warehouse-trackings/{trackingId}/details/import";
			public const string AddToTracking = Base + "/management/warehouse-trackings/{trackingId}/details";
			// [PUT]
			public const string Update = Base + "/management/warehouse-trackings/details/{id}";
			public const string UpdateItem = Base + "/management/warehouse-trackings/details/{id}/item";
			public const string UpdateBarcodeRegistration = Base + "/management/warehouse-trackings/details/{id}/unique-barcode-registration";
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

		/// <summary>
		/// Payment endpoints
		/// </summary>
		public static class Payment
		{
			#region Management
			public const string GetAllTransaction = Base + "/management/payment/transactions";
			#endregion
			
			// [GET]
			public const string GetPayOsPaymentLinkInformation = Base + "/payment/{paymentLinkId}";
			public const string GetPrivacyTransaction = Base + "/payment/transactions";
			// [POST]
			public const string CreateTransaction = Base + "/payment/transactions";
			public const string CreateBorrowRecordTransaction = Base + "/payment/transactions/borrows/records/{borrowRecordId}";
			public const string CreateBorrowRequestTransaction = Base + "/payment/transactions/borrows/requests/{borrowRequestId}";
			public const string CancelPayment = Base + "/payment/cancel/{paymentLinkId}";
			public const string VerifyPayment = Base + "/payment/verify";
			public const string SendWebhookConfirm = Base + "/payment/pay-os/webhook-confirm";
			public const string WebhookPayOsReturn = Base + "/payment/pay-os/return";
			public const string WebhookPayOsCancel = Base + "/payment/pay-os/cancel";
		}

		/// <summary>
		/// Payment endpoints
		/// </summary>
		public static class PaymentMethod
		{
			// [GET]
			public const string GetAll = Base + "/payment-methods";
		}
		
		/// <summary>
		/// User favourite endpoints
		/// </summary>
		public static class UserFavorite
		{
			// [POST]
			public const string AddFavorite = Base + "/user-favorite/add/{id}";
			// [DELETE]
			public const string RemoveFavorite = Base + "/user-favorite/remove/{id}";
			// [GET]
			public const string GetAll = Base + "/user-favorite";
		}
		/// <summary>
		/// AdminConfiguration endpoints
		/// </summary>
		public static class AdminConfiguration
		{
			// [GET]
			public const string GetAll = Base + "/admin-configuration";

			public const string GetDetail = Base + "/admin-configuration/{name}";
			// [PUT]
			public const string Update = Base + "/admin-configuration";
			
		}
		
	}
}
