using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPTU_ELibrary.Application.Common
{
    public class ResultCodeConst
    {
        #region ResultCode

        #region System
        /// <summary>
        /// [SUCCESS] Create success
        /// </summary> 
        public static string SYS_Success0001 = "SYS.Success0001";
        /// <summary>
        /// [SUCCESS] Read success
        /// </summary> 
        public static string SYS_Success0002 = "SYS.Success0002";
        /// <summary>
        /// [SUCCESS] Update success
        /// </summary> 
        public static string SYS_Success0003 = "SYS.Success0003";
        /// <summary>
        /// [SUCCESS] Delete success
        /// </summary> 
        public static string SYS_Success0004 = "SYS.Success0004";
        /// <summary>
        /// [SUCCESS] Import (number) data successfully
        /// </summary> 
        public static string SYS_Success0005 = "SYS.Success0005";
        /// <summary>
        /// [SUCCESS] Create new {0} successfully
        /// </summary> 
        public static string SYS_Success0006 = "SYS.Success0006";
        /// <summary>
        /// [SUCCESS] Deleted data to trash
        /// </summary> 
        public static string SYS_Success0007 = "SYS.Success0007";
        /// <summary>
        /// [SUCCESS] Deleted {0} data successfully
        /// </summary> 
        public static string SYS_Success0008 = "SYS.Success0008";
        /// <summary>
        /// [SUCCESS] Recovery data successfully
        /// </summary> 
        public static string SYS_Success0009 = "SYS.Success0009";
        
        /// <summary>
        /// [FAIL] Create fail
        /// </summary> 
        public static string SYS_Fail0001 = "SYS.Fail0001";
        /// <summary>
        /// [FAIL] Read fail
        /// </summary>
        public static string SYS_Fail0002 = "SYS.Fail0002";
        /// <summary>
        /// [FAIL] Update fail
        /// </summary>
        public static string SYS_Fail0003 = "SYS.Fail0003";
        /// <summary>
        /// [FAIL] Delete fail
        /// </summary>
        public static string SYS_Fail0004 = "SYS.Fail0004";
        /// <summary>
        /// [FAIL] Fail to get data: {0}
        /// </summary>
        public static string SYS_Fail0005 = "SYS.Fail0005";
        /// <summary>
        /// [FAIL] Fail to create new {0}
        /// </summary>
        public static string SYS_Fail0006 = "SYS.Fail0006";
        /// <summary>
        /// [FAIL] Cannot delete because it is bound to other data
        /// </summary>
        public static string SYS_Fail0007 = "SYS.Fail0007";
        /// <summary>
        /// [FAIL] Fail to import data
        /// </summary>
        public static string SYS_Fail0008 = "SYS.Fail0008";
        /// <summary>
        /// [FAIL] Recovery data failed
        /// </summary> 
        public static string SYS_Fail0009 = "SYS.Fail0009";
		
        /// <summary>
		/// [WARNING] Invalid inputs
		/// </summary>
		public static string SYS_Warning0001 = "SYS.Warning0001";
		/// <summary>
		/// [WARNING] Not found {0}
		/// </summary>
		public static string SYS_Warning0002 = "SYS.Warning0002";
		/// <summary>
		/// [WARNING] Cannot progress {0} as {1} already exist
		/// </summary>
		public static string SYS_Warning0003 = "SYS.Warning0003";
		/// <summary>
		/// [WARNING] Data not found or empty
		/// </summary>
		public static string SYS_Warning0004 = "SYS.Warning0004";
		/// <summary>
		/// [WARNING] No data effected
		/// </summary>
		public static string SYS_Warning0005 = "SYS.Warning0005";
		/// <summary>
		/// [WARNING] Unknown error
		/// </summary>
		public static string SYS_Warning0006 = "SYS.Warning0006";
		/// <summary>
		/// [WARNING] Cannot delete {0} as it is in use
		/// </summary>
		public static string SYS_Warning0007 = "SYS.Warning0007";
		/// <summary>
		/// [WARNING] Cannot edit because it is bound to other data
		/// </summary>
		public static string SYS_Warning0008 = "SYS.Warning0008";
		#endregion

		#region Auth
		/// <summary>
		/// [SUCCESS] Create account success 
		/// </summary>
		public static string Auth_Success0001 = "Auth.Success0001";
		/// <summary>
		/// [SUCCESS] Sign-in success 
		/// </summary>
		public static string Auth_Success0002 = "Auth.Success0002";
		/// <summary>
		/// [SUCCESS] Sign-in with password 
		/// </summary>
		public static string Auth_Success0003 = "Auth.Success0003";
		/// <summary>
		/// [SUCCESS] Sign-in with OTP 
		/// </summary>
		public static string Auth_Success0004 = "Auth.Success0004";
		/// <summary>
		/// [SUCCESS] Send OPT to email success
		/// </summary>
		public static string Auth_Success0005 = "Auth.Success0005";
		/// <summary>
		/// [SUCCESS] Change password success 
		/// </summary>
		public static string Auth_Success0006 = "Auth.Success0006";
		/// <summary>
		/// [SUCCESS] Confirm account success
		/// </summary>
		public static string Auth_Success0007 = "Auth.Success0007";
		/// <summary>
		/// [SUCCESS] Create new Token successfully
		/// </summary>
		public static string Auth_Success0008 = "Auth.Success0008";
		/// <summary>
		/// [SUCCESS] Email verification success, please check email to reset password
		/// </summary>
		public static string Auth_Success0009 = "Auth.Success0009";
		/// <summary>
		/// [SUCCESS] Create MFA backup codes succesfully
		/// </summary>
		public static string Auth_Success0010 = "Auth.Success0010";
		
		/// <summary>
		/// [WARNING] Account not allow to access
		/// </summary>
		public static string Auth_Warning0001 = "Auth.Warning0001";
		/// <summary>
		/// [WARNING] Invalid token
		/// </summary>
		public static string Auth_Warning0002 = "Auth.Warning0002";
		/// <summary>
		/// [WARNING] Invalid token with detail message
		/// </summary>
		public static string Auth_Warning0003 = "Auth.Warning0003";
		/// <summary>
		/// [WARNING] Account already confirmed
		/// </summary>
		public static string Auth_Warning0004 = "Auth.Warning0004";
		/// <summary>
		/// [WARNING] OTP code is incorrect, please resend
		/// </summary>
		public static string Auth_Warning0005 = "Auth.Warning0005";
		/// <summary>
		/// [WARNING] Email already exist
		/// </summary>
		public static string Auth_Warning0006 = "Auth.Warning0006";
		/// <summary>
		/// [WARNING] Wrong username or password
		/// </summary>
		public static string Auth_Warning0007 = "Auth.Warning0007";
		/// <summary>
		/// [WARNING] Account email is not verify yet
		/// </summary>
		public static string Auth_Warning0008 = "Auth.Warning0008";
		/// <summary>
		/// [WARNING] The account has enabled 2-factor
		/// </summary>
		public static string Auth_Warning0009 = "Auth.Warning0009";
		/// <summary>
		/// [WARNING] Two-factor authentication is required
		/// </summary>
		public static string Auth_Warning0010 = "Auth.Warning0010";
		/// <summary>
		/// [WARNING] Cannot process as the account has not enabled 2-factor authentication yet
		/// </summary>
		public static string Auth_Warning0011 = "Auth.Warning0011";
		/// <summary>
		/// [WARNING] Backup code is not valid
		/// </summary>
		public static string Auth_Warning0012 = "Auth.Warning0012";
		
		/// <summary>
		/// [FAIL] Fail to update password
		/// </summary>
		public static string Auth_Fail0001 = "Auth.Fail0001";
		/// <summary>
		/// [FAIL] Fail to send OTP 
		/// </summary>
		public static string Auth_Fail0002 = "Auth.Fail0002";
		/// <summary>
		/// [FAIL] Fail to create backup codes
		/// </summary>
		public static string Auth_Fail0003 = "Auth.Fail0003";
		#endregion

		#region Borrow
		/// <summary>
		/// [SUCCESS] Total {0} item(s) have been borrowed successfully
		/// </summary>
		public const string Borrow_Success0001 = "Borrow.Success0001";
		/// <summary>
		/// [SUCCESS] Cancel borrowing {0} item(s) successfully
		/// </summary>
		public const string Borrow_Success0002 = "Borrow.Success0002";
		/// <summary>
		/// [WARNING] Required at least {0} item(s) to process
		/// </summary>
		public const string Borrow_Warning0001 = "Borrow.Warning0001";
		/// <summary>
		/// [WARNING] Item quantity is not available
		/// </summary>
		public const string Borrow_Warning0002 = "Borrow.Warning0002";
		/// <summary>
		/// [WARNING] Duplicate items are not allowed. You can only borrow one copy of each item per time
		/// </summary>
		public const string Borrow_Warning0003 = "Borrow.Warning0003";
		/// <summary>
		/// [WARNING] The item is currently borrowed and cannot be borrowed again
		/// </summary>
		public const string Borrow_Warning0004 = "Borrow.Warning0004";
		/// <summary>
		/// [WARNING] You can borrow up to {0} items at a time
		/// </summary>
		public const string Borrow_Warning0005 = "Borrow.Warning0005";
		/// <summary>
		/// [WARNING] Cannot cancel because item has been proceeded
		/// </summary>
		public const string Borrow_Warning0006 = "Borrow.Warning0006";
		/// <summary>
		/// [WARNING] Cannot process as borrow request has been expired
		/// </summary>
		public const string Borrow_Warning0007 = "Borrow.Warning0007";
		/// <summary>
		/// [WARNING] Cannot process as library card is incorrect
		/// </summary>
		public const string Borrow_Warning0008 = "Borrow.Warning0008";
		/// <summary>
		/// [FAIL] An error occurred, the item borrowing registration failed
		/// </summary>
		public const string Borrow_Fail0001 = "Borrow.Fail0001";
		#endregion
		
		#region Role
		/// <summary>
		/// [WARNING] Role name already exist, please check again
		/// </summary>
		public const string Role_Warning0001 = "Role.Warning0001";
		/// <summary>
		/// [WARNING] Cannot update as role is invalid
		/// </summary>
		public const string Role_Warning0002 = "Role.Warning0002";
		#endregion

		#region File
		/// <summary>
		/// [WARNING] The uploaded file is not supported
		/// </summary>
		public const string File_Warning0001 = "File.Warning0001";

		/// <summary>
		/// [WARNING] TFile not found
		/// </summary>
		public const string File_Warning0002 = "File.Warning0002";
		
		/// <summary>
		/// [WARNING] Invalid column separator selection
		/// </summary>
		public const string File_Warning0003 = "File.Warning0003";
		
		/// <summary>
		/// [WARNING] Tệp tải lên chứa các hình ảnh bị trùng tên: {0}
		/// </summary>
		public const string File_Warning0004 = "File.Warning0004";
		
		#endregion

		#region Cloud
		/// <summary>
		/// [SUCCESS] Upload image successfully
		/// </summary>
		public const string Cloud_Success0001 = "Cloud.Success0001";
		/// <summary>
		/// [SUCCESS] Upload video successfully
		/// </summary>
		public const string Cloud_Success0002 = "Cloud.Success0002";
		/// <summary>
		/// [SUCCESS] Delete image successfully
		/// </summary>
		public const string Cloud_Success0003 = "Cloud.Success0003";
		/// <summary>
		/// [SUCCESS] Delete video successfully
		/// </summary>
		public const string Cloud_Success0004 = "Cloud.Success0004";
		/// <summary>
		/// [WARNING] Not found image resource
		/// </summary>
		public const string Cloud_Warning0001 = "Cloud.Warning0001";
		/// <summary>
		/// [WARNING] Not found video resource
		/// </summary>
		public const string Cloud_Warning0002 = "Cloud.Warning0002";
		/// <summary>
		/// [WARNING] One or more resources not found
		/// </summary>
		public const string Cloud_Warning0003 = "Cloud.Warning0003";
		/// <summary>
        /// [FAIL] Fail to upload image
        /// </summary>
        public const string Cloud_Fail0001 = "Cloud.Fail0001";
        /// <summary>
        /// [FAIL] Fail to upload video
        /// </summary>
        public const string Cloud_Fail0002 = "Cloud.Fail0002";
        /// <summary>
        /// [FAIL] Fail to remove image
        /// </summary>
        public const string Cloud_Fail0003 = "Cloud.Fail0003";
         /// <summary>
        /// [FAIL] Fail to remove video
        /// </summary>
        public const string Cloud_Fail0004 = "Cloud.Fail0004";

		#endregion

		#region User
		/// <summary>
		/// [Warning] User code already exist
		/// </summary>
		public static string User_Warning0001 = "User.Warning0001";
		/// <summary>
		/// [Warning] Invalid role has been chosen  
		/// </summary>
		public static string User_Warning0002 = "User.Warning0002";

		#endregion

		#region Employee
		/// <summary>
		/// [Warning] Employee code already exist
		/// </summary>
		public const string Employee_Warning0001 = "Employee.Warning0001";
		#endregion

		#region Author
		/// <summary>
		/// Author code already exist
		/// </summary>
		public const string Author_Warning0001 = "Author.Warning0001";
		#endregion
		
		#region Notification

		/// <summary>
		/// There is no important message 
		/// </summary>
		public static string Notification_Warning0001 = "Notification.Warning0001";
		/// <summary>
		/// Access to noti that not belongs to this account 
		/// </summary>
		public static string Notification_Warning0002 = "Notification.Warning0002";

		#endregion

		#region Library Item
		/// <summary>
		/// [WARNING] Some authors are unavailable or do not exist
		/// </summary>
		public const string LibraryItem_Warning0001 = "LibraryItem.Warning0001";
		/// <summary>
		/// [WARNING] Item edition number must be unique within the same group
		/// </summary>
		public const string LibraryItem_Warning0002 = "LibraryItem.Warning0002";
		/// <summary>
		/// [WARNING] Library resources must not have duplicate content
		/// </summary>
		public const string LibraryItem_Warning0003 = "LibraryItem.Warning0003";
		/// <summary>
		/// [WARNING] Item edition instance barcode must be unique
		/// </summary>
		public const string LibraryItem_Warning0004 = "LibraryItem.Warning0004";
		/// <summary>
		/// [WARNING] Barcode {0} already exist
		/// </summary>
		public const string LibraryItem_Warning0005 = "LibraryItem.Warning0005";
		/// <summary>
		/// [WARNING] The prefix of barcode is invalid, the prefix pattern of the category is {0}
		/// </summary>
		public const string LibraryItem_Warning0006 = "LibraryItem.Warning0006";
		/// <summary>
		/// [WARNING] ISBN Code {0} already exist
		/// </summary>
		public const string LibraryItem_Warning0007 = "LibraryItem.Warning0007";
		/// <summary>
		/// [WARNING] Cannot change data that is on borrowing or reserved
		/// </summary>
		public const string LibraryItem_Warning0008 = "LibraryItem.Warning0008";
		/// <summary>
		/// [WARNING] Cannot process, please change all item instance status to inventory first
		/// </summary>
		public const string LibraryItem_Warning0009 = "LibraryItem.Warning0009";
		/// <summary>
		/// [WARNING] Cannot delete as there still exist instance within the item
		/// </summary>
		public const string LibraryItem_Warning0010 = "LibraryItem.Warning0010";
		/// <summary>
		/// [WARNING] Cannot change item instance status. {0}
		/// </summary>
		public const string LibraryItem_Warning0011 = "LibraryItem.Warning0011";
		/// <summary>
		/// [WARNING] Cannot change item status to public. {0}
		/// </summary>
		public const string LibraryItem_Warning0012 = "LibraryItem.Warning0012";
		/// <summary>
		/// [WARNING] Cannot change item status to draft. {0}
		/// </summary>
		public const string LibraryItem_Warning0013 = "LibraryItem.Warning0013";
		/// <summary>
		/// [WARNING] Required all item instance to have the same prefix of new category
		/// </summary>
		public const string LibraryItem_Warning0014 = "LibraryItem.Warning0014";

		/// <summary>
		/// [FAIL] An error occurred while updating the inventory data
		/// </summary>
		public const string LibraryItem_Fail0001 = "LibraryItem.Fail0001";
		/// <summary>
		/// [FAIL] The action cannot be performed. Please switch to draft status to proceed
		/// </summary>
		public const string LibraryItem_Fail0002 = "LibraryItem.Fail0002";
		#endregion

		#region Library Card
		/// <summary>
		/// [SUCCESS] Library card is valid
		/// </summary>
		public const string LibraryCard_Success0001 = "LibraryCard.Success0001";
		/// <summary>
		/// [WARNING] Your library card is not activated yet. Please make a payment to activate your card
		/// </summary>
		public const string LibraryCard_Warning0001 = "LibraryCard.Warning0001";
		/// <summary>
		/// [WARNING] Your library card has expired. Please renew it to continue using library services
		/// </summary>
		public const string LibraryCard_Warning0002 = "LibraryCard.Warning0002";
		/// <summary>
		/// [WARNING] Your library card has been suspended due to a violation or administrative action. Please contact the library for assistance (Activated at {0})
		/// </summary>
		public const string LibraryCard_Warning0003 = "LibraryCard.Warning0003";
		/// <summary>
		/// [WARNING] You need a library card to access this service
		/// </summary>
		public const string LibraryCard_Warning0004 = "LibraryCard.Warning0004";
		#endregion
		
		#region Supplier
		/// <summary>
		/// [WARNING] The action cannot be performed, as the supplier is in the warehouse tracking information has been approved
		/// </summary>
		public const string Supplier_Warning0001 = "Supplier.Warning0001";
		#endregion
		
        #region AIService
        /// <summary>
        /// Data matched with image
        /// </summary>
        public static string AIService_Success0001 = "AIService.Success0001";
        /// <summary>
        /// Train sucessfully
        /// </summary>
        public static string AIService_Success0002 = "AIService.Success0002";
        /// <summary>
        /// Start Training model successfully
        /// </summary>
        public static string AIService_Success0003 = "AIService.Success0003";
        /// <summary>
        /// Predict Successfully
        /// </summary>
        public static string AIService_Success0004 = "AIService.Success0004";
        /// <summary>
        /// Grouped items successfully
        /// </summary>
        public static string AIService_Success0005 = "AIService.Success0005";
        /// <summary>
        /// Data did not match with image
        /// </summary>
        public static string AIService_Warning0001 = "AIService.Warning0001";
        /// <summary>
        /// There is an iteration that is training
        /// </summary>
        public static string AIService_Warning0002 = "AIService.Warning0002";
        /// <summary>
        /// Can not detect any books in the picture
        /// </summary>
        public static string AIService_Warning0003 = "AIService.Warning0003";
        /// <summary>
        /// Recommendation is just for 1 book.
        /// </summary>
        public static string AIService_Warning0004 = "AIService.Warning0004";
        #endregion

        #region Warehouse Tracking
        /// <summary>
        /// [WARNING] Do not allow to change status from {0} to {1}
        /// </summary>
        public const string WarehouseTracking_Warning0001 = "WarehouseTracking.Warning0001";
        /// <summary>
        /// [WARNING] The action cannot be performed. Please switch warehouse tracking information to draft
        /// </summary>
        public const string WarehouseTracking_Warning0002 = "WarehouseTracking.Warning0002";
        /// <summary>
        /// [WARNING] Cannot delete warehouse tracking information, as existing item has been cataloged
        /// </summary>
        public const string WarehouseTracking_Warning0003 = "WarehouseTracking.Warning0003";
        /// <summary>
        /// [WARNING] Cannot change status to completed, as existing item has not been cataloged yet
        /// </summary>
        public const string WarehouseTracking_Warning0004 = "WarehouseTracking.Warning0004";
        /// <summary>
        /// [WARNING] Cannot change status to completed, as total item instance is not
        /// enough compared to the total of warehouse tracking information
        /// </summary>
        public const string WarehouseTracking_Warning0005 = "WarehouseTracking.Warning0005";
        /// <summary>
        /// [WARNING] This action cannot be performed, as warehouse tracking detail already exist cataloged item
        /// </summary>
        public const string WarehouseTracking_Warning0006 = "WarehouseTracking.Warning0006";
        /// <summary>
        /// [WARNING] ISBN of selected warehouse tracking detail doesn't match
        /// </summary>
        public const string WarehouseTracking_Warning0007 = "WarehouseTracking.Warning0007";
        /// <summary>
        /// [WARNING] Selected warehouse tracking detail is incorrect, cataloging item need ISBN to continue
        /// </summary>
        public const string WarehouseTracking_Warning0008 = "WarehouseTracking.Warning0008";
        /// <summary>
        /// [WARNING] Cannot change data as warehouse tracking was completed or cancelled
        /// </summary>
        public const string WarehouseTracking_Warning0009 = "WarehouseTracking.Warning0009";
        /// <summary>
        /// [WARNING] Cannot process delete as warehouse tracking detail still contains item
        /// </summary>
        public const string WarehouseTracking_Warning0010 = "WarehouseTracking.Warning0010";
        /// <summary>
        /// [WARNING] The action cannot be performed as category of item and warehouse tracking detail is different
        /// </summary>
        public const string WarehouseTracking_Warning0011 = "WarehouseTracking.Warning0011";
        #endregion

        #endregion
    }
}