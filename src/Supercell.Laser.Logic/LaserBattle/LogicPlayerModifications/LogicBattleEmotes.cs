using Supercell.Laser.Logic.Data;
using Supercell.Laser.Logic.Helper;
using Supercell.Laser.Titan.DataStream;

namespace Supercell.Laser.Logic.Battle.LogicPlayerModifications
{
    public class LogicBattleEmotes
    {
        public void Encode(ByteStream stream)
        {
            ByteStreamHelper.WriteDataReference(stream,
                GlobalId.CreateGlobalId(CsvHelperTable.Emotes.GetId(), 93)); // а че дальше
            ByteStreamHelper.WriteDataReference(stream,
                GlobalId.CreateGlobalId(CsvHelperTable.Emotes.GetId(), 93));
            ByteStreamHelper.WriteDataReference(stream,
                GlobalId.CreateGlobalId(CsvHelperTable.Emotes.GetId(), 93));
            ByteStreamHelper.WriteDataReference(stream,
                GlobalId.CreateGlobalId(CsvHelperTable.Emotes.GetId(), 93));
        }
    }

}
