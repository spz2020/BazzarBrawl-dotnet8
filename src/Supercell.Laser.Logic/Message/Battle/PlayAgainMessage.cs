namespace Supercell.Laser.Logic.Message.Battle
{
    public class PlayAgainMessage : GameMessage
    {

        public override void Encode()
        {
            Console.WriteLine("запросил play again status");
            Stream.WriteBoolean(false);
        }

        public override int GetMessageType()
        {
            return 14177;
        }

        public override int GetServiceNodeType()
        {
            return 4;
        }
    }
}