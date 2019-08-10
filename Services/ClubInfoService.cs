using DR2Rallymaster.ApiModels;
using DR2Rallymaster.DataModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DR2Rallymaster.Services
{
    // This is the central control logic for all Club, Championship, and Event info
    // It calls the data services, creates the data models, etc.
    class ClubInfoService
    {
        // The service that performs queries to the Racenet API
        private readonly RacenetApiUtilities racenetApi = null;

        // The cached ClubInfo, should be refreshed when GetClubInfo() is called
        // otherwise we hang on to it so we can fetch data from it later
        private ClubInfoModel clubInfoCache = null;

        public ClubInfoService(CookieContainer cookieContainer)
        {
            racenetApi = new RacenetApiUtilities(cookieContainer);
        }

        // Gets the club info from the racenet service, deserializes the json,
        // and creates the ClubInfoModel
        public async Task<ClubInfoModel> GetClubInfo(string clubId)
        {
            try
            {
                var returnModel = new ClubInfoModel();

                // get club data
                var racenetClubResult = await racenetApi.GetClubInfo(clubId);
                if (racenetClubResult.Item1 != HttpStatusCode.OK || String.IsNullOrWhiteSpace(racenetClubResult.Item2))
                    return null; // TODO error handling

                var apiClubModel = JsonConvert.DeserializeObject<ClubApiModel>(racenetClubResult.Item2);
                if (apiClubModel != null)
                    returnModel.ClubInfo = apiClubModel;

                // get championship data
                var racenetChampionshipsResult = await racenetApi.GetChampionshipInfo(clubId);
                if (racenetChampionshipsResult.Item1 != HttpStatusCode.OK || String.IsNullOrWhiteSpace(racenetChampionshipsResult.Item2))
                    return null; // TODO error handling

                var apiChampionshipsModel = JsonConvert.DeserializeObject<ChampionshipMetaData[]>(racenetChampionshipsResult.Item2);
                if (racenetChampionshipsResult != null)
                    returnModel.Championships = apiChampionshipsModel;

                // get recent results
                var racenetRecentResultsResult = await racenetApi.GetRecentResults(clubId);
                if (racenetRecentResultsResult.Item1 != HttpStatusCode.OK || String.IsNullOrWhiteSpace(racenetRecentResultsResult.Item2))
                    return null; // TODO error handling

                var recentResultsApiModel = JsonConvert.DeserializeObject<RecentResultsApiModel>(racenetRecentResultsResult.Item2);
                if (recentResultsApiModel != null)
                    returnModel.RecentResults = recentResultsApiModel;


                // cache the data
                clubInfoCache = returnModel;
                return returnModel;
            }
            catch (Exception e)
            {
                // TODO error handling
                return null;
            }
        }

        // Returns the metadata for the championship and all the events for the given championship ID
        public ChampionshipMetaData GetChampionshipMetadata(string championshipId)
        {
            if (clubInfoCache == null)
                return null;   // TODO error handling?

            for (int i=0; i<clubInfoCache.Championships.Length; i++)
            {
                if (clubInfoCache.Championships[i].Id == championshipId)
                    return clubInfoCache.Championships[i];
            }

            return null;   // TODO error handling?
        }

        // Returns the metadata for the event, given the championship and event IDs
        public EventMetadata GetEventMetadata(string championshipId, string eventId)
        {
            if (clubInfoCache == null)
                return null;   // TODO error handling?

            for (int i = 0; i < clubInfoCache.Championships.Length; i++)
            {
                if (clubInfoCache.Championships[i].Id == championshipId)
                {
                    var championship = clubInfoCache.Championships[i];
                    for (int j=0; j<championship.Events.Length; j++)
                    {
                        if (championship.Events[j].Id == eventId)
                            return championship.Events[j];
                    }
                }
            }

            return null;   // TODO error handling?
        }

        // Given a championship ID, event ID, and a file path:
        // Fetch all the stage data for the event
        // convert the stage data to CSV
        // save the data to disk
        public async Task SaveStageDataToCsv(string championshipId, string eventId, string filePath)
        {
            if (clubInfoCache == null)
                return;   // TODO error handling?


            Event eventToOutput = null;
            for (int i = 0; i < clubInfoCache.RecentResults.Championships.Length; i++)
            {
                if (clubInfoCache.Championships[i].Id == championshipId)
                {
                    var championship = clubInfoCache.RecentResults.Championships[i];
                    for (int j = 0; j < championship.Events.Length; j++)
                    {
                        if (championship.Events[j].ChallengeId == eventId)
                        {
                            eventToOutput = championship.Events[j];
                            break;
                        }
                    }
                }
            }

            // if we have found our event, get the stages from it
            if (eventToOutput == null)
                return;


            var responses = new List<object>();
            for (int i = 0; i < eventToOutput.Stages.Length; i++)
            {
                responses.Add(racenetApi.GetStageResults(eventToOutput.ChallengeId, eventToOutput.Id, i.ToString()));
            }
        }
    }
}
