namespace ViWatcher.Client.Helpers
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class HttpHelper 
    {
        public static HttpClient Client { get; set; }

        public static async Task<RequestResult<T>> Get<T>(string url)
        {
            return await MakeRequest<T>(HttpMethod.Get, url);
        }

        public static async Task<RequestResult<T>> Post<T>(string url, object data = null)
        {
            return await MakeRequest<T>(HttpMethod.Post, url, data);
        }
        public static async Task<RequestResult<T>> Put<T>(string url, object data = null)
        {
            return await MakeRequest<T>(HttpMethod.Put, url, data);
        }

        private static async Task<RequestResult<T>> MakeRequest<T>(HttpMethod method, string url, object data = null)
        {            
            try
            {
#if (DEBUG)
                if(url.Contains("i18n") == false)
                    url = "http://localhost:6868" + url;
#endif
                Logger.Instance.DLog("About to request: " + url);
                var request = new HttpRequestMessage
                {
                    Method = method,
                    RequestUri = new Uri(url, UriKind.RelativeOrAbsolute),
                    Content = data != null ? data.AsJson() : null
                };

                if (method == HttpMethod.Post && data == null)
                {
                    // if this is null, asp.net will return a 415 content not support, as the content-type will not be set
                    request.Content = new StringContent("", System.Text.Encoding.UTF8, "application/json");
                }

                var response = await Client.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    T result = typeof(T) == typeof(string) ? (T)(object)body : JsonConvert.DeserializeObject<T>(body);
                    return new RequestResult<T> { Success = true, Body = body, Data = result };
                }
                else
                {
                    return new RequestResult<T> { Success = false, Body = body, Data = default(T) };
                }
            }
            catch (Exception)
            {
                throw;
            }
        }   
    }
}