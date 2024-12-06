
namespace FPTU_ELibrary.Application.Utils
{
	// Summary:
	//		Provide utility procedures to handle any logic related to String datatype
	public static class StringUtils
	{
		private static readonly Random _rnd = new Random();

		// Generate unique code with specific length
		public static string GenerateCode(int length = 6)
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			return new string(Enumerable.Repeat(chars, length)
										.Select(s => s[_rnd.Next(s.Length)])
										.ToArray());
		}

		// Generates a unique token using the current UTC timestamp and a secure GUID.
		public static string GenerateTokenWithTimeStamp()
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
	}
}
