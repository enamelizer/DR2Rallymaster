using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DR2Rallymaster
{
    class RacenetApiUtilities
    {
        private HttpClient httpClient;

        public RacenetApiUtilities(CookieContainer sharedCookieContainer)
        {
            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.CookieContainer = sharedCookieContainer;
            httpClient = new HttpClient(httpClientHandler, true);
        }

        public void GetClubInfo(string clubId)
        {
            // example URL https://dirtrally2.com/api/Club/183582
            // debug
            clubId = "183582";

            var baseUrl = "https://dirtrally2.com/api/Club/";

            if (String.IsNullOrWhiteSpace(clubId))
                return;

            // create URL and query API
            var clubUrl = baseUrl + clubId;
            var resultString = GetStringAsync(clubUrl);

            // debug
            System.Diagnostics.Debug.WriteLine(resultString);
        }

        private async Task<string> GetStringAsync(string uri)
        {
            var response = httpClient.GetAsync(uri).Result;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                return responseData;
            }

            return null;
        }
    }
}
