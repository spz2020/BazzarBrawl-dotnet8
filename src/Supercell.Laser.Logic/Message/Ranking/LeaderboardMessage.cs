namespace Supercell.Laser.Logic.Message.Ranking
{
    using Supercell.Laser.Logic.Avatar;
    using Supercell.Laser.Logic.Club;
    using Supercell.Laser.Logic.Helper;
    using Supercell.Laser.Logic.Home;

    public class LeaderboardMessage : GameMessage
    {
        public int LeaderboardType { get; set; }
        public int Count;

        public List<KeyValuePair<ClientHome, ClientAvatar>> Avatars;
        public List<Alliance> AllianceList;
        public long OwnAvatarId;
        public string Region { get; set; }
        public int BrawlerTrophies;
        public int BrawlerID;

        public LeaderboardMessage() : base()
        {
            Avatars = new List<KeyValuePair<ClientHome, ClientAvatar>>();
            AllianceList = new List<Alliance>();
            LeaderboardType = 1;
        }

        public override void Encode()
        {
            int playerIndex = 0;

            Stream.WriteVInt(LeaderboardType);
            Stream.WriteVInt(0);
            if (LeaderboardType == 0)
            {
                Stream.WriteVInt(16);
                Stream.WriteVInt(BrawlerID);
            }
            else
            {
                Stream.WriteVInt(0);
            }
            Stream.WriteString(Region); // Region

            if (LeaderboardType == 1 || LeaderboardType == 3)
            {
                foreach (var pair in Avatars)
                {
                    var avatar = pair.Value;
                    if (avatar.Trophies == 0)
                    {
                        Count += 0;
                    }
                    else
                    {
                        Count += 1;
                    }
                }
                Stream.WriteVInt(Count);
                foreach (var pair in Avatars)
                {
                    var avatar = pair.Value;
                    if (avatar.Trophies <= 0)
                    {

                        Stream.WriteInt(0);
                        Stream.WriteInt(6974);

                        Stream.WriteVInt(1);
                        Stream.WriteVInt(0);

                        Stream.WriteVInt(1);

                        Stream.WriteString(null); // Club name

                        Stream.WriteString("NoName");
                        Stream.WriteVInt(100);
                        Stream.WriteVInt(28000000);
                        Stream.WriteVInt(43000000);
                        Stream.WriteVInt(0);
                    }
                    else
                    {
                        var home = pair.Key;
                        if (avatar.AccountId == OwnAvatarId)
                        {
                            playerIndex = Avatars.IndexOf(pair) + 1;
                        }

                        Stream.WriteVLong(avatar.AccountId);

                        Stream.WriteVInt(1);
                        Stream.WriteVInt(avatar.Trophies);

                        Stream.WriteVInt(1);

                        Stream.WriteString(null); // Club name

                        Stream.WriteString(avatar.Name ?? "NoName");
                        Stream.WriteVInt(100);
                        Stream.WriteVInt(home.ThumbnailId);
                        Stream.WriteVInt(43000000 + home.Namecolor);
                        Stream.WriteVInt(0);
                    }
                }
            }
            else if (LeaderboardType == 2)
            {
                Stream.WriteVInt(AllianceList.Count);
                foreach (var alliance in AllianceList)
                {
                    Stream.WriteVLong(alliance.Id);

                    Stream.WriteVInt(1);
                    Stream.WriteVInt(alliance.Trophies);

                    Stream.WriteVInt(2);

                    Stream.WriteString(alliance.Name);
                    Stream.WriteVInt(alliance.Members.Count);
                    ByteStreamHelper.WriteDataReference(Stream, alliance.AllianceBadgeId);
                }
            }

            if (LeaderboardType == 0)
            {
                foreach (var pair in Avatars)
                {
                    var avatar = pair.Value;
                    if (BrawlerTrophies == 0)
                    {
                        Count += 0;
                    }
                    else
                    {
                        Count += 1;
                    }
                }
                Stream.WriteVInt(Count);
                foreach (var pair in Avatars)
                {
                    var avatar = pair.Value;
                    if (BrawlerTrophies <= 0)
                    {

                        Stream.WriteInt(0);
                        Stream.WriteInt(6974);

                        Stream.WriteVInt(1);
                        Stream.WriteVInt(0);

                        Stream.WriteVInt(1);

                        Stream.WriteString(null); // Club name

                        Stream.WriteString("NoName");
                        Stream.WriteVInt(100);
                        Stream.WriteVInt(28000000);
                        Stream.WriteVInt(43000000);
                        Stream.WriteVInt(0);
                    }
                    else
                    {
                        var home = pair.Key;
                        if (avatar.AccountId == OwnAvatarId)
                        {
                            playerIndex = Avatars.IndexOf(pair) + 1;
                        }

                        Stream.WriteVLong(avatar.AccountId);

                        Stream.WriteVInt(1);
                        Stream.WriteVInt(BrawlerTrophies);

                        Stream.WriteVInt(1);

                        Stream.WriteString(null); // Club name

                        Stream.WriteString(avatar.Name ?? "NoName");
                        Stream.WriteVInt(100);
                        Stream.WriteVInt(home.ThumbnailId);
                        Stream.WriteVInt(43000000 + home.Namecolor);
                        Stream.WriteVInt(0);
                    }
                }
            }
            else
            {
                Stream.WriteVInt(0);
            }

            Stream.WriteVInt(0);
            Stream.WriteVInt(playerIndex);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            if (LeaderboardType == 1)
            {
                Stream.WriteString("RU");
            }
            else
            {
                Stream.WriteString("RU");
            }
        }

        public override int GetMessageType()
        {
            return 24403;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
