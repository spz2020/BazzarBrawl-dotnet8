
using Supercell.Laser.Logic.Data;

namespace Supercell.Laser.Logic.Csv;

public static class LogicDataTables
{
    private static string _globalPath = null!;
    private static Dictionary<int, LogicDataTable> _dataTables;

    public static void CreateReferences(string gPath)
    {
        _globalPath = gPath;
        _dataTables = new Dictionary<int, LogicDataTable>();

        InitDataTable(CsvHelperTable.Resources.GetFileName(), CsvHelperTable.Resources.GetId());
        InitDataTable(CsvHelperTable.Projectiles.GetFileName(), CsvHelperTable.Projectiles.GetId());
        InitDataTable(CsvHelperTable.AllianceBadges.GetFileName(), CsvHelperTable.AllianceBadges.GetId());
        InitDataTable(CsvHelperTable.AllianceRoles.GetFileName(), CsvHelperTable.AllianceRoles.GetId());
        InitDataTable(CsvHelperTable.Locations.GetFileName(), CsvHelperTable.Locations.GetId());
        InitDataTable(CsvHelperTable.Characters.GetFileName(), CsvHelperTable.Characters.GetId());
        InitDataTable(CsvHelperTable.AreaEffects.GetFileName(), CsvHelperTable.AreaEffects.GetId());
        InitDataTable(CsvHelperTable.Items.GetFileName(), CsvHelperTable.Items.GetId());
        InitDataTable(CsvHelperTable.Skills.GetFileName(), CsvHelperTable.Skills.GetId());
        InitDataTable(CsvHelperTable.Cards.GetFileName(), CsvHelperTable.Cards.GetId());
        InitDataTable(CsvHelperTable.Tiles.GetFileName(), CsvHelperTable.Tiles.GetId());
        InitDataTable(CsvHelperTable.GameModeVariation.GetFileName(), CsvHelperTable.GameModeVariation.GetId());
        InitDataTable(CsvHelperTable.Messages.GetFileName(), CsvHelperTable.Messages.GetId());
        InitDataTable(CsvHelperTable.Milestones.GetFileName(), CsvHelperTable.Milestones.GetId());
        InitDataTable(CsvHelperTable.NameColors.GetFileName(), CsvHelperTable.NameColors.GetId());
        InitDataTable(CsvHelperTable.Regions.GetFileName(), CsvHelperTable.Regions.GetId());
        InitDataTable(CsvHelperTable.PlayerThumbnails.GetFileName(), CsvHelperTable.PlayerThumbnails.GetId());
        InitDataTable(CsvHelperTable.Skins.GetFileName(), CsvHelperTable.Skins.GetId());
        InitDataTable(CsvHelperTable.Globals.GetFileName(), CsvHelperTable.Globals.GetId());
        InitDataTable(CsvHelperTable.Themes.GetFileName(), CsvHelperTable.Themes.GetId());
        InitDataTable(CsvHelperTable.SkinConfs.GetFileName(), CsvHelperTable.SkinConfs.GetId());
    }

    private static void InitDataTable(string path, int tableIndex)
    {
        if (!File.Exists(_globalPath + path)) return;
        {
            var lines = File.ReadAllLines(_globalPath + path);
            {
                if (lines.Length > 1) _dataTables!.Add(tableIndex, new LogicDataTable(new CsvNode(lines, path).GetTable(), tableIndex));
            }
        }
    }

    public static int GetTableCount()
    {
        return 61;
    }

    public static LogicDataCSV GetDataById(int globalId)
    {
        return GlobalId.GetClassId(globalId) is >= 0 and <= 60 && _dataTables![GlobalId.GetClassId(globalId)] != null!
            ? _dataTables[GlobalId.GetClassId(globalId)].GetItemById(globalId)
            : null!;
    }

    private static LogicDataTable GetDataFromTable(int tableIndex)
    {
        return tableIndex >= 0 && tableIndex <= GetTableCount() - 1 && _dataTables![tableIndex] != null!
            ? _dataTables[tableIndex]
            : null!;
    }

    public static LogicDataCSV[] GetAllDataFromCsvById(int id)
    {
        var data = Array.Empty<LogicDataCSV>();
        if (GetDataFromTable(id) == null!) return null!;

        for (var i = 0; i < GetDataFromTable(id).GetItemCount(); i++)
        {
            Array.Resize(ref data, data.Length + 1);
            {
                data[^1] = GetDataById(GlobalId.CreateGlobalId(id, i));
            }
        }

        return data;
    }

    public static LogicSkillData GetSkillByName(string name)
    {
        return (LogicSkillData)_dataTables![CsvHelperTable.Skills.GetId()].GetDataByName(name);
    }
    public static LogicProjectileData GetProjectileByName(string name)
    {
        return (LogicProjectileData)_dataTables![CsvHelperTable.Projectiles.GetId()].GetDataByName(name);
    }

    public static LogicSkinConfData GetSkinConfByName(string name)
    {
        return (LogicSkinConfData)_dataTables![CsvHelperTable.SkinConfs.GetId()].GetDataByName(name);
    }

    public static LogicSkinData GetSkinByName(string name)
    {
        return (LogicSkinData)_dataTables![CsvHelperTable.Skins.GetId()].GetDataByName(name);
    }
}