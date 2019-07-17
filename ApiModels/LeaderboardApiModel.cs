namespace DR2Rallymaster.ApiModels
{
    // Represents the JSON result returned from the "Leaderboard" endpoint
    // Contains all the drivers and times for the event
    // Example URL:
    // https://dirtrally2.com/api/Leaderboard
    //
    // Example params (JSON):
    //{"challengeId":"11453","selectedEventId":0,"stageId":"6","page":1,"pageSize":100,"orderByTotalTime":true,"platformFilter":"None","playerFilter":"Everyone","filterByAssists":"Unspecified","filterByWheel":"Unspecified","nationalityFilter":"None","eventId":"11615"}
    //
    // Where
    // challengeId = comes from the recent results
    // eventId = comes form the recent results
    // stageId = zero based

    public class LeaderboardApiModel
    {
        public int PageRequested { get; set; }
        public int PageSize { get; set; }
        public string PageCount { get; set; }
        public Entry[] Entries { get; set; }
    }

    public class Entry
    {
        public int Rank { get; set; }
        public string Name { get; set; }
        public bool IsVIP { get; set; }
        public bool IsFounder { get; set; }
        public bool IsPlayer { get; set; }
        public bool IsDnfEntry { get; set; }
        public string VehicleName { get; set; }
        public string StageTime { get; set; }
        public string StageDiff { get; set; }
        public string TotalTime { get; set; }
        public string TotalDiff { get; set; }
    }

}
