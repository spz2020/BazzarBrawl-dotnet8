namespace Supercell.Laser.Logic.Message.Account
{
    using Supercell.Laser.Logic.Message.Home;
    public class KeepAliveMessage : GameMessage
    {
        public override void Decode()
        {
            LobbyInfoMessage message = new LobbyInfoMessage();
        }
        public override int GetMessageType()
        {
            return 10108;
        }

        public override int GetServiceNodeType()
        {
            return 1;
        }
    }
}
