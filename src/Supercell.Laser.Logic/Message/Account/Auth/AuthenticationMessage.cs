namespace Supercell.Laser.Logic.Message.Account.Auth
{
    public class AuthenticationMessage : GameMessage
    {
        public AuthenticationMessage() : base()
        {
            AccountId = 0;
        }

        public long AccountId;
        public string PassToken;

        public int ClientMajor;
        public int ClientMinor;
        public int ClientBuild;

        public string OsVersion;
        public bool isAndroid;

        public override void Decode()
        {
            AccountId = Stream.ReadLong();
            PassToken = Stream.ReadString();

            ClientMajor = Stream.ReadInt();
            ClientMinor = Stream.ReadInt();
            ClientBuild = Stream.ReadInt();

            Stream.ReadString();
            Stream.ReadString();
            Stream.ReadVInt();
            Stream.ReadVInt();
            Stream.ReadString();

            OsVersion = Stream.ReadString();
            isAndroid = Stream.ReadBoolean();

            Console.WriteLine("AccountID: " + AccountId + " PassToken" + PassToken + " ClientMajor " + ClientMajor + " ClientMinor " + ClientMinor + " ClientBuild " + ClientBuild + " OsVersion " + OsVersion + " isAndroid " + isAndroid);
        }

        public override int GetMessageType()
        {
            return 10101;
        }

        public override int GetServiceNodeType()
        {
            return 1;
        }
    }
}
