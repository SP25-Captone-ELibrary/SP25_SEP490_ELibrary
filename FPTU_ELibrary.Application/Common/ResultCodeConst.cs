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
		/// [WARNING] Invalid inputs
		/// </summary>
		public static string SYS_Warning0001 = "SYS.Warning0001";
		/// <summary>
		/// [WARNING] Not found
		/// </summary>
		public static string SYS_Warning0002 = "SYS.Warning0002";
		/// <summary>
		/// [WARNING] Already exist
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
		public const string Cloud_Warning0003 = "Cloud.Warning0003";
		/// <summary>
		/// [WARNING] Not found video resource
		/// </summary>
		public const string Cloud_Warning0004 = "Cloud.Warning0004";
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
		/// [Warning] Require User code is missing  
		/// </summary>
		public static string User_Warning0001 = "User.Warning0001";
		/// <summary>
		/// [Warning] Invalid role has been chosen  
		/// </summary>
		public static string User_Warning0002 = "User.Warning0002";

		#endregion
		#region Notification

		/// <summary>
		/// There is no important message 
		/// </summary>
		public static string Notification_Warning0001 = "Notification.Warning0001";

		#endregion

		#endregion
	}
}
