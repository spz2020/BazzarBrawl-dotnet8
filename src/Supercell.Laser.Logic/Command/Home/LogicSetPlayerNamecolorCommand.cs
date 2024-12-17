namespace Supercell.Laser.Logic.Command.Home
{
    using Supercell.Laser.Logic.Home;
    using Supercell.Laser.Titan.DataStream;

    public class LogicSetPlayerNamecolorCommand : Command
    {
        public int Namecolor;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            stream.ReadVInt();
            Namecolor = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            homeMode.Home.Namecolor = Namecolor;
            return 0;
        }

        public override int GetCommandType()
        {
            return 527;
        }
    }
}