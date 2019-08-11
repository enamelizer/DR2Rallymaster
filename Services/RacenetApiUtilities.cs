using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DR2Rallymaster.Services
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

        // Makes a call to get inital state to get the cross site scripting token set
        // this is required to fetch stage results
        public async Task<bool> GetInitialState()
        {
            try
            {
                var initialState = await GetStringAsync("https://dirtrally2.com/api/ClientStore/GetInitialState");
                if (initialState.Item1 != HttpStatusCode.OK || String.IsNullOrWhiteSpace(initialState.Item2))
                    return false;

                dynamic data = JObject.Parse(initialState.Item2);
                var headerName = (data.application.antiForgeryHeaderName).ToString(); // RaceNet.XSRFH
                var token = (data.identity.token).ToString();

                // setup headers
                httpClient.DefaultRequestHeaders.Add(headerName, token);
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
                httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");                                                              // maybe take this from the user's locale?
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:68.0) Gecko/20100101 Firefox/68.0");   // shhhhh lets pretend
                //httpClient.DefaultRequestHeaders.Add();
            }
            catch
            {
                return false;
            }

            return true;
        }

        // Given a club ID, generate the appropriate URL and fetch the club data
        public async Task<Tuple<HttpStatusCode, string>> GetClubInfo(string clubId)
        {
            // example URL https://dirtrally2.com/api/Club/183582

            var baseUrl = "https://dirtrally2.com/api/Club/";

            // no need to make a http call with a known bad club ID
            if (String.IsNullOrWhiteSpace(clubId))
                return new Tuple<HttpStatusCode, string>(HttpStatusCode.NotFound, null);

            // create URL and query API
            var clubUrl = baseUrl + clubId;
            return await GetStringAsync(clubUrl);
        }

        // Given a club ID, generate the appropriate URL and get the championship data
        public async Task<Tuple<HttpStatusCode, string>> GetChampionshipInfo(string clubId)
        {
            // example URL https://dirtrally2.com/api/Club/183582/championships

            var baseUrl = "https://dirtrally2.com/api/Club/{0}/championships";

            // no need to make a http call with a known bad club ID
            if (String.IsNullOrWhiteSpace(clubId))
                return new Tuple<HttpStatusCode, string>(HttpStatusCode.NotFound, null);

            // create URL and query API
            var champUrl = String.Format(baseUrl, clubId);
            return await GetStringAsync(champUrl);
        }

        // Given a club ID, get the recent results
        public async Task<Tuple<HttpStatusCode, string>> GetRecentResults(string clubId)
        {
            // example URL https://dirtrally2.com/api/Club/183582/recentResults
            var baseUrl = "https://dirtrally2.com/api/Club/{0}/recentResults";

            // no need to make a http call with a known bad club ID
            if (String.IsNullOrWhiteSpace(clubId))
                return new Tuple<HttpStatusCode, string>(HttpStatusCode.NotFound, null);

            // create URL and query API
            var recentResultsUrl = String.Format(baseUrl, clubId);
            return await GetStringAsync(recentResultsUrl);
        }

        public async Task<Tuple<HttpStatusCode, string>> GetStageResults(string challengeId, string eventId, string stageId)
        {
            // example request payload
            // {"challengeId":"15146","selectedEventId":0,"stageId":"0","page":1,"pageSize":100,"orderByTotalTime":true,"platformFilter":"None","playerFilter":"Everyone","filterByAssists":"Unspecified","filterByWheel":"Unspecified","nationalityFilter":"None","eventId":"15309"}
            var baseRequestPayload = "{{\"challengeId\":\"{0}\",\"selectedEventId\":0,\"stageId\":\"{1}\",\"page\":1,\"pageSize\":100,\"orderByTotalTime\":true,\"platformFilter\":\"None\",\"playerFilter\":\"Everyone\",\"filterByAssists\":\"Unspecified\",\"filterByWheel\":\"Unspecified\",\"nationalityFilter\":\"None\",\"eventId\":\"{2}\"}}";
            var requestPayload = String.Format(baseRequestPayload, challengeId, stageId, eventId);

            var leaderboardUrl = "https://dirtrally2.com/api/Leaderboard";

            var response = await PostStringAsync(leaderboardUrl, requestPayload);

            return response;
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

        // Given a URI, send a POST with params and return the status code and result as a string
        private async Task<Tuple<HttpStatusCode, string>> PostStringAsync(string uri, string requestPayload)
        {
            // send the get and await the response
            var response = await httpClient.PostAsync(uri, new StringContent(requestPayload, Encoding.UTF8, "application/json"));
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
