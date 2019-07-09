using System;

namespace DR2Rallymaster.Models
{
    // This represents the JSON result returned for the "championships" endpoint
    // Contains information about current, past (and future?) championships
    // Example URL:
    // https://dirtrally2.com/api/Club/183582/championships
    // no params

    public class ChampionshipsApiModel
    {
        public ChampionshipMetaData[] ChampionshipMetaData { get; set; }
    }

    public class ChampionshipMetaData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public EventMetadata[] Events { get; set; }
    }

    public class EventMetadata
    {
        public string Id { get; set; }
        public string CountryId { get; set; }
        public string CountryName { get; set; }
        public string LocationId { get; set; }
        public string LocationName { get; set; }
        public string FirstStageRouteId { get; set; }
        public string FirstStageConditions { get; set; }
        public bool HasParticipated { get; set; }
        public Entrywindow EntryWindow { get; set; }
        public string EventStatus { get; set; }
        public string EventTime { get; set; }
    }

    public class Entrywindow
    {
        public DateTime Start { get; set; }
        public DateTime Open { get; set; }
        public DateTime Close { get; set; }
        public DateTime End { get; set; }
    }

}
