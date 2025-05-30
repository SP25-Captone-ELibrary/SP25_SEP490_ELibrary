﻿using System;
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
        /// [FAIL] Unable to delete because it is bound to other data
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
		/// [WARNING] Unable to progress {0} as {1} already exist
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
		/// [WARNING] Unable to delete {0} as it is in use
		/// </summary>
		public static string SYS_Warning0007 = "SYS.Warning0007";
		/// <summary>
		/// [WARNING] Unable to edit because it is bound to other data
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
		/// [WARNING] Unable to process as the account has not enabled 2-factor authentication yet
		/// </summary>
		public static string Auth_Warning0011 = "Auth.Warning0011";
		/// <summary>
		/// [WARNING] Backup code is not valid
		/// </summary>
		public static string Auth_Warning0012 = "Auth.Warning0012";
		/// <summary>
		/// [WARNING] Please sign in to access this feature
		/// </summary>
		public static string Auth_Warning0013 = "Auth.Warning0013";
		
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
		/// [SUCCESS] Total {0} item(s) have been added to borrow record successfully
		/// </summary>
		public const string Borrow_Success0003 = "Borrow.Success0003";
		/// <summary>
		/// [SUCCESS] Register library digital resource success
		/// </summary>
		public const string Borrow_Success0004 = "Borrow.Success0004";
		/// <summary>
		/// [SUCCESS] Extend expiration date for library digital resource success
		/// </summary>
		public const string Borrow_Success0005 = "Borrow.Success0005";
		/// <summary>
		/// [SUCCESS] Total {0} Item(s) extended successfully
		/// </summary>
		public const string Borrow_Success0006 = "Borrow.Success0006";
		/// <summary>
		/// [SUCCESS] Add item to borrow request successfully
		/// </summary>
		public const string Borrow_Success0007 = "Borrow.Success0007";
		/// <summary>
		/// [SUCCESS] Total {0} items have been returned successfully
		/// </summary>
		public const string Borrow_Success0008 = "Borrow.Success0008";
		/// <summary>
		/// [SUCCESS] Cancel borrowing {0} digital item(s) successfully
		/// </summary>
		public const string Borrow_Success0009 = "Borrow.Success0009";
		/// <summary>
        /// [SUCCESS] Cancel {0} item reservations successfully
        /// </summary>
        public const string Borrow_Success0010 = "Borrow.Success0010";
		/// <summary>
        /// [SUCCESS] Cancel borrow request successfully
        /// </summary>
        public const string Borrow_Success0011 = "Borrow.Success0011";
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
		/// [WARNING] A maximum of {0} items can be borrowed at the same time. Currently, you may still borrow {1} more.
		/// Please contact the library if you need to borrow more
		/// </summary>
		public const string Borrow_Warning0005 = "Borrow.Warning0005";
		/// <summary>
		/// [WARNING] Unable to cancel because item has been proceeded
		/// </summary>
		public const string Borrow_Warning0006 = "Borrow.Warning0006";
		/// <summary>
		/// [WARNING] Unable to process as borrow request has been expired
		/// </summary>
		public const string Borrow_Warning0007 = "Borrow.Warning0007";
		/// <summary>
		/// [WARNING] Unable to process as library card is incorrect
		/// </summary>
		public const string Borrow_Warning0008 = "Borrow.Warning0008";
		/// <summary>
		/// [WARNING] The total number of items entered is not enough compared to the total number registered to borrow
		/// </summary>
		public const string Borrow_Warning0009 = "Borrow.Warning0009";
		/// <summary>
		/// [WARNING] The total number of items entered exceeds the total number registered for borrowing
		/// </summary>
		public const string Borrow_Warning0010 = "Borrow.Warning0010";
		/// <summary>
		/// [WARNING] Unable to create borrow record because borrow request has been processed
		/// </summary>
		public const string Borrow_Warning0011 = "Borrow.Warning0011";
		/// <summary>
		/// [WARNING] Unable to create borrow record because borrow request has been cancelled
		/// </summary>
		public const string Borrow_Warning0012 = "Borrow.Warning0012";
		/// <summary>
		/// [WARNING] Unable to create borrow record because borrow request has been expired
		/// </summary>
		public const string Borrow_Warning0013 = "Borrow.Warning0013";
		/// <summary>
		/// [WARNING] Instance belongs to borrowing item
		/// </summary>
		public const string Borrow_Warning0014 = "Borrow.Warning0014";
		/// <summary>
		/// [WARNING] Digital resource {0} is borrowing. Unable to not create register
		/// </summary>
		public const string Borrow_Warning0015 = "Borrow.Warning0015";
		/// <summary>
		/// [WARNING] Digital resource {0} is borrowed but now is in expired status. You can extend expiration date in your borrowed history
		/// </summary>
		public const string Borrow_Warning0016 = "Borrow.Warning0016";
		/// <summary>
		/// [WARNING] Digital resource {0} not found in borrow history to process extend expiration date
		/// </summary>
		public const string Borrow_Warning0017 = "Borrow.Warning0017";
		/// <summary>
		/// Digital resource has not been borrowed. Please create borrow form for this resource before accessing
		/// </summary>
		public const string Borrow_Warning0018 = "Borrow.Warning0018";
		/// <summary>
		/// Digital resource was borrowed unsuccessfully or expired
		/// </summary>
		public const string Borrow_Warning0019 = "Borrow.Warning0019";
		/// <summary>
		/// [WARNING] Unable to process extend borrow record expiration as this item has already been reserved. Please return the item by {0} to ensure its continued circulations
		/// </summary>
		public const string Borrow_Warning0020 = "Borrow.Warning0020";
		/// <summary>
		/// [WARNING] Unable to process extend borrow record before date {0}
		/// </summary>
		public const string Borrow_Warning0021 = "Borrow.Warning0021";
		/// <summary>
		/// [WARNING] Failed to add item to borrow request as {0}
		/// </summary>
		public const string Borrow_Warning0022 = "Borrow.Warning0022";
		/// <summary>
		/// [WARNING] Item {0} doesn't exist in borrow record
		/// </summary>
		public const string Borrow_Warning0023 = "Borrow.Warning0023";
		/// <summary>
		/// [WARNING] Item {0} doesn't exist in borrow record
		/// </summary>
		public const string Borrow_Warning0024 = "Borrow.Warning0024";
		/// <summary>
		/// [WARNING] Please add fines for overdue item
		/// </summary>
		public const string Borrow_Warning0025 = "Borrow.Warning0025";
		/// <summary>
		/// [WARNING] Fine has been duplicated in return item
		/// </summary>
		public const string Borrow_Warning0026 = "Borrow.Warning0026";
		/// <summary>
		/// [WARNING] Please update item status for return item
		/// </summary>
		public const string Borrow_Warning0027 = "Borrow.Warning0027";
		/// <summary>
		/// [WARNING] Unable to change item status to lost as item isn't in borrowing status
		/// </summary>
		public const string Borrow_Warning0028 = "Borrow.Warning0028";
		/// <summary>
		/// [WARNING] Items marked as lost must not exist in return list
		/// </summary>
		public const string Borrow_Warning0029 = "Borrow.Warning0029";
		/// <summary>
        /// [WARNING] Confirm all item need to be returned in day but not exist in return list
        /// </summary>
        public const string Borrow_Warning0030 = "Borrow.Warning0030";
        /// <summary>
        /// [WARNING] Please add fines for item marked as lost
        /// </summary>
        public const string Borrow_Warning0031 = "Borrow.Warning0031";
		/// <summary>
        /// [WARNING] Exist duplicate or inappropriate fines for return item marked as lost
        /// </summary>
        public const string Borrow_Warning0032 = "Borrow.Warning0032";
        /// <summary>
        /// [WARNING] Estimated price of item not found. Please update it to process lost return
        /// </summary>
        public const string Borrow_Warning0033 = "Borrow.Warning0033";
        /// <summary>
        /// [WARNING] Not found price value for fine policy {0}
        /// </summary>
        public const string Borrow_Warning0034 = "Borrow.Warning0034";
        /// <summary>
        /// [WARNING] Fine policy is invalid for returning item
        /// </summary>
        public const string Borrow_Warning0035 = "Borrow.Warning0035";
        /// <summary>
        /// [WARNING] Reader has already reserved this item and haven been assigned with instance {0}. Requested instance {1} is not match
        /// </summary>
        public const string Borrow_Warning0036 = "Borrow.Warning0036";
        /// <summary>
        /// [WARNING] Unable to create borrow record as item {0} has been requested by reader by but have not been processed yet
        /// </summary>
        public const string Borrow_Warning0037 = "Borrow.Warning0037";
        /// <summary>
	    /// [WARNING] Unable to create borrow record as exist {0} pending item requests awaiting processing
	    /// </summary>
	    public const string Borrow_Warning0038 = "Borrow.Warning0038";
        /// <summary>
	    /// [WARNING] Unable to create borrow record as there are still {0} items have been reserved and successfully assigned to user, but have not been yet processed
	    /// </summary>
	    public const string Borrow_Warning0039 = "Borrow.Warning0039";
	    /// <summary>
	    /// [WARNING] A maximum of {0} items can be borrowed at the same time. Currently, reader may still borrow {1} more
	    /// </summary>
	    public const string Borrow_Warning0040 = "Borrow.Warning0040";
	    /// <summary>
	    /// [WARNING] A maximum of {0} items can be borrowed at the same time. Total requested items to create borrow record is {1}. Please update max amount can borrow of reader
	    /// </summary>
	    public const string Borrow_Warning0041 = "Borrow.Warning0041";
	    /// <summary>
	    /// [WARNING] A maximum of {0} items can be borrowed at the same time. Please contact the library if you need to borrow more
	    /// </summary>
	    public const string Borrow_Warning0042 = "Borrow.Warning0042";
		
		/// <summary>
		/// [FAIL] An error occurred, the item borrowing registration failed
		/// </summary>
		public const string Borrow_Fail0001 = "Borrow.Fail0001";
		/// <summary>
		/// [FAIL] An error occured, failed to create borrow record
		/// </summary>
		public const string Borrow_Fail0002 = "Borrow.Fail0002";
		/// <summary>
		/// [FAIL] Failed to register library digital resource as {0}
		/// </summary>
		public const string Borrow_Fail0003 = "Borrow.Fail0003";
		/// <summary>
		/// [FAIL] Failed to register library digital resource
		/// </summary>
		public const string Borrow_Fail0004 = "Borrow.Fail0004";
		/// <summary>
		/// [FAIL] Failed to extend library digital resource as {0}
		/// </summary>
		public const string Borrow_Fail0005 = "Borrow.Fail0005";
		/// <summary>
        /// [FAIL] Failed to extend library digital resource expiration date
        /// </summary>
        public const string Borrow_Fail0006 = "Borrow.Fail0006";
        /// <summary>
        /// [FAIL] Failed to extend borrow record as {0}
        /// </summary>
        public const string Borrow_Fail0007 = "Borrow.Fail0007";
        /// <summary>
        /// [FAIL] Failed to add item to borrow request
        /// </summary>
        public const string Borrow_Fail0008 = "Borrow.Fail0008";
        /// <summary>
        /// [FAIL] Failed to cancel borrow request
        /// </summary>
        public const string Borrow_Fail0009 = "Borrow.Fail0009";
        /// <summary>
        /// [FAIL] Failed to process return items
        /// </summary>
        public const string Borrow_Fail0010 = "Borrow.Fail0010";
        /// <summary>
        /// [FAIL] Error has been occurred. An item instance belongs to several reservations at the same time
        /// </summary>
        public const string Borrow_Fail0011 = "Borrow.Fail0011";
        /// <summary>
        /// [FAIL] Failed to cancel digital borrow request
        /// </summary>
        public const string Borrow_Fail0012 = "Borrow.Fail0012";
        /// <summary>
        /// [FAIL] Failed to cancel item reservation 
        /// </summary>
        public const string Borrow_Fail0013 = "Borrow.Fail0013";
		#endregion

		#region Reservatation
		/// <summary>
		/// [SUCCESS] Total {0} items have been confirmed reservation label
		/// </summary>
		public const string Reservation_Success0001 = "Reservation.Success0001";
		/// <summary>
		/// [SUCCESS] Assign items success for {0} reservations
		/// </summary>
		public const string Reservation_Success0002 = "Reservation.Success0002";
		/// <summary>
		/// [SUCCESS] The pickup expiration date has been extended to {0}
		/// </summary>
		public const string Reservation_Success0003 = "Reservation.Success0003";
		/// <summary>
        /// [FAIL] The instance has been reserved by the reader, but failed to update the reservation status while create borrow record
        /// </summary>
        public const string Reservation_Fail0001 = "Reservation.Fail0001";
        /// <summary>
        /// [FAIL] Failed to confirm reservation label application
        /// </summary>
        public const string Reservation_Fail0002 = "Reservation.Fail0002";
        /// <summary>
        /// [FAIL] Unable to assign items to reservations
        /// </summary>
        public const string Reservation_Fail0003 = "Reservation.Fail0003";
        /// <summary>
        /// [FAIL] Item instances' status must be out of shelf to process assigning items for reservations
        /// </summary>
        public const string Reservation_Fail0004 = "Reservation.Fail0004";
        /// <summary>
        /// [FAIL] Item instances' status must be out of shelf and circulated to process assigning items for reservations
        /// </summary>
        public const string Reservation_Fail0005 = "Reservation.Fail0005";
        /// <summary>
        /// [FAIL] Failed to get applied reservation information
        /// </summary>
        public const string Reservation_Fail0006 = "Reservation.Fail0006";
        /// <summary>
        /// [FAIL] Failed to extend pick up expiration date
        /// </summary>
        public const string Reservation_Fail0007 = "Reservation.Fail0007";
		
		/// <summary>
		/// [WARNING] Unable to create item reservation as {0}
		/// </summary>
		public const string Reservation_Warning0001 = "Reservation.Warning0001";
		/// <summary>
		/// [WARNING] Unable to reserve for item {0} as you are requesting for this item
		/// </summary>
		public const string Reservation_Warning0002 = "Reservation.Warning0002";
		/// <summary>
		/// [WARNING] Unable to reserve for item {0} as you are borrowing this item
		/// </summary>
		public const string Reservation_Warning0003 = "Reservation.Warning0003";
		/// <summary>
		/// [WARNING] You has already reserved item {0}
		/// </summary>
		public const string Reservation_Warning0004 = "Reservation.Warning0004";
		/// <summary>
		/// [WARNING] Unable to reserve for item {0} as this item is still available to borrow
		/// </summary>
		public const string Reservation_Warning0005 = "Reservation.Warning0005";
		/// <summary>
		/// [WARNING] Unable to confirm reservation label application as {0}
		/// </summary>
		public const string Reservation_Warning0006 = "Reservation.Warning0006";
		/// <summary>
		/// [WARNING] Unable to find assignable items as {0}
		/// </summary>
		public const string Reservation_Warning0007 = "Reservation.Warning0007";
		/// <summary>
        /// [WARNING] The pickup expiration date has been extended. Cannot extend more
        /// </summary>
        public const string Reservation_Warning0008 = "Reservation.Warning0008";
        /// <summary>
        /// [WARNING] Unable to extend pickup expiration date as {0}
        /// </summary>
        public const string Reservation_Warning0009 = "Reservation.Warning0009";
		#endregion
		
		#region Role
		/// <summary>
		/// [WARNING] Role name already exist, please check again
		/// </summary>
		public const string Role_Warning0001 = "Role.Warning0001";
		/// <summary>
		/// [WARNING] Unable to update as role is invalid
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
		/// [SUCCESS] Send notification successfully
		/// </summary>
		public static string Notification_Success0001 = "Notification.Success0001";
		/// <summary>
		/// [WARNING] There is no important message 
		/// </summary>
		public static string Notification_Warning0001 = "Notification.Warning0001";
		/// <summary>
		/// [WARNING] Access to noti that not belongs to this account 
		/// </summary>
		public static string Notification_Warning0002 = "Notification.Warning0002";
		/// <summary>
		/// [WARNING] Not found user's email {0} to process send privacy notification
		/// </summary>
		public static string Notification_Warning0003 = "Notification.Warning0003";
		/// <summary>
		/// [FAIL] Failed to send notification
		/// </summary>
		public static string Notification_Fail0001 = "Notification.Fail0001";
		#endregion

		#region Library Item
		/// <summary>
		/// [SUCCESS] Item has been shelved successfully
		/// </summary>
		public const string LibraryItem_Success0001 = "LibraryItem.Success0001";
		/// <summary>
		/// [SUCCESS] Total {0} item have been shelved successfully
		/// </summary>
		public const string LibraryItem_Success0002 = "LibraryItem.Success0002";
		/// <summary>
        /// [SUCCESS] Item has been unshelved successfully
        /// </summary>
        public const string LibraryItem_Success0003 = "LibraryItem.Success0003";
        /// <summary>
        /// [SUCCESS] Total {0} item have been unshelved successfully
        /// </summary>
        public const string LibraryItem_Success0004 = "LibraryItem.Success0004";
        /// <summary>
        /// [SUCCESS] You have succesfully rated the item
        /// </summary>
        public const string LibraryItem_Success0005 = "LibraryItem.Success0005";
        /// <summary>
        /// [SUCCESS] Added to favorites successfully
        /// </summary>
        public const string LibraryItem_Success0006 = "LibraryItem.Success0006";
        /// <summary>
        /// [SUCCESS] Item removed from favorites successfully
        /// </summary>
        public const string LibraryItem_Success0007 = "LibraryItem.Success0007";
        /// <summary>
        /// [SUCCESS] Update group items successfully
        /// </summary>
        public const string LibraryItem_Success0008 = "LibraryItem.Success0008";
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
		/// [WARNING] Unable to change data that is on borrowing or reserved
		/// </summary>
		public const string LibraryItem_Warning0008 = "LibraryItem.Warning0008";
		/// <summary>
		/// [WARNING] Unable to process, please change all item instance status to inventory first
		/// </summary>
		public const string LibraryItem_Warning0009 = "LibraryItem.Warning0009";
		/// <summary>
		/// [WARNING] Unable to delete as there still exist instance within the item
		/// </summary>
		public const string LibraryItem_Warning0010 = "LibraryItem.Warning0010";
		/// <summary>
		/// [WARNING] Unable to change item instance status. {0}
		/// </summary>
		public const string LibraryItem_Warning0011 = "LibraryItem.Warning0011";
		/// <summary>
		/// [WARNING] Unable to change item status to public. {0}
		/// </summary>
		public const string LibraryItem_Warning0012 = "LibraryItem.Warning0012";
		/// <summary>
		/// [WARNING] Unable to change item status to draft. {0}
		/// </summary>
		public const string LibraryItem_Warning0013 = "LibraryItem.Warning0013";
		/// <summary>
		/// [WARNING] Required all item instance to have the same prefix of new category
		/// </summary>
		public const string LibraryItem_Warning0014 = "LibraryItem.Warning0014";
		/// <summary>
		/// [WARNING] The number of instance item exceed than default config threshold. Please modify system configuration to continue
		/// </summary>
		public const string LibraryItem_Warning0015 = "LibraryItem.Warning0015";
		/// <summary>
		/// [WARNING] The number of instance item exceed than default config threshold. Please modify system configuration to continue
		/// </summary>
		public const string LibraryItem_Warning0016 = "LibraryItem.Warning0016";
		/// <summary>
		/// [WARNING] Unable to place item {0} on shelf {1}
		/// </summary>
		public const string LibraryItem_Warning0017 = "LibraryItem.Warning0017";
		/// <summary>
		/// [WARNING] No suitable shelf found for the requested item
		/// </summary>
		public const string LibraryItem_Warning0018 = "LibraryItem.Warning0018";
		/// <summary>
		/// [WARNING] The item's status cannot be updated because it has not been shelved yet
		/// </summary>
		public const string LibraryItem_Warning0019 = "LibraryItem.Warning0019";
		/// <summary>
        /// [WARNING] Unable to update item's shelf status to 'in-shelf' as {0}
        /// </summary>
        public const string LibraryItem_Warning0020 = "LibraryItem.Warning0020";
        /// <summary>
        /// [WARNING] Unable to update item's shelf status to 'out-of-shelf' as {0}
        /// </summary>
        public const string LibraryItem_Warning0021 = "LibraryItem.Warning0021";
        /// <summary>
        /// [WARNING] Unable to update item instance status as {0}
        /// </summary>
        public const string LibraryItem_Warning0022 = "LibraryItem.Warning0022";
        /// <summary>
        /// [WARNING] Resource does not belong to this item
        /// </summary>
        public const string LibraryItem_Warning0023 = "LibraryItem.Warning0023";
        /// <summary>
	    /// [WARNING] There are currently {0} pending reservations.Please assign appropriate items for pending reservations
	    /// </summary>
	    public const string LibraryItem_Warning0024 = "LibraryItem.Warning0024";
        /// <summary>
        /// [WARNING] Unable to put the items out of shelf because the available quantity cannot be less than the total number of borrowing requests
        /// </summary>
        public const string LibraryItem_Warning0025 = "LibraryItem.Warning0025";
		/// <summary>
        /// [WARNING] Unable to put out of shelf for item {0} as the number of items on shelf cannot be less than the number of borrowing requests
        /// </summary>
        public const string LibraryItem_Warning0026 = "LibraryItem.Warning0026";
        /// <summary>
        /// [WARNING] Item instance has been in lost status
        /// </summary>
        public const string LibraryItem_Warning0027 = "LibraryItem.Warning0027";
        /// <summary>
        /// [WARNING] Item instance is not in lost status
        /// </summary>
        public const string LibraryItem_Warning0028 = "LibraryItem.Warning0028";
        /// <summary>
        /// [WARNING] Item category is not belong to book series to process add to group items
        /// </summary>
        public const string LibraryItem_Warning0029 = "LibraryItem.Warning0029";
        /// <summary>
        /// [WARNING] Item {0} cannot group with group {1} as it does not match cutter number, classification number and author with group detail
        /// </summary>
        public const string LibraryItem_Warning0030 = "LibraryItem.Warning0030";
        /// <summary>
        /// [WARNING] Unable to create a new group because the document may be grouped with existing groups. Please check again
        /// </summary>
        public const string LibraryItem_Warning0031 = "LibraryItem.Warning0031";
        /// <summary>
        /// [WARNING] Unable to update DDC range, as still existing {0} library items on shelf
        /// </summary>
        public const string LibraryItem_Warning0032 = "LibraryItem.Warning0032";
        /// <summary>
        /// [WARNING] Unable to update shelf number, as still existing {0} library items on shelf
        /// </summary>
        public const string LibraryItem_Warning0033 = "LibraryItem.Warning0033";
        /// <summary>
		/// [FAIL] An error occurred while updating the inventory data
		/// </summary>
		public const string LibraryItem_Fail0001 = "LibraryItem.Fail0001";
		/// <summary>
		/// [FAIL] The action cannot be performed. Please switch to draft status to proceed
		/// </summary>
		public const string LibraryItem_Fail0002 = "LibraryItem.Fail0002";
		/// <summary>
		/// [FAIL] Shelving the item was unsuccessful
		/// </summary>
		public const string LibraryItem_Fail0003 = "LibraryItem.Fail0003";
		/// <summary>
		/// [FAIL] Unshelving the item was unsuccessful
		/// </summary>
		public const string LibraryItem_Fail0004 = "LibraryItem.Fail0004";
		/// <summary>
		/// [FAIL] Failed to rating for the item
		/// </summary>
		public const string LibraryItem_Fail0005 = "LibraryItem.Fail0005";
		/// <summary>
        /// [FAIL] Failed to update group items
        /// </summary>
        public const string LibraryItem_Fail0006 = "LibraryItem.Fail0006";
		#endregion

		#region Library Card
		/// <summary>
		/// [SUCCESS] Library card is valid
		/// </summary>
		public const string LibraryCard_Success0001 = "LibraryCard.Success0001";
		/// <summary>
		/// [SUCCESS] Register library card success
		/// </summary>
		public const string LibraryCard_Success0002 = "LibraryCard.Success0002";
		/// <summary>
		/// [SUCCESS] The library card has been archived and is no longer valid
		/// </summary>
		public const string LibraryCard_Success0003 = "LibraryCard.Success0003";
		/// <summary>
		/// [SUCCESS] Send card reconfirmation successfully
		/// </summary>
		public const string LibraryCard_Success0004 = "LibraryCard.Success0004";
		/// <summary>
		/// [SUCCESS] Extend library card expiration successfully
		/// </summary>
		public const string LibraryCard_Success0005 = "LibraryCard.Success0005";
		/// <summary>
		/// [WARNING] Library card is not activated yet. Please contact library to activate your card
		/// </summary>
		public const string LibraryCard_Warning0001 = "LibraryCard.Warning0001";
		/// <summary>
		/// [WARNING] Library card has expired. Please renew it to continue using library services
		/// </summary>
		public const string LibraryCard_Warning0002 = "LibraryCard.Warning0002";
		/// <summary>
		/// [WARNING] Library card has been suspended due to a violation or administrative action. Please contact the library for assistance (Activated at {0})
		/// </summary>
		public const string LibraryCard_Warning0003 = "LibraryCard.Warning0003";
		/// <summary>
		/// [WARNING] You need a library card to access this service
		/// </summary>
		public const string LibraryCard_Warning0004 = "LibraryCard.Warning0004";
		/// <summary>
		/// [WARNING] Library card does not match the card registered to borrow
		/// </summary>
		public const string LibraryCard_Warning0005 = "LibraryCard.Warning0005";
		/// <summary>
		/// [WARNING] Fail to extend library card as {0}
		/// </summary>
		public const string LibraryCard_Warning0006 = "LibraryCard.Warning0006";
		/// <summary>
		/// [WARNING] The action cannot be performed, as library card need to change status to archived
		/// </summary>
		public const string LibraryCard_Warning0007 = "LibraryCard.Warning0007";
		/// <summary>
		/// [WARNING] Fail to update. Total borrow amount threshold is not smaller than {0}
		/// </summary>
		public const string LibraryCard_Warning0008 = "LibraryCard.Warning0008";
		/// <summary>
		/// [WARNING] Unable to update library card status to {0} as {1}
		/// </summary>
		public const string LibraryCard_Warning0009 = "LibraryCard.Warning0009";
		/// <summary>
		/// [WARNING] Unable to process confirm card as not found payment information
		/// </summary>
		public const string LibraryCard_Warning0010 = "LibraryCard.Warning0010";
		/// <summary>
		/// [WARNING] Fail to confirm card as library card has been confirmed
		/// </summary>
		public const string LibraryCard_Warning0011 = "LibraryCard.Warning0011";
		/// <summary>
		/// [WARNING] Fail to reject library card as it has been rejected
		/// </summary>
		public const string LibraryCard_Warning0012 = "LibraryCard.Warning0012";
		/// <summary>
		/// [WARNING] Unable to process send card reconfirmation when card status is not rejected
		/// </summary>
		public const string LibraryCard_Warning0013 = "LibraryCard.Warning0013";
		/// <summary>
		/// [WARNING] Library card has not paid yet
		/// </summary>
		public const string LibraryCard_Warning0014 = "LibraryCard.Warning0014";
		/// <summary>
		/// [WARNING] Register library card failed
		/// </summary>
		public const string LibraryCard_Fail0001 = "LibraryCard.Fail0001";
		/// <summary>
		/// [WARNING] Failed to archive library card
		/// </summary>
		public const string LibraryCard_Fail0002 = "LibraryCard.Fail0002";
		/// <summary>
		/// [WARNING] Failed to send reconfirmation library card
		/// </summary>
		public const string LibraryCard_Fail0003 = "LibraryCard.Fail0003";
		/// <summary>
		/// [WARNING] Fail to extend library card
		/// </summary>
		public const string LibraryCard_Fail0004 = "LibraryCard.Fail0004";
		/// <summary>
		/// [WARNING] Register library card failed as {0}
		/// </summary>
		public const string LibraryCard_Fail0005 = "LibraryCard.Fail0005";
		/// <summary>
		/// [WARNING] Fail to extend library card as {0}
		/// </summary>
		public const string LibraryCard_Fail0006 = "LibraryCard.Fail0006";
		#endregion

		#region Library Closure Day
		/// <summary>
		/// [SUCCESS] The time already exists in the library's closure days list
		/// </summary>
		public const string LibraryClosureDay_Warning0001 = "LibraryClosureDay.Warning0001";
		/// <summary>
		/// [SUCCESS] Closure datetime is invalid
		/// </summary>
		public const string LibraryClosureDay_Warning0002 = "LibraryClosureDay.Warning0002";
		#endregion
		
		#region Supplier
		/// <summary>
		/// [WARNING] The action cannot be performed, as the supplier is in the warehouse tracking information has been approved
		/// </summary>
		public const string Supplier_Warning0001 = "Supplier.Warning0001";
		#endregion
		
        #region AIService
        /// <summary>
        /// [SUCCESS] Data matched with image
        /// </summary>
        public static string AIService_Success0001 = "AIService.Success0001";
        /// <summary>
        /// [SUCCESS] Train sucessfully
        /// </summary>
        public static string AIService_Success0002 = "AIService.Success0002";
        /// <summary>
        /// [SUCCESS] Start Training model successfully
        /// </summary>
        public static string AIService_Success0003 = "AIService.Success0003";
        /// <summary>
        /// [SUCCESS] Predict Successfully
        /// </summary>
        public static string AIService_Success0004 = "AIService.Success0004";
        /// <summary>
        /// [SUCCESS] Grouped items successfully
        /// </summary>
        public static string AIService_Success0005 = "AIService.Success0005";
        /// <summary>
        /// [SUCCESS] Success to detect face image
        /// </summary>
        public static string AIService_Success0006 = "AIService.Success0006";
        /// <summary>
        /// [SUCCESS] Is able to train
        /// </summary>
        public static string AIService_Success0007 = "AIService.Success0007";
        /// <summary>
        /// [WARNING] Data did not match with image
        /// </summary>
        public static string AIService_Warning0001 = "AIService.Warning0001";
        /// <summary>
        /// [WARNING] There is an iteration that is training
        /// </summary>
        public static string AIService_Warning0002 = "AIService.Warning0002";
        /// <summary>
        /// [WARNING] Can not detect any books in the picture
        /// </summary>
        public static string AIService_Warning0003 = "AIService.Warning0003";
        /// <summary>
        /// [WARNING] Recommendation is just for 1 book.
        /// </summary>
        public static string AIService_Warning0004 = "AIService.Warning0004";
        /// <summary>
        /// [WARNING] Existing items in categories that are not allow to train AI
        /// </summary>
        public static string AIService_Warning0005 = "AIService.Warning0005";
        /// <summary>
        /// [WARNING] One or more items grouped
        /// </summary>
        public static string AIService_Warning0006 = "AIService.Warning0006";
        /// <summary>
        /// [WARNING] Fail to detect face image
        /// </summary>
        public static string AIService_Warning0007 = "AIService.Warning0007";
        /// <summary>
        /// [WARNING] Required at least 5 images for single book
        /// </summary>
        public static string AIService_Warning0008 = "AIService.Warning0008";
        /// <summary>
        /// [WARNING] There is existing AI training session
        /// </summary>
        public static string AIService_Warning0009 = "AIService.Warning0009";
		/// <summary>
        /// [WARNING] Required at least 5 items for book series
        /// </summary>
        public static string AIService_Warning0010 = "AIService.Warning0010";
        /// <summary>
        /// [WARNING] Sorry, we could not find any items matching the book cover image you uploaded.
        /// Please double-check the image or try a different one
        /// </summary>
        public static string AIService_Warning0011 = "AIService.Warning0011";
        #endregion

        #region Warehouse Tracking
        /// <summary>
        /// [SUCCESS] Total {0} warehouse tracking detail has been registered unique barcode success
        /// </summary>
        public const string WarehouseTracking_Success0001 = "WarehouseTracking.Success0001";
        /// <summary>
        /// [SUCCESS] Supplement request created successfully
        /// </summary>
        public const string WarehouseTracking_Success0002 = "WarehouseTracking.Success0002";
        /// <summary>
        /// [SUCCESS] Warehouse stock-in file archived successfully
        /// </summary>
        public const string WarehouseTracking_Success0003 = "WarehouseTracking.Success0003";
        /// <summary>
        /// [SUCCESS] Warehouse supplement request file archived successfully
        /// </summary>
        public const string WarehouseTracking_Success0004 = "WarehouseTracking.Success0004";
        /// <summary>
        /// [WARNING] Do not allow to change status from {0} to {1}
        /// </summary>
        public const string WarehouseTracking_Warning0001 = "WarehouseTracking.Warning0001";
        /// <summary>
        /// [WARNING] The action cannot be performed. Please switch warehouse tracking information to draft
        /// </summary>
        public const string WarehouseTracking_Warning0002 = "WarehouseTracking.Warning0002";
        /// <summary>
        /// [WARNING] Unable to delete warehouse tracking information, as existing item has been cataloged
        /// </summary>
        public const string WarehouseTracking_Warning0003 = "WarehouseTracking.Warning0003";
        /// <summary>
        /// [WARNING] Unable to change status to completed, as existing item has not been cataloged yet
        /// </summary>
        public const string WarehouseTracking_Warning0004 = "WarehouseTracking.Warning0004";
        /// <summary>
        /// [WARNING] Unable to change status to completed, as total item instance is not
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
        /// [WARNING] Unable to change data as warehouse tracking was completed or cancelled
        /// </summary>
        public const string WarehouseTracking_Warning0009 = "WarehouseTracking.Warning0009";
        /// <summary>
        /// [WARNING] Unable to process delete as warehouse tracking detail still contains item
        /// </summary>
        public const string WarehouseTracking_Warning0010 = "WarehouseTracking.Warning0010";
        /// <summary>
        /// [WARNING] The action cannot be performed as category of item and warehouse tracking detail is different
        /// </summary>
        public const string WarehouseTracking_Warning0011 = "WarehouseTracking.Warning0011";
        /// <summary>
        /// [WARNING] Unable to process update as exist item has been cataloged
        /// </summary>
        public const string WarehouseTracking_Warning0012 = "WarehouseTracking.Warning0012";
        /// <summary>
        /// [WARNING] Warehouse tracking detail has already been in other item
        /// </summary>
        public const string WarehouseTracking_Warning0013 = "WarehouseTracking.Warning0013";
        /// <summary>
        /// [WARNING] Unable to change data as existing item has been cataloged
        /// </summary>
        public const string WarehouseTracking_Warning0014 = "WarehouseTracking.Warning0014";
        /// <summary>
        /// [WARNING] Warehouse tracking file type is invalid
        /// </summary>
        public const string WarehouseTracking_Warning0015 = "WarehouseTracking.Warning0015";
        /// <summary>
        /// [WARNING] Unable to change item when tracking type is stock out or transfer
        /// </summary>
        public const string WarehouseTracking_Warning0016 = "WarehouseTracking.Warning0016";
        /// <summary>
        /// [WARNING] Warehouse tracking detail has already been registered unique barcode
        /// </summary>
        public const string WarehouseTracking_Warning0017 = "WarehouseTracking.Warning0017";
        /// <summary>
        /// [WARNING] Unable to register unique barcode for warehouse tracking detail as it has not been cataloged yet
        /// </summary>
        public const string WarehouseTracking_Warning0018 = "WarehouseTracking.Warning0018";
        /// <summary>
        /// [WARNING] Stock transaction type {0} is not valid to registering unique barcode for item
        /// </summary>
        public const string WarehouseTracking_Warning0019 = "WarehouseTracking.Warning0019";
        /// <summary>
        /// [WARNING] Not found range unique registration barcode. Please update to continue
        /// </summary>
        public const string WarehouseTracking_Warning0020 = "WarehouseTracking.Warning0020";
        /// <summary>
        /// [WARNING] Unique barcode range of warehouse tracking detail is not valid. Please modify and try again
        /// </summary>
        public const string WarehouseTracking_Warning0021 = "WarehouseTracking.Warning0021";
        /// <summary>
        /// [WARNING] Supplement request item not found
        /// </summary>
        public const string WarehouseTracking_Warning0022 = "WarehouseTracking.Warning0022";
        /// <summary>
        /// [WARNING] ISBN doesn't match with supplement request item
        /// </summary>
        public const string WarehouseTracking_Warning0023 = "WarehouseTracking.Warning0023";
        /// <summary>
        /// [WARNING] The stock transaction type {0} is invalid for creating a supplement request
        /// </summary>
        public const string WarehouseTracking_Warning0024 = "WarehouseTracking.Warning0024";
        /// <summary>
        /// [WARNING] The ISBN for the item suggested for extra acquisition is already in use
        /// </summary>
        public const string WarehouseTracking_Warning0025 = "WarehouseTracking.Warning0025";
        /// <summary>
        /// [WARNING] No related item was found for supplement request item
        /// </summary>
        public const string WarehouseTracking_Warning0026 = "WarehouseTracking.Warning0026";
        /// <summary>
        /// [WARNING] No file found for warehouse stock-in file to proceed with storage
        /// </summary>
        public const string WarehouseTracking_Warning0027 = "WarehouseTracking.Warning0027";
        /// <summary>
        /// [WARNING] Unable to create a file for the warehouse receipt as it is not in a completed status.
        /// Please verify the receipt status and complete the process before creating the file
        /// </summary>
        public const string WarehouseTracking_Warning0028 = "WarehouseTracking.Warning0028";
        /// <summary>
        /// [WARNING] Unable to create a file for the supplement warehouse request receipt as it is not in a completed status.
        /// Please verify the receipt status and complete the process before creating the file
        /// </summary>
        public const string WarehouseTracking_Warning0029 = "WarehouseTracking.Warning0029";
        /// <summary>
        /// [WARNING] Tracking type is invalid to process creating file
        /// </summary>
        public const string WarehouseTracking_Warning0030 = "WarehouseTracking.Warning0030";
		/// <summary>
        /// [FAIL] Failed to register unique barcode for warehouse tracking detail
        /// </summary>
        public const string WarehouseTracking_Fail0001 = "WarehouseTracking.Fail0001";
        /// <summary>
        /// [FAIL] Failed to create supplement request
        /// </summary>
        public const string WarehouseTracking_Fail0002 = "WarehouseTracking.Fail0002";
        /// <summary>
        /// [FAIL] Failed to archive the warehouse receipt file. Please check and try again later
        /// </summary>
        public const string WarehouseTracking_Fail0003 = "WarehouseTracking.Fail0003";
        /// <summary>
        /// [FAIL] Failed to archive the supplement warehouse request receipt file. Please check and try again later
        /// </summary>
        public const string WarehouseTracking_Fail0004 = "WarehouseTracking.Fail0004";
        #endregion

        #region Transaction
        /// <summary>
        /// [SUCCESS] Create payment link successfully
        /// </summary>
        public const string Transaction_Success0001 = "Transaction.Success0001";
        /// <summary>
        /// [SUCCESS] Verify payment transaction successfully
        /// </summary>
        public const string Transaction_Success0002 = "Transaction.Success0002";
        /// <summary>
        /// [SUCCESS] Cancel payment transaction successfully
        /// </summary>
        public const string Transaction_Success0003 = "Transaction.Success0003";
        /// <summary>
        /// [WARNING] Not found library card to process extend expiration date
        /// </summary>
        public const string Transaction_Warning0001 = "Transaction.Warning0001";
        /// <summary>
        /// [WARNING] Unable to process create payment for library card register as not found card information
        /// </summary>
        public const string Transaction_Warning0002 = "Transaction.Warning0002";
        /// <summary>
        /// [WARNING] Failed to create payment transaction as existing transaction with pending status
        /// </summary>
        public const string Transaction_Warning0003 = "Transaction.Warning0003";
        /// <summary>
        /// [FAIL] Failed to create payment transaction. Please try again
        /// </summary>
        public const string Transaction_Fail0001 = "Transaction.Fail0001";
        /// <summary>
        /// [FAIL] Payment object does not exist. Please try again
        /// </summary>
        public const string Transaction_Fail0002 = "Transaction.Fail0002";
        /// <summary>
        /// [FAIL] You are not allowed to create this type of transaction
        /// </summary>
        public const string Transaction_Fail0003 = "Transaction.Fail0003";
        /// <summary>
        /// [FAIL] Error has been occurred. Failed to create payment link
        /// </summary>
        public const string Transaction_Fail0004 = "Transaction.Fail0004";
        /// <summary>
        /// [FAIL] Not found any transaction match to verify
        /// </summary>
        public const string Transaction_Fail0005 = "Transaction.Fail0005";
        /// <summary>
        /// [FAIL] Failed to verify payment transaction
        /// </summary>
        public const string Transaction_Fail0006 = "Transaction.Fail0006";
        /// <summary>
        /// [FAIL] Failed to cancel payment transaction
        /// </summary>
        public const string Transaction_Fail0007 = "Transaction.Fail0007";
        #endregion
        
        #region Fine
        /// <summary>
        /// [WARNING] Paid for {0} fines successfully
        /// </summary>
        public const string Fine_Success0001 = "Fine.Success0001";
        /// <summary>
        /// [WARNING] Existing {0} fines haven't been paid yet. Please make payment to continue
        /// </summary>
        public const string Fine_Warning0001 = "Fine.Warning0001";
        /// <summary>
        /// [WARNING] Failed to pay for fines as {0}
        /// </summary>
        public const string Fine_Fail0001 = "Fine.Fail0001";
        /// <summary>
        /// [WARNING] Failed to pay for fines
        /// </summary>
        public const string Fine_Fail0002 = "Fine.Fail0002";
        #endregion
        #endregion
    }
}