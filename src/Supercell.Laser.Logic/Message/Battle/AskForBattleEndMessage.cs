namespace Supercell.Laser.Logic.Message.Battle
{
    using Supercell.Laser.Logic.Home.Structures;

    public class AskForBattleEndMessage : GameMessage
    {
        public List<Hero> Heroes;
        public int battleResult;
        public int mathResult;
        public int mapID;
        public int players;
        public int BrawlerID;
        public int SkinID;
        public int Team;
        public string Name;

        // Trophies
        public int BrawlerTrophies;
        public int BrawlerHighestTrophies;

        public override void Decode()
        {
            battleResult = Stream.ReadVInt(); // GameMode
            Stream.ReadVInt();
            mathResult = Stream.ReadVInt(); // Result
            Stream.ReadVInt(); // LocationID
            mapID = Stream.ReadVInt(); // mapID
            players = Stream.ReadVInt(); // Battle End Players
            Stream.ReadVInt(); // CsvID Brawler
            BrawlerID = Stream.ReadVInt(); // BrawlerID
            Stream.ReadVInt(); // CsvID Skin
            SkinID = Stream.ReadVInt(); // SkinID
            Team = Stream.ReadVInt(); // Team
            Stream.ReadVInt(); // ?

            Console.WriteLine("AskForBattleEnd sent!");
            Console.WriteLine("battleResult " + battleResult + " mathResult " + mathResult + " mapID " + mapID + " Name " + Name + " BrawlerID " + BrawlerID + " SkinID " + SkinID);
            /*foreach (Hero hero in Heroes)
            {
                if (hero.CharacterId == 16000000 + BrawlerID)
                {
                    BrawlerTrophies = hero.Trophies;
                    BrawlerHighestTrophies = hero.HighestTrophies;

                    BrawlerTrophies = hero.Trophies;
                    BrawlerHighestTrophies = hero.HighestTrophies;
                }
            }*/
        }

        public override int GetMessageType()
        {
            return 14110;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}