namespace Supercell.Laser.Logic.Message.Security
{
    public class ClientHelloMessage : GameMessage
    {
        public int Protocol;
        public int KeyVersion { get; set; }
        public int MajorVersion;
        public int ClientSeed { get; set; }

        public override int GetMessageType()
        {
            return 10100;
        }

        public override void Decode()
        {
            Protocol = Stream.ReadInt();
            KeyVersion = Stream.ReadInt();
            MajorVersion = Stream.ReadInt();
            ClientSeed = Stream.ReadInt();
            Console.WriteLine("[ClientHelloMessage] Protocol: " + Protocol + " KeyVersion: " + KeyVersion + " MajorVersion " + MajorVersion + " ClientSeed " + ClientSeed);
        }

        public override int GetServiceNodeType()
        {
            return 1;
        }
    }
}
