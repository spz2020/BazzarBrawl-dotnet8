namespace Supercell.Laser.Logic.Battle.LogicPlayerModifications;
public class LogicAttackSpecialParams // потом еще это доделать надо да если что да хорошо todo ПОМЕНТИМ как TO DOOOOOOO
{
    private readonly Dictionary<float, int[]> PatternDictionaryList;

    public LogicAttackSpecialParams()
    {
        PatternDictionaryList = new Dictionary<float, int[]>();

        var Pattern = new int[100];

        var TwicedAngle = 1;
        var AngleCorrector = -1;

        for (var i = 0; i < 100; i++)
            if (i % 2 == 0)
                Pattern[i] = TwicedAngle;
            else
                Pattern[i] = AngleCorrector;

        PatternDictionaryList.Add(0.2f, Pattern);
    }

    public int[] AssignValues(float a1, int a2)
    {
        return PatternDictionaryList[a1];
    }

    public void Destruct()
    {
        PatternDictionaryList.Clear();
    }
}