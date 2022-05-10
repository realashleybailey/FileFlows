namespace FileFlows.ServerShared.Helpers;

public class FileHelper
{

    /// <summary>
    /// Removes illegal file/path characters from a string
    /// </summary>
    /// <param name="input">the string to clean</param>
    /// <returns>the original string with all illegal characters removed</returns>
    public static string RemoveIllegalCharacters(string input)
    {
        string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        foreach (var c in invalid)
            input = input.Replace(c.ToString(), string.Empty);
        return input;
    }

    /// <summary>
    /// Calculates a fingerprint for a file
    /// </summary>
    /// <param name="file">The filename</param>
    /// <returns>The fingerprint</returns>
    public static string CalculateFingerprint(string file)
    {
        try
        {
            var fileInfo = new FileInfo(file);
            if (fileInfo.Exists == false)
                return String.Empty;

            using var hasher = System.Security.Cryptography.SHA256.Create();
            DateTime start = DateTime.Now;
            byte[] hash;
            if (fileInfo.Length > 100_000_000)
            {
                // compute hash on first 100MB to speed it update
                using (var stream = new FileStream(file, FileMode.Open))
                {
                    var bytes = new byte[100_000_000];
                    int realLength = stream.Read(bytes, 0, bytes.Length);
                    hash = hasher.ComputeHash(bytes, 0, realLength);
                    bytes = null;
                }
                GC.Collect();
            }
            else
            {
                using var stream = File.OpenRead(file);
                hash = hasher.ComputeHash(stream);
            }
            string hashStr = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            return hashStr;
        }
        catch(Exception ex)
        {
            Logger.Instance?.ELog($"Failed to calculate fingerprint for file '{file}': {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            return string.Empty;
        }
    }
}
