namespace FileFlows.Shared.Helpers
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using FileFlows.Shared.Models;

    public class HttpHelper
    {
        public static HttpClient Client { get; set; }

        public static async Task<RequestResult<T>> Get<T>(string url)
        {
            return await MakeRequest<T>(HttpMethod.Get, url);
        }
        public static async Task<RequestResult<T>> Get<T>(string url, int timeoutSeconds = 0)
        {
            return await MakeRequest<T>(HttpMethod.Get, url, timeoutSeconds: timeoutSeconds);
        }
#if (!DEMO)
        public static async Task<RequestResult<string>> Post(string url, object data = null)
        {
            return await MakeRequest<string>(HttpMethod.Post, url, data);
        }
        public static async Task<RequestResult<T>> Post<T>(string url, object data = null, int timeoutSeconds = 0)
        {
            return await MakeRequest<T>(HttpMethod.Post, url, data, timeoutSeconds: timeoutSeconds);
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

        private static async Task<RequestResult<T>> MakeRequest<T>(HttpMethod method, string url, object data = null, int timeoutSeconds = 0)
        {
            try
            {
#if (DEBUG)
                if (url.Contains("i18n") == false && url.StartsWith("http") == false)
                    url = "http://localhost:6868" + url;
#endif
                if(url.Contains("fileflows.com") == false)
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


                Console.WriteLine("Making request[" + method + "]: " + url);
                HttpResponseMessage response;
                if (timeoutSeconds > 0)
                {
                    using var cancelToken = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                    response = await Client.SendAsync(request, cancelToken.Token);
                }
                else
                    response = await Client.SendAsync(request);

                if (typeof(T) == typeof(byte[]))
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    if (response.IsSuccessStatusCode)
                        return new RequestResult<T> { Success = true, Data = (T)(object)bytes };
                    return new RequestResult<T> { Success = false };
                }

                string body = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode && body.Contains("An unhandled error has occurred.") == false)
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
                    if (body.Contains("An unhandled error has occurred."))
                        body = "An unhandled error has occurred."; // asp.net error
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