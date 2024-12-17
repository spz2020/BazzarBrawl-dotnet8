namespace Supercell.Laser.Logic.Message.Home
{
    public class GetBattleLogMessage : GameMessage
    {
        public int Type;

        public GetBattleLogMessage() : base()
        {
            ;
        }

        public override void Decode()
        {
            //Type = Stream.ReadInt();
        }

        public override int GetMessageType()
        {
            return 14114;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
