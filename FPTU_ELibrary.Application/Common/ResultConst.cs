using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPTU_ELibrary.Application.Common
{
	public class ResultConst
	{
		#region Success Codes

		public static int SUCCESS_SIGNIN_CODE = 1;
		public static string SUCCESS_SIGNIN_MSG = "Sign in successfully";

		public static int SUCCESS_SIGNUP_CODE = 1;
		public static string SUCCESS_SIGNUP_MSG = "Sign up successfully";

		public static int SUCCESS_INSERT_CODE = 1;
		public static string SUCCESS_INSERT_MSG = "Save data success";

		public static int SUCCESS_READ_CODE = 1;
		public static string SUCCESS_READ_MSG = "Get data success";

		public static int SUCCESS_UPDATE_CODE = 1;
		public static string SUCCESS_UPDATE_MSG = "Update data success";

		public static int SUCCESS_REMOVE_CODE = 1;
		public static string SUCCESS_REMOVE_MSG = "Remove data success";

		#endregion

		#region Fail Code

		public static int FAIL_INSERT_CODE = -1;
		public static string FAIL_INSERT_MSG = "Save data fail";

		public static int FAIL_READ_CODE = -1;
		public static string FAIL_READ_MSG = "Get data fail";

		public static int FAIL_UPDATE_CODE = -1;
		public static string FAIL_UPDATE_MSG = "Update data fail";

		public static int FAIL_REMOVE_CODE = -1;
		public static string FAIL_REMOVE_MSG = "Remove data fail";


		#endregion

		#region Warning Code

		public static int WARNING_NO_DATA_CODE = 2;
		public static string WARNING_NO_DATA_MSG = "No Data Found";

		#endregion
	}
}
