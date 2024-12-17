namespace Supercell.Laser.Logic.Message.Battle
{
    public class PlayAgainStatusMessage : GameMessage
    {
        public int AcceptedPlayers = 1;
        public int Type;
        public int Timer = 11111111;

        public override void Decode()
        {
            Type = Stream.ReadInt();
        }
        public override void Encode()
        {
            Console.WriteLine("запросил play again status");
            Stream.WriteInt(Type);
            Console.WriteLine(Type);
            Stream.WriteVInt(1);
            Stream.WriteInt(0);
            Stream.WriteInt(0);

            Stream.WriteVInt(Timer);
            Stream.WriteInt(0);
            Stream.WriteInt(0);

            Stream.WriteInt(1);

            Stream.WriteInt(1);
        }

        public override int GetMessageType()
        {
            return 24777;
        }

        public override int GetServiceNodeType()
        {
            return 4;
        }
    }
}