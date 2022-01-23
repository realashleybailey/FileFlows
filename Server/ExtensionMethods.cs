namespace FileFlows.Server
{
    /// <summary>
    /// Extension methods
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Downloads a remote file and saves it locally
        /// </summary>
        /// <param name="client">The HttpClient instance</param>
        /// <param name="url">The url to download the file from</param>
        /// <param name="filename">The filename to save the file to</param>
        /// <returns>an awaited task</returns>
        public static async Task DownloadFile(this HttpClient client, string url, string filename)
        {
            using (var s = await client.GetStreamAsync(url))
            {
                using (var fs = new FileStream(filename, FileMode.CreateNew))
                {
                    await s.CopyToAsync(fs);
                }
            }
        }
    }
}
