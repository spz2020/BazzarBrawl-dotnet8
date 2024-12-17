namespace Supercell.Laser.Logic.Message.Home
{
    using Supercell.Laser.Logic.Helper;

    public class BattleLogMessage : GameMessage
    {
        public DateTime BattleLogCreateTimer;


        public BattleLogMessage() : base()
        {
            ;
        }

        public override void Encode()
        {
            Stream.WriteBoolean(true);
            Stream.WriteVInt(1); // Count

            Stream.WriteVInt(0);
            Stream.WriteVInt(3); // timer create
            Stream.WriteVInt(1); // battle log type
            Stream.WriteVInt(1337); // result count
            Stream.WriteVInt(6); // time
            Stream.WriteVInt(1); // type [1 - normal ; 3 - survived 
            // по либе тут должен быть bool но ладно
            ByteStreamHelper.WriteDataReference(Stream, 15000000 + 7); // map id
            Stream.WriteVInt(0); // result [0 - win ; 1 - defeat ; 3 - draw
            Stream.WriteVInt(0);

            Stream.WriteVInt(1);
            Stream.WriteBoolean(false);

            Stream.WriteVInt(1);

            Stream.WriteVInt(0);
            Stream.WriteBoolean(false);
            Stream.WriteVInt(0);
            Stream.WriteBoolean(false);
            Stream.WriteVInt(6); // idk


            Stream.WriteVInt(0);
            Stream.WriteLong(1);
            Stream.WriteVInt(0);
            Stream.WriteBoolean(false);
            ByteStreamHelper.WriteDataReference(Stream, 16000000); // brawler id
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            //Stream.WriteVInt(0);
            Stream.WriteVInt(0); // brawler lvl
            Stream.WriteString("да я че ебу что сюда писать"); // player name
            Stream.WriteVInt(28000000); // player name colour
            Stream.WriteVInt(43000000); // player thumbnail

            Stream.WriteVInt(0);
            Stream.WriteLong(1);
            Stream.WriteVInt(0);
            Stream.WriteBoolean(false);
            ByteStreamHelper.WriteDataReference(Stream, 16000000); // brawler id
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            //Stream.WriteVInt(0);
            Stream.WriteVInt(0); // brawler lvl
            Stream.WriteString("да я че ебу что сюда писать"); // player name
            Stream.WriteVInt(28000000); // player name colour
            Stream.WriteVInt(43000000); // player thumbnail

            Stream.WriteVInt(0);
            Stream.WriteLong(1);
            Stream.WriteVInt(0);
            Stream.WriteBoolean(false);
            ByteStreamHelper.WriteDataReference(Stream, 16000000); // brawler id
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            //Stream.WriteVInt(0);
            Stream.WriteVInt(0); // brawler lvl
            Stream.WriteString("да я че ебу что сюда писать"); // player name
            Stream.WriteVInt(28000000); // player name colour
            Stream.WriteVInt(43000000); // player thumbnail

            Stream.WriteVInt(0);
            Stream.WriteLong(1);
            Stream.WriteVInt(0);
            Stream.WriteBoolean(false);
            ByteStreamHelper.WriteDataReference(Stream, 16000000); // brawler id
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            //Stream.WriteVInt(0);
            Stream.WriteVInt(0); // brawler lvl
            Stream.WriteString("да я че ебу что сюда писать"); // player name
            Stream.WriteVInt(28000000); // player name colour
            Stream.WriteVInt(43000000); // player thumbnail

            Stream.WriteVInt(0);
            Stream.WriteLong(1);
            Stream.WriteVInt(0);
            Stream.WriteBoolean(false);
            ByteStreamHelper.WriteDataReference(Stream, 16000000); // brawler id
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            //Stream.WriteVInt(0);
            Stream.WriteVInt(0); // brawler lvl
            Stream.WriteString("да я че ебу что сюда писать"); // player name
            Stream.WriteVInt(28000000); // player name colour
            Stream.WriteVInt(43000000); // player thumbnail

        }

        public override int GetMessageType()
        {
            return 23458;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}


