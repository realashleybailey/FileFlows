namespace FileFlows.ServerShared.Helpers;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Class used to encrypt and decrypt communication from the licensing server
/// </summary>
class LicenseEncryption
{

    /// <summary>
    /// Encrypts a string
    /// </summary>
    /// <param name="key">the encryption key as a base64 string</param>
    /// <param name="data">the string to encrypt</param>
    /// <returns>the string encrypted as a base64 string</returns>
    internal static string Encrypt(string key, string? data)
    {
        if (string.IsNullOrEmpty(data))
            return string.Empty;
        
        RSA rsa = RSA.Create();

        var cipher = new RSACryptoServiceProvider();
        byte[] publicKey = Convert.FromBase64String(key);
        cipher.ImportRSAPublicKey(publicKey, out int bytesRead);
        byte[] bData = Encoding.UTF8.GetBytes(data);
        byte[] cipherText = cipher.Encrypt(bData, false);
        return Convert.ToBase64String(cipherText);
    }
    
    /// <summary>
    /// Encrypts a string
    /// </summary>
    /// <param name="key">the encryption key as a base64 string</param>
    /// <param name="data">the string to decrypt</param>
    /// <returns>the string decrypted</returns>
    internal static string Decrypt(string key, string? data)
    {
        if (string.IsNullOrEmpty(data))
            return string.Empty;
        try
        {
            var cipher = new RSACryptoServiceProvider();
            byte[] privateKey = Convert.FromBase64String(key);
            cipher.ImportRSAPrivateKey(privateKey, out int bytesRead);

            byte[] ciphterText = Convert.FromBase64String(data);
            byte[] plainText = cipher.Decrypt(ciphterText, false);
            string result = Encoding.UTF8.GetString(plainText);

            return result;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }    
}