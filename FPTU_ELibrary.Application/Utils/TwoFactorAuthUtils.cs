using OtpNet;
using QRCoder;

namespace FPTU_ELibrary.Application.Utils;

//  Summary:
//      This class is to provide procedures to handle two-factor authentication feature
public class TwoFactorAuthUtils
{
    #region Generator
    //  Summary:
    //      Generate secret key for specific user
    public static string GenerateSecretKey()
    {
        var key = KeyGeneration.GenerateRandomKey(20); // 160-bit key
        return Base32Encoding.ToString(key); // Base32 encoding for compatibility
    }
    
    //  Summary:
    //      Generate QrCode URI
    public static string GenerateQrCodeUri(string email, string secretKey, string issuer)
    {
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}" +
               $"?secret={secretKey}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";
    }
    
    //  Summary:
    //      Generate QrCode from URI
    public static byte[] GenerateQrCode(string uri)
    {
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);

        // Generate QR code as PNG bytes
        return qrCode.GetGraphic(20);
    }
    
    //  Summary:
    //      Generate back up codes for authenticator recovery
    public static List<string> GenerateBackupCodes(int count = 5)
    {
        var backupCodes = new List<string>();
        for (int i = 0; i < count; i++)
        {
            backupCodes.Add(Guid.NewGuid().ToString("N").Substring(0, 10)); // 10-character random code
        }
        return backupCodes;
    }
    #endregion
    
    #region Verification
    public static bool VerifyOtp(string secretKey, string otpCode)
    {
        var totp = new Totp(Base32Encoding.ToBytes(secretKey));
        return totp.VerifyTotp(otpCode, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
    }
    
    #endregion

    #region Hashing
    public static IEnumerable<string> HashBackupCodes(IEnumerable<string> codes)
    {
        // Hash the codes for secure storage
        return codes.Select(code => BCrypt.Net.BCrypt.HashPassword(code));
    }

    public static string? VerifyBackupCodeAndGetMatch(string providedCode, IEnumerable<string> hashedCodes)
    {
        return hashedCodes.FirstOrDefault(hash => BCrypt.Net.BCrypt.Verify(providedCode, hash));
    }
    #endregion
}