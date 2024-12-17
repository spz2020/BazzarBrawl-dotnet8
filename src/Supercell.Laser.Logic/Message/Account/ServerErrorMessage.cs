namespace Supercell.Laser.Logic.Message.Account
{
    public class ServerErrorMessage : GameMessage
    {
        private readonly int _reason;

        public ServerErrorMessage(int reason)
        {
            _reason = reason;
        }

        public override void Encode()
        {
            base.Encode();

            Stream.WriteInt(_reason);
        }

        public override int GetMessageType()
        {
            return 24115;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}