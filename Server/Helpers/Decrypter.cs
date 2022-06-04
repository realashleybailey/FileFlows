using System.Security.Cryptography;
using System.Text;

namespace FileFlows.Server.Helpers
{
    public class Decrypter
    {
        internal static string EncryptionKey => AppSettings.Instance.EncryptionKey;

        public static string Decrypt(string text)
        {
            byte[] IV = Convert.FromBase64String(text.Substring(0, 20));
            text = text.Substring(20).Replace(" ", "+");
            byte[] cipherBytes = Convert.FromBase64String(text);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, IV);
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    text = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return text;

        }

        public static string Encrypt(string text)
        {
            byte[] clearBytes = Encoding.Unicode.GetBytes(text);
            Random rand= new Random(DateTime.Now.Millisecond);
            using (Aes encryptor = Aes.Create())
            {
                byte[] IV = new byte[15];
                rand.NextBytes(IV);
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, IV);
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    text = Convert.ToBase64String(IV) + Convert.ToBase64String(ms.ToArray());
                }
            }
            return text;
        }
    }
}
