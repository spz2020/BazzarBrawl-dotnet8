namespace Supercell.Laser.Logic.Message.Team
{
    using Supercell.Laser.Logic.Data;
    public class TeamSetLocationMessage : GameMessage
    {
        public int LocationId;
        public int Slot;
        public override void Decode()
        {
            base.Decode();
            Slot = Stream.ReadVInt();
            LocationId = Stream.ReadVInt();
            DataTables.Get(DataType.Location).GetDataByGlobalId<LocationData>(LocationId);
        }

        public override int GetMessageType()
        {
            return 14363;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
