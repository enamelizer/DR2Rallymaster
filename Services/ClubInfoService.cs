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
        private readonly RacenetApiUtilities racenetApi = null;

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

                // first get club data
                var racenetClubResult = await racenetApi.GetClubInfo(clubId);
                if (racenetClubResult.Item1 != HttpStatusCode.OK || String.IsNullOrEmpty(racenetClubResult.Item2))
                    return null; // TODO error handling

                var apiClubModel = JsonConvert.DeserializeObject<ClubApiModel>(racenetClubResult.Item2);
                if (apiClubModel != null)
                {
                    returnModel.ClubName = apiClubModel.Club.Name;
                    returnModel.ClubDesc = apiClubModel.Club.Description;
                }

                // now get championship data
                var racenetChampionshipsResult = await racenetApi.GetChampionshipInfo(clubId);
                if (racenetChampionshipsResult.Item1 != HttpStatusCode.OK || String.IsNullOrEmpty(racenetChampionshipsResult.Item2))
                    return null; // TODO error handling

                var apiChampionshipsModel = JsonConvert.DeserializeObject<ChampionshipMetaData[]>(racenetChampionshipsResult.Item2);
                if (racenetChampionshipsResult != null)
                {
                    returnModel.Championships = apiChampionshipsModel;
                }

                return returnModel;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
