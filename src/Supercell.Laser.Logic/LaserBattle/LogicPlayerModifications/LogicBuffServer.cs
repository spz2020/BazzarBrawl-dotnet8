using Supercell.Laser.Logic.Battle.Objects;

namespace Supercell.Laser.Logic.Battle.LogicPlayerModifications;
public class LogicBuffServer
{
    private readonly int BuffType;
    private readonly int TickOnEnd;
    private readonly LogicCharacterServer CharacterServer;

    public LogicBuffServer(int buffType, int tickOnEnd, LogicCharacterServer characterServer)
    {
        BuffType = buffType;
        TickOnEnd = tickOnEnd;
        CharacterServer = characterServer;
    }

    public bool Tick(int a1)
    {
        if (OnBuffEnd(a1)) return true;
        // CharacterServer.IncreaseSize(1);
        return false;
    }

    public bool CanBuffStack()
    {
        return BuffType == 10;
    }

    public bool OnBuffEnd(int a1)
    {
        return a1 > TickOnEnd;
    }
}