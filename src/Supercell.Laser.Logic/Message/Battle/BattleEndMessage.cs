namespace Supercell.Laser.Logic.Message.Battle
{
    using Supercell.Laser.Logic.Battle.Structures;
    using Supercell.Laser.Logic.Helper;
    using Supercell.Laser.Logic.Home.Quest;

    public class BattleEndMessage : GameMessage
    {
        public BattleEndMessage() : base()
        {
            ProgressiveQuests = new List<Quest>();
        }

        public int Result;
        public int TokensReward;
        public int ExpReward;
        public int TrophiesReward;
        public List<BattlePlayer> Players;
        public List<Quest> ProgressiveQuests;
        public BattlePlayer OwnPlayer;
        public bool StarToken;

        public int GameMode;

        public bool IsPvP;
        public int Counts;


        public override void Encode()
        {
            Stream.WriteVInt(GameMode); // game mode
            Stream.WriteVInt(Result);

            Stream.WriteVInt(TokensReward); // tokens reward
            Stream.WriteVInt(TrophiesReward); // trophies reward
            Stream.WriteVInt(1488); // power play reward
            Stream.WriteVInt(0); // double tokens reward
            Stream.WriteVInt(0); // Double Token Event
            Stream.WriteVInt(0); // token doublers left
            Stream.WriteVInt(0); // Robo Rumble/Boss Fight Level Passed
            Stream.WriteVInt(1337); // power play epic score
            Stream.WriteVInt(1); // Championship Level Passed
            Stream.WriteVInt(0); // Challenge Reward Type (0 = Star Points, 1 = Star Tokens)
            Stream.WriteVInt(0); // Challenge Reward Ammount
            Stream.WriteVInt(0); // Championship Losses Left
            Stream.WriteVInt(0); // Championship Maximun Losses
            Stream.WriteVInt(0); // Coin Shower Event
            Stream.WriteVInt(1347); // underdog trophies

            Stream.WriteBoolean(StarToken);
            Stream.WriteBoolean(false); // no experience
            Stream.WriteBoolean(false); // no tokens left
            Stream.WriteBoolean(false); // championship game
            Stream.WriteBoolean(IsPvP); // is PvP
            Stream.WriteBoolean(false); // training game
            Stream.WriteBoolean(false); // is power play

            Stream.WriteVInt(-64); // Championship Challenge Type
            Stream.WriteBoolean(true); // Championship Cleared

            Console.WriteLine(Players.Count);
            Counts = Players.Count;
            Stream.WriteVInt(Counts);
            foreach (BattlePlayer player in Players)
            {
                Stream.WriteBoolean(player.AccountId == OwnPlayer.AccountId); // is own player
                Stream.WriteBoolean(player.TeamIndex != OwnPlayer.TeamIndex); // is enemy
                Stream.WriteBoolean(false); // Star player

                ByteStreamHelper.WriteDataReference(Stream, player.CharacterId);
                Stream.WriteVInt(0); // skin

                Stream.WriteVInt(player.Trophies); // trophies
                Stream.WriteVInt(0); // PowerPlay Points
                Stream.WriteVInt(player.HeroPowerLevel + 1); // power level
                bool isOwn = player.AccountId == OwnPlayer.AccountId;
                Stream.WriteBoolean(isOwn);
                if (isOwn)
                {
                    Stream.WriteLong(player.AccountId);
                }

                player.DisplayData.Encode(Stream);
            }

            Stream.WriteVInt(2);
            // Experience Array
            Stream.WriteVInt(0); // Normal Experience ID
            Stream.WriteVInt(ExpReward); // Normal Experience Gained
            Stream.WriteVInt(8); // Star Player Experience ID
            Stream.WriteVInt(0);// Star Player Experience Gained

            Stream.WriteVInt(1);
            {
                Stream.WriteVInt(39);
                Stream.WriteVInt(20);
            }

            Stream.WriteVInt(2);
            {
                Stream.WriteVInt(1);
                Stream.WriteVInt(OwnPlayer.Trophies); // Trophies
                Stream.WriteVInt(OwnPlayer.HighestTrophies); // Highest Trophies

                Stream.WriteVInt(5);
                Stream.WriteVInt(100);
                Stream.WriteVInt(100);
            }

            ByteStreamHelper.WriteDataReference(Stream, 28000000);

            Stream.WriteBoolean(false); // is play again

            if (Stream.WriteBoolean(ProgressiveQuests.Count > 0))
            {
                Stream.WriteVInt(ProgressiveQuests.Count);
                foreach (Quest quest in ProgressiveQuests)
                {
                    quest.Encode(Stream);
                }
            }
        }

        public override int GetMessageType()
        {
            return 23456;
        }

        public override int GetServiceNodeType()
        {
            return 27;
        }
    }
}