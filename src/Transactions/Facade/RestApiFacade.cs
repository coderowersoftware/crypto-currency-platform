using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Transactions.Facade
{
    public interface IRestApiFacade
    {
        Task<string> SendAsync(HttpMethod httpMethod, 
            Uri uri, 
            IDictionary<string, string> headers,
            object payload,
            bool isAuthTokenRequired = false,
            Uri? authUri = null,
            IDictionary<string, string>? creds = null);
    }

    public class RestApiFacade : IRestApiFacade
    {
        private readonly HttpClient _httpClient;

        public RestApiFacade()
        {
            _httpClient = new HttpClient();

            // Ignore ssl certificate errors
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
        }

        public async Task<string> SendAsync(HttpMethod httpMethod, 
            Uri uri, 
            IDictionary<string, string> headers,
            object payload,
            bool isAuthTokenRequired = false,
            Uri? authUri = null,
            IDictionary<string, string>? creds = null)
        {
            using(HttpRequestMessage requestMessage = new HttpRequestMessage { RequestUri = uri, Method = httpMethod })
            {
                requestMessage.Headers.Add(HttpRequestHeader.Accept.ToString(), "application/json");
                requestMessage.Headers.Add(HttpRequestHeader.Connection.ToString(), "keep-alive");
                foreach(var header in headers ?? new Dictionary<string, string>())
                {
                    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                if(isAuthTokenRequired)
                {
                    requestMessage.Headers.Add(HttpRequestHeader.Authorization.ToString(), $"Bearer {await GetAuthToken(authUri, creds).ConfigureAwait(false)}");
                }

                if(httpMethod == HttpMethod.Post
                    || httpMethod == HttpMethod.Put
                    || httpMethod == HttpMethod.Patch)
                {
                    requestMessage.Content = new StringContent(JsonConvert.SerializeObject(payload, new StringEnumConverter()), Encoding.UTF8, "application/json");
                }

                HttpResponseMessage responseMessage = await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);

                if(responseMessage != null)
                {
                    if(responseMessage.IsSuccessStatusCode || responseMessage.StatusCode == HttpStatusCode.BadRequest)
                    {
                        return await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }
                    else if(responseMessage.StatusCode == HttpStatusCode.NotFound
                        || responseMessage.StatusCode == HttpStatusCode.NoContent)
                    {
                        return string.Empty;
                    }
                }
                return string.Empty;
            }
        }

        private async Task<string> GetAuthToken(Uri? authUri, IDictionary<string, string>? creds)
        {
            if(authUri == null || creds == null) throw new ArgumentNullException(nameof(creds));
            using(HttpRequestMessage request = new HttpRequestMessage { RequestUri = authUri, Method = HttpMethod.Post })
            {
                request.Headers.Add(HttpRequestHeader.Accept.ToString(), "application/json");
                request.Content = new StringContent(JsonConvert.SerializeObject(creds), Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _httpClient.SendAsync(request).ConfigureAwait(false);

                if(response != null)
                {
                    if(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }
                    else if(response.StatusCode == HttpStatusCode.NotFound
                        || response.StatusCode == HttpStatusCode.NoContent)
                    {
                        return string.Empty;
                    }
                }
            }
            return string.Empty;
        }
    }
}