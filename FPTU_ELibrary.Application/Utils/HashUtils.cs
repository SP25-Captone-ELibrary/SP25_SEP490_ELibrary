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
        public static string GenerateRandomPassword()
        {
            //Collection of password requirement 
            const string upperCaseLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string specialCharacters = "@$!%*?&";
            const string allCharacters =
                "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@$!%*?&";

            Random random = new Random();

            char[] result = new char[8];

            // Process randomly add value for blank value of default password
            int upperCasePosition = random.Next(8);
            int digitPosition;
            int specialCharPosition;
            do
            {
                digitPosition = random.Next(8);
            } while (digitPosition == upperCasePosition);

            do
            {
                specialCharPosition = random.Next(8);
            } while (specialCharPosition == upperCasePosition || specialCharPosition == digitPosition);
            
            result[upperCasePosition] = upperCaseLetters[random.Next(upperCaseLetters.Length)];
            result[digitPosition] = digits[random.Next(digits.Length)];
            result[specialCharPosition] = specialCharacters[random.Next(specialCharacters.Length)];
            
            for (int i = 0; i < 8; i++)
            {
                if (result[i] == '\0') //only add value for the blank value
                {
                    result[i] = allCharacters[random.Next(allCharacters.Length)];
                }
            }
            return new string(result);
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