
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FPTU_ELibrary.Application.Utils
{
	// Summary:
	//		Provide utility procedures to handle any logic related to String datatype
	public static class StringUtils
	{
		private static readonly Random _rnd = new Random();

		// Generate unique code with specific length
		public static string GenerateUniqueCode(int length = 6)
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			return new string(Enumerable.Repeat(chars, length)
										.Select(s => s[_rnd.Next(s.Length)])
										.ToArray());
		}

		// Generate unique code based on current timestamp
		public static string GenerateUniqueCodeWithTimestamp(int length = 6)
		{
			if (length <= 0) return null!;
			
			// Get the current timestamp
			string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");

			// Generate a random string 
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
			Random random = new Random();
			StringBuilder randomString = new StringBuilder();

			for (int i = 0; i < length; i++)
			{
				randomString.Append(chars[random.Next(chars.Length)]);
			}

			// Combine timestamp and random string
			string combinedCode = timestamp + randomString;
			// Substring to specific length 
			return combinedCode.Substring(0, Math.Min(length, combinedCode.Length));
		}
		
		// Generates a unique token using the current UTC timestamp and a secure GUID.
		public static string GenerateTokenWithTimestamp()
		{
			try
			{
				// Generate timestamp as a byte array
				byte[] time = BitConverter.GetBytes(DateTime.UtcNow.ToBinary());

				// Generate key with random GUID
				byte[] key = Guid.NewGuid().ToByteArray();

				// Combine timestamp and key, then encode in Base64
				return Convert.ToBase64String(time.Concat(key).ToArray());
			}
			catch (Exception ex)
			{
				// Handle any unexpected errors
				throw new InvalidOperationException("Error generating token.", ex);
			}
		}

		// Validates the provided token by decoding its timestamp and ensuring it's within a valid time frame.
		public static bool IsValidTokenWithTimeStamp(string token, int expirationMinutes = 5)
		{
			try
			{
				// Decode the Base64 string to get the original byte array
				byte[] tokenByteArray = Convert.FromBase64String(token);

				// Extract and convert the timestamp from the first 8 bytes
				long timestamp = BitConverter.ToInt64(tokenByteArray, 0);
				DateTime when = DateTime.FromBinary(timestamp);

				// Check if the token has expired
				return when >= DateTime.UtcNow.AddMinutes(-expirationMinutes);
			}
			catch (FormatException ex)
			{
				// Handle invalid Base64 strings
				throw new ArgumentException("Invalid token format.", nameof(token), ex);
			}
			catch (ArgumentOutOfRangeException ex)
			{
				// Handle tokens with insufficient bytes
				throw new ArgumentException("Invalid token structure.", nameof(token), ex);
			}
			catch (Exception ex)
			{
				// Unexpected errors
				throw new InvalidOperationException("Error validating token.", ex);
			}
		}
		
		// Formats a string by replacing placeholders like <0>, <1>, etc., with the provided arguments.
		public static string Format(string input, params string[]? args)
		{
			if (string.IsNullOrEmpty(input))
				return null!; 

			if (args == null || args.Length == 0)
				return input; // Return original string if no args provided.

			for (int i = 0; i < args.Length; i++)
			{
				input = input.Replace($"<{i}>", args[i]);
			}

			return input;
		}
		
		// Add white space to string
		public static string AddWhitespaceToString(string input)
		{
			if (string.IsNullOrEmpty(input))
				return input;

			// Use a regex to identify boundaries between lowercase and uppercase letters
			string result = Regex.Replace(input, "([a-z])([A-Z])", "$1 $2");

			return result;
		}
		
		// Remove word and add white space to string
		public static string RemoveWordAndAddWhitespace(string input, string wordToRemove)
		{
			if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(wordToRemove))
				return input;

			// Remove the specified word
			string withoutWord = Regex.Replace(input, wordToRemove, "", RegexOptions.IgnoreCase);

			// Add whitespace to the remaining string
			return AddWhitespaceToString(withoutWord);
		}
		
		// Convert string to CamelCase
		public static string ToCamelCase(string s)
		{
			if (string.IsNullOrEmpty(s) || !char.IsUpper(s[0]))
			{
				return s;
			}

			var chars = s.ToCharArray();

			for (var i = 0; i < chars.Length; i++)
			{
				if (i == 1 && !char.IsUpper(chars[i]))
				{
					break;
				}

				var hasNext = (i + 1 < chars.Length);
				if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
				{
					break;
				}

				chars[i] = char.ToLower(chars[i], CultureInfo.InvariantCulture);
			}

			return new string(chars);
		}
		
		// Validate numeric & datetime
		public static bool IsNumeric(string text) => int.TryParse(text, out _);  
		public static bool IsDateTime(string text) => DateTime.TryParse(text, out _);
		
		// Validate Http/Https Url
		public static bool IsValidUrl(string url)
		{
			Uri? uriResult;
			return Uri.TryCreate(url, UriKind.Absolute, out uriResult) 
			              && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
		}
		
		// Get Public Id from Url 
		public static string? GetPublicIdFromUrl(string url)
		{
			// Check whether URL is valid
			if (!IsValidUrl(url)) return null;
	        
            var lastSlashIndex = url.LastIndexOf('/');
            var lastDotIndex = url.LastIndexOf('.');
			
            // Check if both slash and dot exist in the URL
            if (lastSlashIndex == -1 || lastDotIndex == -1 || lastDotIndex < lastSlashIndex) return null;
    
            // Extract the public ID
            return url.Substring(lastSlashIndex + 1, lastDotIndex - lastSlashIndex - 1);
        }
	}
}
