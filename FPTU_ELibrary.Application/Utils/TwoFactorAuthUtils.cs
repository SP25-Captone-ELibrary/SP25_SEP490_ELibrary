using System.Security.Cryptography;
using System.Text;
using FPTU_ELibrary.Application.Configurations;
using OtpNet;
using QRCoder;

namespace FPTU_ELibrary.Application.Utils;

//  Summary:
//      This class is to provide procedures to handle two-factor authentication feature
public class TwoFactorAuthUtils
{
    private static byte[] _key = null!;
    private static byte[] _iv = null!;
    
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
    //  Summary:
    //      Encrypts backup codes 
    public static IEnumerable<string> EncryptBackupCodes(IEnumerable<string> codes, AppSettings appSettings)
    {
        _key = Convert.FromBase64String(appSettings.AESKey);
        _iv = Convert.FromBase64String(appSettings.AESIV);
        
        return codes.Select(Encrypt);
    }
    
    //  Summary:
    //      Decrypts backup codes
    public static IEnumerable<string> DecryptBackupCodes(IEnumerable<string> encryptedCodes, AppSettings appSettings)
    {
        _key = Convert.FromBase64String(appSettings.AESKey);
        _iv = Convert.FromBase64String(appSettings.AESIV);
        
        return encryptedCodes.Select(Decrypt);
    }

    // Summary:
    //      Verifies a provided backup code 
    public static string? VerifyBackupCodeAndGetMatch(string providedCode, IEnumerable<string> encryptedCodes, AppSettings appSettings)
    {
        _key = Convert.FromBase64String(appSettings.AESKey);
        _iv = Convert.FromBase64String(appSettings.AESIV);
        
        return encryptedCodes
            .FirstOrDefault(encryptedCode => Decrypt(encryptedCode) == providedCode);
    }
    
    private static string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);

        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(encryptedBytes);
    }
    
    private static string Decrypt(string encryptedText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        var encryptedBytes = Convert.FromBase64String(encryptedText);

        var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
        return Encoding.UTF8.GetString(decryptedBytes);
    }
    
    #endregion
}