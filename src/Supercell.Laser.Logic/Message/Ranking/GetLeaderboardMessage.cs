namespace Supercell.Laser.Logic.Message.Ranking
{
    public class GetLeaderboardMessage : GameMessage
    {
        public bool IsRegional { get; set; }
        public int LeaderboardType { get; set; }
        public int BrawlerID;

        public override void Decode()
        {
            base.Decode();

            IsRegional = Stream.ReadBoolean();
            LeaderboardType = Stream.ReadVInt();
            Stream.ReadVInt();
            BrawlerID = Stream.ReadVInt();
            Console.WriteLine("IsRegional " + IsRegional + " LeaderBoardType " + LeaderboardType + " BrawlerID " + BrawlerID);
        }

        public override int GetMessageType()
        {
            return 14403;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
