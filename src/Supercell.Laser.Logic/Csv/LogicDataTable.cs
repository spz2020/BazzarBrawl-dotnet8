using Supercell.Laser.Titan.Math;

namespace Supercell.Laser.Logic.Csv;

public class LogicDataTable
{
    private readonly CsvTable _table;
    private readonly int _tableIndex;
    private LogicArrayList<LogicDataCSV> _items = null!;

    public LogicDataTable(CsvTable table, int index)
    {
        _tableIndex = index;
        _table = table;

        LoadTable();
    }

    public void LoadTable()
    {
        _items = new LogicArrayList<LogicDataCSV>();
        {
            for (var i = 0; i < _table.GetRowCount(); i++)
            {
                var data = CreateItem(_table.GetRowAt(i));

                if (data == null!) break;

                _items.Add(data);
            }
        }

        CreateReferences();
    }

    public LogicDataCSV CreateItem(CsvRow row)
    {
        LogicDataCSV data = null!;

        return _tableIndex switch
        {
            20 => new LogicSkillData(row, this),
            _ => data
        };
    }

    public void CreateReferences()
    {
        //for (var i = 0; i < _items.Count; i++) _items[i].AutoLoadData();
        //for (var i = 0; i < _items.Count; i++) _items[i].CreateReferences();
    }

    public LogicDataCSV GetItemAt(int index)
    {
        return _items[index];
    }

    public LogicDataCSV GetDataByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null!;
        {
            for (var i = 0; i < _items.Count; i++)
                if (_items[i].GetName().Equals(name))
                    return _items[i];
        }

        return null!;
    }

    public LogicDataCSV GetItemById(int globalId)
    {
        return GlobalId.GetInstanceId(globalId) < 0 || GlobalId.GetInstanceId(globalId) >= _items.Count
            ? null!
            : _items[GlobalId.GetInstanceId(globalId)];
    }

    public int GetItemCount()
    {
        return _items.Count;
    }

    public int GetTableIndex()
    {
        return _tableIndex;
    }

    public string GetTableName()
    {
        return _table.GetFileName();
    }
}