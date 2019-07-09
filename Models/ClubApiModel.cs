namespace DR2Rallymaster.Models
{
    // This represents the JSON result returned from the "Club" endpoint
    // Contains information about the club as well as info about the user's permissions
    // example url:
    // https://dirtrally2.com/api/Club/183582
    // no params

    public class ClubApiModel
    {
        public string Result { get; set; }
        public Club Club { get; set; }
        public int PendingInvites { get; set; }
        public string Role { get; set; }
        public Permissions Permissions { get; set; }
    }

    public class Club
    {
        public bool HasFutureChampionship { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int MemberCount { get; set; }
        public int BackgroundImageId { get; set; }
        public string ClubAccessType { get; set; }
        public bool IsMember { get; set; }
        public bool HasAskedToJoin { get; set; }
        public bool HasBeenInvitedToJoin { get; set; }
        public bool HasActiveChampionship { get; set; }
        public MyChampionshipProgress MyChampionshipProgress { get; set; }
    }

    public class MyChampionshipProgress
    {
        public int EventCount { get; set; }
        public int FinishedCount { get; set; }
        public int CompletedCount { get; set; }
        public object[] CompletedEventIndexes { get; set; }
    }

    public class Permissions
    {
        public bool CanEditClubSettings { get; set; }
        public bool CanDisbandClub { get; set; }
        public bool CanCancelChampionship { get; set; }
        public bool CanAcceptOrDenyJoinRequest { get; set; }
        public bool CanCreateChampionship { get; set; }
        public bool CanPromoteToAdmin { get; set; }
        public bool CanPromoteToOwner { get; set; }
        public bool CanDemoteToAdmin { get; set; }
        public bool CanDemoteToPlayer { get; set; }
        public bool CanKickMember { get; set; }
    }

}
