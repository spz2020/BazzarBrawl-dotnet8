namespace Supercell.Laser.Logic.Command.Home
{
    using Supercell.Laser.Logic.Home;
    using Supercell.Laser.Titan.DataStream;

    public class LogicSelectSkinCommand : Command
    {
        public int CharacterId;
        public int SkinId;
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            SkinId = stream.ReadVInt();
            CharacterId = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 506;
        }
    }
}
