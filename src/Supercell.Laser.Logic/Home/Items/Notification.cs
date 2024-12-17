using Supercell.Laser.Titan.DataStream;

namespace Supercell.Laser.Logic.Home.Items
{
    public class Notification
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public bool IsViewed { get; set; }
        public int TimePassed { get; set; }
        public string MessageEntry { get; set; }
        public string PrimaryMessageEntry { get; set; }
        public string SecondaryMessageEntry { get; set; }
        public string ButtonMessageEntry { get; set; }
        public string FileLocation { get; set; }
        public string FileSha { get; set; }
        public string ExtLint { get; set; }
        public List<int> HeroesIds { get; set; }
        public List<int> HeroesTrophies { get; set; }
        public List<int> HeroesTrophiesReseted { get; set; }
        public List<int> StarpointsAwarded { get; set; }
        public int DonationCount;

        public int SkinID;

        public int BrawlerID;

        public int ResourceID;

        public int ResourceCount;

        public int RevokePersonHighID;
        public int RevokePersonLowID;
        public int RevokeCount;
        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(Id);
            stream.WriteInt(Index);
            stream.WriteBoolean(IsViewed);
            stream.WriteInt(TimePassed);
            stream.WriteString(MessageEntry);
            switch (Id)
            {
                case 83:
                    stream.WriteInt(0);
                    stream.WriteStringReference(PrimaryMessageEntry);
                    stream.WriteInt(0);
                    stream.WriteStringReference(SecondaryMessageEntry);
                    stream.WriteInt(0);
                    stream.WriteStringReference(ButtonMessageEntry);
                    stream.WriteStringReference(FileLocation);
                    stream.WriteStringReference(FileSha);
                    stream.WriteStringReference(ExtLint);
                    break;
                case 79:
                    stream.WriteVInt(HeroesIds.Count);
                    for (int i = 0; i < HeroesIds.Count; i++)
                    {
                        stream.WriteVInt(HeroesIds[i]);
                        stream.WriteVInt(HeroesTrophies[i]);
                        stream.WriteVInt(HeroesTrophiesReseted[i]);
                        stream.WriteVInt(StarpointsAwarded[i]);
                    }
                    break;
                case 89:
                    stream.WriteVInt(DonationCount);
                    break;
                case 94:
                    stream.WriteVInt(29000000 + SkinID);
                    break;
                case 93:
                    stream.WriteVInt(0); // mb lvl
                    stream.WriteVInt(16000000 + BrawlerID);
                    break;
                case 90:
                    stream.WriteVInt(0);
                    stream.WriteVInt(5000000 + ResourceID);
                    stream.WriteVInt(ResourceCount);
                    break;
                case 85:
                    stream.WriteVInt(0); // type revork
                    stream.WriteVInt(RevokeCount);
                    stream.WriteInt(RevokePersonHighID);
                    stream.WriteInt(RevokePersonLowID);
                    stream.WriteVInt(0);
                    stream.WriteString("");
                    break;
                case 73: // unlock brawl pass message
                    stream.WriteVInt(0);
                    break;
                case 75: // challange спс сисечки появились :3
                    stream.WriteVInt(0);
                    break;
                default:
                    stream.WriteVInt(0); // а нехуй пенисом по ебалу водить
                    break;
            }

        }
    }
}
