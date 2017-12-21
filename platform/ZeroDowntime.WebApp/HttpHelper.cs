using System;
using System.Text;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;


namespace ZeroDowntime.WebApp
{
    public static class HttpHelper
    {
        private static readonly HttpClient httpClient;

        static HttpHelper()
        {
            httpClient = new HttpClient();
        }


        /// <summary>
        /// Helper method for HTTP Post request
        /// </summary>
        /// <param name="requestUri"></param>
        /// <param name="apiKey"></param>
        /// <param name="postBody"></param>
        /// <returns></returns>
        public static async Task<string> PostAsync(string requestUri, string postBody)
        {
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            //httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var httpContent = new StringContent(postBody, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(requestUri, httpContent);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            throw new Exception($"request {requestUri} returned status code {response.StatusCode} for request {requestUri} input {postBody}");
        }

        /// <summary>
        /// Helper mthod for HTTP get request
        /// </summary>
        /// <param name="requestUri"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public static async Task<string> GetAsync(string requestUri)
        {
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            //httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
 
            var response = await httpClient.GetAsync(requestUri); 
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            throw new Exception($"request {requestUri} returned status code {response.StatusCode}");
        }

    }
}
