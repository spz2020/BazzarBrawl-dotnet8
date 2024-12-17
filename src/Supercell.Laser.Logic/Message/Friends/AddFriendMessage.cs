namespace Supercell.Laser.Logic.Message.Friends
{
    public class AddFriendMessage : FriendAvatarBaseMessage
    {
        public int Reason;
        public int ReasonDetails;

        public override void Decode()
        {
            base.Decode();

            Reason = Stream.ReadInt();
            ReasonDetails = Stream.ReadInt();

            Console.WriteLine("Reason " + Reason + " ReasonDetails " + ReasonDetails);
        }

        public override int GetMessageType()
        {
            return 10502;
        }

        public override int GetServiceNodeType()
        {
            return 3;
        }
    }
}
