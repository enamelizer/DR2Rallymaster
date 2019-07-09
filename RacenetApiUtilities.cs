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
        // The client used to get data from the API, contains the user authentication cookies
        private readonly HttpClient httpClient;

        public RacenetApiUtilities(CookieContainer sharedCookieContainer)
        {
            var httpClientHandler = new HttpClientHandler { CookieContainer = sharedCookieContainer };
            httpClient = new HttpClient(httpClientHandler, true);
        }

        // Given a club ID, generate the appropriate URL and fetch the data
        public async Task<Tuple<HttpStatusCode, string>> GetClubInfo(string clubId)
        {
            // example URL https://dirtrally2.com/api/Club/183582
            // debug
            clubId = "183582";

            var baseUrl = "https://dirtrally2.com/api/Club/";

            // no need to make a http call with a known bad club ID
            if (String.IsNullOrWhiteSpace(clubId))
                return new Tuple<HttpStatusCode, string>(HttpStatusCode.NotFound, null);

            // create URL and query API
            var clubUrl = baseUrl + clubId;
            return await GetStringAsync(clubUrl);
        }

        // Given a URI, send a GET and return the status code and result as a string
        private async Task<Tuple<HttpStatusCode, string>> GetStringAsync(string uri)
        {
            // send the get and await the response
            var response = await httpClient.GetAsync(uri);
            var statusCode = response.StatusCode;

            // if we succeed, get the data and return it
            if (statusCode == HttpStatusCode.OK)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                return new Tuple<HttpStatusCode, string>(statusCode, responseData);
            }

            // if we failed, send the status code back so the caller knows why
            return new Tuple<HttpStatusCode, string>(statusCode, null);
        }
    }
}
