using System.Security.Cryptography;
using System.Text;

namespace FPTU_ELibrary.Application.Utils
{
    // Summary:
    //		Provide utility procedures to handle any logic related to encrypt/decrypt
    public class HashUtils
    {
        //  Summary:
        //      Verify that the hash of given text matches to provided hash
        public static bool VerifyPassword(string password, string storedHash)
            => BC.EnhancedVerify(password, storedHash);

        //  Summary:
        //      Pre-hash a password with SHA384 then using the OpenBSD BCrypt scheme and a salt
        public static string HashPassword(string password)
            => BC.EnhancedHashPassword(password, 13);

        //  Summary:
        //      Generate random password base on specific requirement
        public static string GenerateRandomPassword(int length = 8)
        {
            // Define character groups
            const string uppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"; // Uppercase letters
            const string lowercaseChars = "abcdefghijklmnopqrstuvwxyz"; // Lowercase letters
            const string digitChars = "0123456789"; // Numbers
            const string specialChars = "@"; // Special character
            const string allChars = uppercaseChars + digitChars; // Combined character pool

            Random random = new Random();
            StringBuilder password = new StringBuilder();

            // Ensure at least one uppercase letter and one special character
            password.Append(uppercaseChars[random.Next(uppercaseChars.Length)]);
            password.Append(specialChars[random.Next(specialChars.Length)]);

            // Fill the remaining characters randomly from all character pools
            for (int i = 3; i < length; i++)
            {
                password.Append(allChars[random.Next(allChars.Length)]);
            }

            // Shuffle the characters to prevent predictable patterns
            return new string(password.ToString().OrderBy(c => random.Next()).ToArray());
        }
        
        //  Summary:
        //      HmacSha256 Encoding
        public static string HmacSha256(string text, string key)
        {
            ASCIIEncoding encoding = new ASCIIEncoding();

            Byte[] textBytes = encoding.GetBytes(text);
            Byte[] keyBytes = encoding.GetBytes(key);
            Byte[] hashBytes;
            using (HMACSHA256 hash = new HMACSHA256(keyBytes))
                hashBytes = hash.ComputeHash(textBytes);

            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}