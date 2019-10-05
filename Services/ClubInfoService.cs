using DR2Rallymaster.ApiModels;
using DR2Rallymaster.DataModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
            var apiInitialized = racenetApi.GetInitialState();      // TODO: if this fails, we fail (dramatic music)
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

            var responses = new List<Tuple<HttpStatusCode, string>>();
            for (int i = 0; i < eventToOutput.Stages.Length; i++)
            {
                responses.Add(await racenetApi.GetStageResults(eventToOutput.ChallengeId, eventToOutput.Id, i.ToString()));
            }

            var rallyData = new Rally();

            // push this to the number cruncher
            foreach(var response in responses)
            {
                if (response.Item1 != HttpStatusCode.OK || String.IsNullOrWhiteSpace(response.Item2))
                    return; // TODO error handling

                var apiData = JsonConvert.DeserializeObject<LeaderboardApiModel>(response.Item2);
                if (apiData == null)
                    return; // TODO error handling

                var stageData = new Stage();

                foreach (var driverEntry in apiData.Entries)
                    stageData.AddDriver(new DriverTime(driverEntry.Rank, driverEntry.Name, driverEntry.VehicleName, driverEntry.TotalTime, driverEntry.TotalDiff));

                rallyData.AddStage(stageData);
            }

            // now crunch the numbers
            // TODO we should no longer need to do this step since stage times already exist, they should be added to the DriverTime object instead
            rallyData.CalculateTimes();

            // get the CSV formatted output and write it to file
            // TODO this should probably be somewhere else for encapsulation and separation of concerns
            var outputString = GetCsvOverallTimes(rallyData);
            outputString += Environment.NewLine;
            outputString += GetCsvStageTimes(rallyData);
            outputString += Environment.NewLine;
            outputString += GetCsvChartOutput(rallyData);

            File.WriteAllText(filePath, outputString, Encoding.UTF8);
        }

        private string GetCsvOverallTimes(Rally rallyData)
        {
            var outputSB = new StringBuilder();

            int stageCount = 1;
            foreach (Stage stage in rallyData)
            {
                outputSB.AppendLine("SS" + stageCount);
                outputSB.AppendLine("Overall");
                outputSB.AppendLine("Pos, Pos Chng, Name, Vehicle, Time, Diff 1st, Diff Prev");

                List<KeyValuePair<string, DriverTime>> sortedStageData = stage.DriverTimes.ToList();
                sortedStageData.Sort((x, y) =>
                {
                    if (x.Value != null && y.Value == null)
                        return -1;
                    else if (x.Value == null && y.Value != null)
                        return 1;
                    else if (x.Value == null && y.Value == null)
                        return 0;
                    else
                        return x.Value.CalculatedOverallTime.CompareTo(y.Value.CalculatedOverallTime);
                });

                foreach (KeyValuePair<string, DriverTime> driverTimeKvp in sortedStageData)
                {
                    var driverName = driverTimeKvp.Key;
                    var driverTime = driverTimeKvp.Value;

                    if (driverTime != null)
                    {
                        var formatString = @"hh\:mm\:ss\.fff";
                        var line = driverTime.OverallPosition + "," +
                                   driverTime.CalculatedPositionChange + "," +
                                   driverTime.DriverName + "," +
                                   driverTime.Vehicle + "," +
                                   driverTime.CalculatedOverallTime.ToString(formatString) + "," +
                                   driverTime.CalculatedOverallDiffFirst.ToString(formatString) + "," +
                                   driverTime.CalculatedOverallDiffPrevious.ToString(formatString);

                        outputSB.AppendLine(line);
                    }
                    else
                    {
                        outputSB.AppendLine(",,," + driverName + ",,DNF");
                    }
                }

                outputSB.AppendLine("");
                stageCount++;
            }

            return outputSB.ToString();
        }

        private string GetCsvStageTimes(Rally rallyData)
        {
            var outputSB = new StringBuilder();

            int stageCount = 1;

            foreach (Stage stage in rallyData)
            {
                outputSB.AppendLine("SS" + stageCount);
                outputSB.AppendLine("Stage");
                outputSB.AppendLine("Pos, Name, Vehicle, Time, Diff 1st, Diff Prev");

                List<KeyValuePair<string, DriverTime>> sortedStageData = stage.DriverTimes.ToList();
                sortedStageData.Sort((x, y) =>
                {
                    if (x.Value != null && y.Value == null)
                        return -1;
                    else if (x.Value == null && y.Value != null)
                        return 1;
                    else if (x.Value == null && y.Value == null)
                        return 0;
                    else
                        return x.Value.CalculatedStageTime.CompareTo(y.Value.CalculatedStageTime);
                });

                foreach (KeyValuePair<string, DriverTime> driverTimeKvp in sortedStageData)
                {
                    var driverName = driverTimeKvp.Key;
                    var driverTime = driverTimeKvp.Value;

                    // if a driver did not complete the pervious stage due to alt+f4 or a crash, the next stage
                    // will have a zero time for the CalculatedStageTime, so enter DNF
                    if (driverTime == null || driverTime.CalculatedStageTime == TimeSpan.Zero)
                    {
                        outputSB.AppendLine(",," + driverName + ",,DNF");
                    }
                    else
                    {
                        var formatString = @"mm\:ss\.fff";
                        var line = driverTime.CaclulatedStagePosition + "," +
                                   driverTime.DriverName + "," +
                                   driverTime.Vehicle + "," +
                                   driverTime.CalculatedStageTime.ToString(formatString) + "," +
                                   driverTime.CalculatedStageDiffFirst.ToString(formatString) + "," +
                                   driverTime.CalculatedStageDiffPrevious.ToString(formatString);

                        outputSB.AppendLine(line);
                    }
                }

                outputSB.AppendLine("");
                stageCount++;
            }

            return outputSB.ToString();
        }

        private string GetCsvChartOutput(Rally rallyData)
        {
            var outputSB = new StringBuilder();
            var positionDict = new Dictionary<string, List<int>>();
            List<KeyValuePair<string, DriverTime>> sortedStageData = null;

            foreach (Stage stage in rallyData)
            {
                sortedStageData = stage.DriverTimes.ToList();
                sortedStageData.Sort((x, y) =>
                {
                    if (x.Value != null && y.Value == null)
                        return -1;
                    else if (x.Value == null && y.Value != null)
                        return 1;
                    else if (x.Value == null && y.Value == null)
                        return 0;
                    else
                        return x.Value.CalculatedOverallTime.CompareTo(y.Value.CalculatedOverallTime);
                });

                foreach (KeyValuePair<string, DriverTime> driverTimeKvp in sortedStageData)
                {
                    if (driverTimeKvp.Value == null)
                        continue;

                    var driverKey = driverTimeKvp.Key;
                    var position = driverTimeKvp.Value.OverallPosition;

                    if (!positionDict.ContainsKey(driverKey))
                        positionDict.Add(driverKey, new List<int>());

                    positionDict[driverKey].Add(position);
                }
            }

            // sortedStageData should contain the last stage, sorted by overall time
            if (sortedStageData == null)
                return null;

            foreach (KeyValuePair<string, DriverTime> driverTimeKvp in sortedStageData)
            {
                var driverKey = driverTimeKvp.Key;
                var positionList = positionDict[driverKey];

                string line = driverKey + "," + String.Join(",", positionList);

                outputSB.AppendLine(line);
            }

            return outputSB.ToString();
        }
    }
}
