namespace Supercell.Laser.Logic.Message.Home
{
    //using Supercell.Laser.Server.Networking.Session;
    public class LobbyInfoMessage : GameMessage
    {
        public int PlayersOnline;
        public string Version;
        public int Ping;
        public string Pinged;
        public override void Encode()
        {
            if (Ping >= 0 && Ping <= 49)
            {
                Pinged = " ▂▄▅▆ " + "(" + Ping + "ms)";
            }
            if (Ping >= 50 && Ping <= 99)
            {
                Pinged = " ▂▄▅   " + "(" + Ping + "ms)";
            }
            if (Ping >= 100 && Ping <= 199)
            {
                Pinged = " ▂▄   " + "(" + Ping + "ms)";
            }
            if (Ping >= 200 && Ping <= 299)
            {
                Pinged = " ▂    " + "(" + Ping + "ms)";
            }
            if (Ping >= 300)
            {
                Pinged = " ▁    " + "(" + Ping + "ms)";
            }
            Stream.WriteVInt(PlayersOnline);
            Stream.WriteString("Supercell Laser v27.5хх" + "\nt.me/bazzarservers\nThis content is not affiliated with, endorsed, sponsored, or specifically approved by Supercell and Supercell is not responsible for it.\nFor more information see: Supercell's Fan Content Policy and also Supercell's Term of Service\n<cff0012>Я<cff0024> <cff0036>Х<cff0048>О<cff005b>Ч<cff006d>У<cff007f> <cff0091>Т<cff00a3>Р<cff00b6>А<cff00c8>Х<cfe00da>Н<cff00ec>У<cff00fe>Т<cff00ff>Ь<cec00ff> <cda00ff>С<cc800ff>Е<cb600ff>Р<ca300ff>О<c9100ff>Г<c7f00ff>О<c6d00ff> <c5b00ff>В<c4800ff> <c3600ff>Ж<c2400fe>О</c>\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n");

            Stream.WriteVInt(0); // Event count
        }
        public override int GetMessageType()
        {
            return 23457;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
