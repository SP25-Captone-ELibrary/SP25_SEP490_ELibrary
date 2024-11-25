namespace FPTU_ELibrary.Application.Utils
{
	// Summary:
	//		Provide utility procedures to handle any logic related to encrypt/decrypt
	public class HashUtils
	{
		public static bool VerifyPassword(string password, string storedHash)
			// Verfifies that the hash of given text matches to provided hash
			=> BC.EnhancedVerify(password, storedHash);

		public static string HashPassword(string password)
			// Pre-hash a password with SHA384 then using the OpenBSD BCrypt scheme and a salt
			=> BC.EnhancedHashPassword(password, 13);
	}
}
