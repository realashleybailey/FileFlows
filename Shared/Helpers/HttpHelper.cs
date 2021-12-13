namespace FileFlows.Shared.Helpers
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using FileFlows.Shared.Models;

    public class HttpHelper
    {
        public static HttpClient Client { get; set; }

        public static async Task<RequestResult<T>> Get<T>(string url)
        {
            return await MakeRequest<T>(HttpMethod.Get, url);
        }
#if (!DEMO)
        public static async Task<RequestResult<string>> Post(string url, object data = null)
        {
            return await MakeRequest<string>(HttpMethod.Post, url, data);
        }
        public static async Task<RequestResult<T>> Post<T>(string url, object data = null)
        {
            return await MakeRequest<T>(HttpMethod.Post, url, data);
        }
        public static async Task<RequestResult<string>> Put(string url, object data = null)
        {
            return await MakeRequest<string>(HttpMethod.Put, url, data);
        }
        public static async Task<RequestResult<T>> Put<T>(string url, object data = null)
        {
            return await MakeRequest<T>(HttpMethod.Put, url, data);
        }

        public static async Task<RequestResult<string>> Delete(string url, object data = null)
        {
            return await MakeRequest<string>(HttpMethod.Delete, url, data);
        }
#endif

        private static async Task<RequestResult<T>> MakeRequest<T>(HttpMethod method, string url, object data = null)
        {
            try
            {
#if (DEBUG)
                if (url.Contains("i18n") == false && url.StartsWith("http") == false)
                    url = "http://localhost:6868" + url;
#endif
                Logger.Instance?.DLog("About to request: " + url);
                var request = new HttpRequestMessage
                {
                    Method = method,
                    RequestUri = new Uri(url, UriKind.RelativeOrAbsolute),
                    Content = data != null ? AsJson(data) : null
                };

                if (method == HttpMethod.Post && data == null)
                {
                    // if this is null, asp.net will return a 415 content not support, as the content-type will not be set
                    request.Content = new StringContent("", Encoding.UTF8, "application/json");
                }

                var response = await Client.SendAsync(request);

                if (typeof(T) == typeof(byte[]))
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    if (response.IsSuccessStatusCode)
                        return new RequestResult<T> { Success = true, Data = (T)(object)bytes };
                    return new RequestResult<T> { Success = false };
                }

                string body = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = { new FileFlows.Shared.Json.ValidatorConverter() }
                    };
                    T result = typeof(T) == typeof(string) ? (T)(object)body : JsonSerializer.Deserialize<T>(body, options);
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

        private static StringContent AsJson(object o)
        {
            string json = o.ToJson();
            return new StringContent(json, Encoding.UTF8, "application/json");
        }
    }
}