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
		/// [SUCCESS] Create new <0> successfully
		/// </summary> 
		public static string SYS_Success0006 = "SYS.Success0006";
		
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
		/// [FAIL] Fail to get data: <0>
		/// </summary>
		public static string SYS_Fail0005 = "SYS.Fail0005";
		/// <summary>
		/// [FAIL] Fail to create new <0>
		/// </summary>
		public static string SYS_Fail0006 = "SYS.Fail0006";
		
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
		/// [WARNING] Data not found
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
		/// [FAIL] Fail to update password
		/// </summary>
		public static string Auth_Fail0001 = "Auth.Fail0001";
		#endregion

		#endregion
	}
}
