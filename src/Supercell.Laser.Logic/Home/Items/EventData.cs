namespace Supercell.Laser.Logic.Home.Items
{
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Helper;
    using Supercell.Laser.Titan.DataStream;

    public class EventData
    {
        public int Slot;
        public int LocationId;
        public DateTime EndTime;
        public LocationData Location => DataTables.Get(DataType.Location).GetDataByGlobalId<LocationData>(LocationId);

        public void Encode(ByteStream encoder)
        {
            if (Slot != 9)
            {
                encoder.WriteVInt(0);
                encoder.WriteVInt(Slot); // slot
                encoder.WriteVInt(0);
                encoder.WriteVInt((int)(EndTime - DateTime.Now).TotalSeconds);
                encoder.WriteVInt(10);

                ByteStreamHelper.WriteDataReference(encoder, Location);

                encoder.WriteVInt(2); // 0xacec7c
                encoder.WriteString(null); // 0xacecac
                encoder.WriteVInt(0); // 0xacecc0
                encoder.WriteVInt(0); // 0xacecd4
                encoder.WriteVInt(0); // 0xacece8

                encoder.WriteVInt(0); // 0xacecfc

                encoder.WriteVInt(0); // 0xacee58
                encoder.WriteVInt(0); // 0xacee6c
            }
            else
            {
                encoder.WriteVInt(0);
                encoder.WriteVInt(9); // slot
                encoder.WriteVInt(0);
                encoder.WriteVInt(1588);
                encoder.WriteVInt(10);

                ByteStreamHelper.WriteDataReference(encoder, 15000000);

                encoder.WriteVInt(2); // 0xacec7c
                encoder.WriteString(null); // 0xacecac
                encoder.WriteVInt(0); // 0xacecc0
                encoder.WriteVInt(3); // - пытки
                encoder.WriteVInt(1488); // всего пыток 

                encoder.WriteVInt(0); // 0xacecfc

                encoder.WriteVInt(1); // 0xacee58
                encoder.WriteVInt(1); // 0xacee6c
            }
        }
    }
}
